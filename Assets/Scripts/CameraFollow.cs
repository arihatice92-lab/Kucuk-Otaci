using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sorumluluk: Kamerayı oyuncunun arkasında tutar, Q/E ile oyuncunun etrafında döndürür ve
/// oyuncu ile kamera arasına giren engelleri (ağaç, bina) aşmak için kamerayı yaklaştırır.
/// Oyuncunun nasıl hareket ettiğini bilmez; yürüme yönü zaten kameraya göre hesaplanır (PlayerMovement).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    [Tooltip("Boş bırakılırsa sahnedeki oyuncu otomatik bulunur.")]
    [SerializeField] private Transform target;

    [Header("Konum")]
    [Tooltip("Oyuncunun arkasında (-Z) ve yukarısında (Y) duracağı mesafe. Kamera döndükçe birlikte döner.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -8f);
    [Tooltip("Kameranın baktığı nokta, oyuncunun ayağından bu kadar yukarıdadır.")]
    [SerializeField] private float lookHeight = 1.2f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Dönüş (Q / E)")]
    [SerializeField] private Key rotateLeftKey = Key.Q;
    [SerializeField] private Key rotateRightKey = Key.E;
    [Tooltip("Saniyede kaç derece döner.")]
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Engel kaçınma")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float collisionRadius = 0.3f;
    [Tooltip("Kamera engel yüzünden oyuncuya bundan fazla yaklaşmaz.")]
    [SerializeField] private float minDistance = 1.5f;
    [Tooltip("Engel kalkınca kameranın eski mesafesine dönme hızı.")]
    [SerializeField] private float recoverSpeed = 4f;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private float yaw;
    private Vector3 smoothedPosition;
    private float currentDistance;
    private bool snapNextFrame = true;

    private void Awake()
    {
        if (target == null)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null) target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError("CameraFollow: Hedef bulunamadı, kamera takip etmeyecek.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // Oyun başında kamera oyuncunun baktığı yönün arkasında başlar.
        if (target != null) yaw = target.eulerAngles.y;
    }

    // Ara sahneden dönünce (script yeniden açılınca) kamera yavaşça süzülmek yerine anında yerine oturur.
    private void OnEnable()
    {
        snapNextFrame = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleRotationInput();

        Vector3 pivot = target.position + Vector3.up * lookHeight;
        Vector3 desired = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;

        if (snapNextFrame)
        {
            smoothedPosition = desired;
            currentDistance = (desired - pivot).magnitude;
            snapNextFrame = false;
        }
        else
        {
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            smoothedPosition = Vector3.Lerp(smoothedPosition, desired, k);
        }

        transform.position = AvoidObstacles(pivot, smoothedPosition);
        transform.LookAt(pivot);
    }

    private void HandleRotationInput()
    {
        // Diyalog kutusuna yazılırken Q/E harf olarak yazılır, kamera dönmesin.
        if (UIInputState.IsTyping) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float direction = 0f;
        if (keyboard[rotateLeftKey].isPressed) direction -= 1f;
        if (keyboard[rotateRightKey].isPressed) direction += 1f;

        yaw += direction * rotateSpeed * Time.deltaTime;
    }

    // Oyuncudan kameraya bir küre fırlatır, arada engel varsa kamerayı engelin önüne çeker.
    private Vector3 AvoidObstacles(Vector3 pivot, Vector3 position)
    {
        Vector3 direction = position - pivot;
        float fullDistance = direction.magnitude;
        if (fullDistance < 0.01f) return position;
        direction /= fullDistance;

        int count = Physics.SphereCastNonAlloc(
            pivot, collisionRadius, direction, hitBuffer, fullDistance,
            obstacleMask, QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];

            if (hit.distance <= 0f) continue;                                      // başlangıçta içinde kalan collider
            if (hit.collider.transform.IsChildOf(target)) continue;                 // oyuncunun kendisi
            if (hit.collider.GetComponentInParent<CompanionController>() != null) continue; // Fındık
            if (hit.collider.GetComponentInParent<MapBoundary>() != null) continue;         // harita sınırı duvarları

            if (hit.distance < nearest) nearest = hit.distance;
        }

        float allowed = fullDistance;
        if (nearest < float.MaxValue)
        {
            allowed = Mathf.Max(nearest, Mathf.Min(minDistance, fullDistance));
        }

        // Engel yaklaşınca kamera hemen çekilir, engel kalkınca yavaşça geri açılır.
        if (allowed < currentDistance)
        {
            currentDistance = allowed;
        }
        else
        {
            float k = 1f - Mathf.Exp(-recoverSpeed * Time.deltaTime);
            currentDistance = Mathf.Lerp(currentDistance, allowed, k);
        }

        return pivot + direction * currentDistance;
    }
}