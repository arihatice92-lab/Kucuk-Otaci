using System;
using UnityEngine;

// Sorumluluk: Sadece kapının fiziksel hareketini ve etkileşimini yönetir (SOLID - Single Responsibility).
// Açılma başladığında ve bittiğinde olayla haber verir; sonrasında ne olacağını bilmez.
public class VillageGate : MonoBehaviour, IInteractable
{
    [Header("Kapı Hareket Ayarları")]
    [Tooltip("Kapı açıldığında yukarı kalkacağı mesafe")]
    [SerializeField] private Vector3 openOffset = new Vector3(0, 4f, 0);
    [SerializeField] private float openSpeed = 2f;

    [SerializeField] private NpcController npcController;

    [Header("Ses (isteğe bağlı)")]
    [SerializeField] private AudioSource openSound;

    public event Action OpeningStarted;
    public event Action Opened;

    public string InteractionPrompt => "Köy Kapısı";

    private bool isOpening;
    private bool isOpen;
    private Vector3 targetPosition;

    private void OnEnable()
    {
        if (npcController != null)
        {
            npcController.OnGateShouldOpen += OpenGate;
        }
    }

    private void OnDisable()
    {
        if (npcController != null)
        {
            npcController.OnGateShouldOpen -= OpenGate;
        }
    }

    private void Start()
    {
        targetPosition = transform.position + openOffset;
    }

    private void Update()
    {
        if (!isOpening) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isOpening = false;
            isOpen = true;
            Debug.Log("[VillageGate] Kapı tamamen açıldı.");
            Opened?.Invoke();
        }
    }

    public void Interact()
    {
        Debug.Log("[VillageGate] Kapı kilitli. Basri Amca ile konuşmalısın.");
    }

    public void OpenGate()
    {
        if (isOpening || isOpen) return; // zaten açılıyor ya da açık

        isOpening = true;
        Debug.Log("[VillageGate] Basri Amca kapıyı açıyor...");

        if (openSound != null) openSound.Play();
        OpeningStarted?.Invoke();
    }
}