using TMPro;
using UnityEngine;

// Sorumluluk: Altındaki tüm HerbPickup'ları sayar. Gereken sayıda ot toplanınca
// "şifalı ot" görevini tamamlar. Yeni ot eklemek için ot nesnesini bu nesnenin altına sürüklemek yeterlidir.
public class HerbField : MonoBehaviour
{
    [Header("Görev")]
    [Tooltip("Görevin tamamlanması için toplanması gereken ot sayısı. 0 ise alandaki hepsi gerekir.")]
    [SerializeField] private int requiredCount = 3;
    [SerializeField] private string completedMessage = "Yeterince şifalı ot topladın, köy kapısına gidebilirsin!";

    [Header("İsteğe bağlı")]
    [SerializeField] private TMP_Text counterText;   // "Şifalı ot: 2/3"

    private HerbPickup[] items;
    private int collected;
    private int required;
    private bool completed;

    private void Awake()
    {
        items = GetComponentsInChildren<HerbPickup>(true);

        if (items.Length == 0)
            Debug.LogWarning("[HerbField] Altında hiç HerbPickup yok, görev hiçbir zaman tamamlanmaz.");

        if (requiredCount > items.Length)
            Debug.LogWarning($"[HerbField] {requiredCount} ot gerekiyor ama alanda sadece {items.Length} ot var. Görev tamamlanamaz!");

        required = requiredCount > 0 ? requiredCount : items.Length;

        foreach (HerbPickup item in items)
            item.Collected += OnHerbCollected;

        UpdateCounter();
    }

    private void OnDestroy()
    {
        if (items == null) return;

        foreach (HerbPickup item in items)
            if (item != null) item.Collected -= OnHerbCollected;
    }

    private void OnHerbCollected(HerbPickup item)
    {
        collected++;
        UpdateCounter();

        if (completed || collected < required) return;
        completed = true;

        if (QuestManager.Instance != null)
            QuestManager.Instance.Complete(QuestId.HerbsCollected);

        HintUI.Say(completedMessage, 4f);
        Debug.Log("[Görev] Yeterli şifalı ot toplandı.");
    }

    private void UpdateCounter()
    {
        if (counterText == null) return;
        counterText.text = $"Şifalı ot: {collected}/{required}";
    }
}
