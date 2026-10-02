using UnityEngine;

// Sorumluluk: Köy kapısının fiziksel hareketini yönetir.

public class VillageGate : MonoBehaviour
{
    [Header("Kapı Hareket Ayarları")]
    [Tooltip("Kapı açıldığında yukarı kalkacağı mesafe")]
    [SerializeField] private Vector3 openOffset = new Vector3(0, 4f, 0);
    [SerializeField] private float openSpeed = 2f;

    private bool isOpening = false;
    private Vector3 targetPosition;

    private void Start()
    {
        targetPosition = transform.position + openOffset;
    }

    private void Update()
    {
        if (isOpening)
        {
            // Kapıyı hedef yüksekliğe doğru akıcı şekilde kaydır
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isOpening = false;
                Debug.Log("[VillageGate] Kapı tamamen açıldı.");
            }
        }
    }

    // LLM'den izin kararı çıktığında dışarıdan çağrılacak tetikleyici fonksiyon
    public void OpenGate()
    {
        if (!isOpening)
        {
            isOpening = true;
            Debug.Log("[VillageGate] Basri Amca kapıyı açıyor...");
        }
    }
}