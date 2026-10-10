using TMPro;
using UnityEngine;

// Sorumluluk: Altındaki tüm DebrisItem'ları sayar. Hepsi toplanınca "çöp" görevini tamamlar.
// Yeni çöp eklemek için çöp nesnesini bu nesnenin altına sürüklemek yeterlidir.
public class DebrisField : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;   // İsteğe bağlı: "Çöp: 2/5"
    [SerializeField] private string completedMessage = "Yol temizlendi!";

    private DebrisItem[] items;
    private int remaining;

    private void Awake()
    {
        items = GetComponentsInChildren<DebrisItem>(true);
        remaining = items.Length;

        if (items.Length == 0)
            Debug.LogWarning("[DebrisField] Altında hiç DebrisItem yok, görev hiçbir zaman tamamlanmaz.");

        foreach (DebrisItem item in items)
            item.Collected += OnItemCollected;

        UpdateCounter();
    }

    private void OnDestroy()
    {
        if (items == null) return;

        foreach (DebrisItem item in items)
            if (item != null) item.Collected -= OnItemCollected;
    }

    private void OnItemCollected(DebrisItem item)
    {
        remaining--;
        UpdateCounter();

        if (remaining > 0) return;

        if (QuestManager.Instance != null)
            QuestManager.Instance.Complete(QuestId.DebrisCleared);

        HintUI.Say(completedMessage);
        Debug.Log("[Görev] Tüm çöpler toplandı.");
    }

    private void UpdateCounter()
    {
        if (counterText == null) return;

        int total = items.Length;
        counterText.text = $"Çöp: {total - remaining}/{total}";
    }
}
