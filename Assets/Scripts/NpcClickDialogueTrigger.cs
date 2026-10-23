using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Sorumluluk: Oyuncu NPC'ye tıkladığında ya da yakındayken E'ye bastığında
// diyalog akışını başlatmak ve kapatmak. Paneli ya da hareketi kendisi bilmez
// bunlar Inspector'dan olaylara bağlanır tek sorumluluk, gevşek bağlılık
[RequireComponent(typeof(Collider))]
public class NpcClickDialogueTrigger : MonoBehaviour
{
    [Header("Etkileşim")]
    [SerializeField] private float interactDistance = 4f;   // bu mesafede konuşma açılır
    [SerializeField] private float hintDistance = 8f;       // E'ye basınca "uzaksın" uyarısı için üst sınır
    [SerializeField] private Transform player;              // boşsa 'Player' tag'i ya da adı aranır
    [SerializeField] private Camera cam;                    // boşsa Camera.main aranır
    [SerializeField] private bool allowKeyE = true;
    [SerializeField] private bool closeWithEsc = true;
    [SerializeField] private bool lookAtPlayerOnOpen = true;
    [SerializeField] private float messageCooldown = 1.5f;
    [SerializeField] private string tooFarMessage = "Basri Amca'ya biraz daha yaklaş.";

    [Header("Hata ayıklama")]
    [SerializeField] private bool debugLog = false;

    [Header("Olaylar")]
    [SerializeField] private UnityEvent onDialogueOpened;
    [SerializeField] private UnityEvent onDialogueClosed;

    public bool IsOpen { get; private set; }

    private float lastMessageTime = -999f;

    void Awake()
    {
        if (player == null)
        {
            GameObject p = null;
            try { p = GameObject.FindGameObjectWithTag("Player"); } catch { }
            if (p == null) p = GameObject.Find("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (IsOpen)
        {
            if (closeWithEsc && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
            return;
        }

        bool clicked = Mouse.current != null
                       && Mouse.current.leftButton.wasPressedThisFrame
                       && ClickedOnThisNpc();

        bool keyPressed = allowKeyE
                          && Keyboard.current != null
                          && Keyboard.current.eKey.wasPressedThisFrame
                          && DistanceToPlayer() <= hintDistance;

        if (!clicked && !keyPressed) return;

        if (DistanceToPlayer() > interactDistance)
        {
            WarnTooFar();
            return;
        }

        Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (lookAtPlayerOnOpen && player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (debugLog) Debug.Log("[Basri] Diyalog açıldı.");
        onDialogueOpened?.Invoke();
    }

    // "Sohbetten çık" butonu bu metoda bağlanır
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (debugLog) Debug.Log("[Basri] Diyalog kapandı.");
        onDialogueClosed?.Invoke();
    }

    float DistanceToPlayer()
    {
        if (player == null) return 0f; // oyuncu bulunamazsa mesafe kontrolü yapma
        return Vector3.Distance(player.position, transform.position);
    }

    void WarnTooFar()
    {
        if (Time.time - lastMessageTime < messageCooldown) return;
        lastMessageTime = Time.time;

        if (debugLog) Debug.Log($"[Basri] Çok uzak: {DistanceToPlayer():F1} m (sınır {interactDistance} m)");
        HintUI.Say(tooFarMessage);
    }

    bool ClickedOnThisNpc()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            if (debugLog) Debug.LogWarning("[Basri] Kamera bulunamadı.");
            return false;
        }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);

        bool found = false;
        foreach (RaycastHit h in hits)
        {
            if (debugLog) Debug.Log($"[Basri] Tıklama isabet: {h.collider.name}");
            if (h.collider.transform.IsChildOf(transform)) found = true;
        }

        if (debugLog && !found) Debug.Log("[Basri] Tıklama Basri Amca'ya isabet etmedi.");
        return found;
    }
}