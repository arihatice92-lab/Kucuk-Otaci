using UnityEngine;

// Sorumluluk: Yoldaki yılan karşılaşması. Oyuncu E'ye basınca elindeki erzağı yılana verir,
// yılan çekilir ve yol açılır. Erzak yoksa oyun kilitlenmez, ipucu mesajı gösterilir.
public class SnakeEncounter : MonoBehaviour, IPlayerInteractable
{
    [Header("Gereken eşya")]
    [SerializeField] private PlayerInventory inventory;   // Boşsa sahnede otomatik bulunur
    [SerializeField] private ItemId requiredItem = ItemId.Erzak;
    [SerializeField] private int requiredCount = 1;

    [Header("Çözülünce kapanacaklar")]
    [SerializeField] private GameObject[] disableOnResolved;   // Yılan modeli ve yolu kapatan engel

    [Header("Mesajlar")]
    [SerializeField] private string noItemMessage = "Yılan yolu kapatıyor. Ona verecek bir şeyin yok.";
    [SerializeField] private string resolvedMessage = "Yılan erzağı alıp çekildi, yol açıldı!";

    public bool IsResolved { get; private set; }

    public string PlayerPrompt => "Erzağı yılana ver";
    public bool CanInteract => !IsResolved;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindAnyObjectByType<PlayerInventory>();
    }

    public void InteractByPlayer()
    {
        if (IsResolved) return;

        if (inventory == null || !inventory.TryConsume(requiredItem, requiredCount))
        {
            HintUI.Say(noItemMessage);
            return;
        }

        IsResolved = true;

        foreach (GameObject go in disableOnResolved)
            if (go != null) go.SetActive(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.Complete(QuestId.SnakeResolved);

        HintUI.Say(resolvedMessage);
        Debug.Log("[Görev] Yılan karşılaşması çözüldü.");
    }
}
