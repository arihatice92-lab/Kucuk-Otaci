using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum QuestId
{
    BridgeRepaired,   // Fındık yıkık köprüyü onardı
    SnakeResolved,    // Erzak yılana verildi, yol açıldı
    DebrisCleared,    // Selin getirdiği çöpler toplandı
    KittenRescued     // Fındık ağaçta mahsur kalan kedi yavrusunu kurtardı
}

// Görev durumunu okumak isteyen sınıflar (Validator, DialogueManager vb.)
// somut QuestManager'a değil bu arayüze bağımlı olur (SOLID - Dependency Inversion).
public interface IQuestProgress
{
    bool AllCompleted { get; }
    bool IsCompleted(QuestId id);
    string BuildStatusForPrompt();

    bool TryGetMissingHint(out string fact, out string[] keywords);
}

// Sorumluluk: Hangi görevin tamamlandığını tutar ve duyurur. Başka bir şey yapmaz.
public class QuestManager : MonoBehaviour, IQuestProgress
{
    public static QuestManager Instance { get; private set; }

    public event Action<QuestId> OnQuestCompleted;
    public event Action OnAllQuestsCompleted;

    private readonly HashSet<QuestId> completed = new HashSet<QuestId>();

    private static readonly Dictionary<QuestId, string> Descriptions = new Dictionary<QuestId, string>
    {
        { QuestId.BridgeRepaired, "yıkık köprü onarıldı" },
        { QuestId.SnakeResolved,  "yoldaki yılana erzak verilip geçildi" },
        { QuestId.DebrisCleared,  "selin getirdiği çöpler toplandı" },
        { QuestId.KittenRescued,  "ağaçta mahsur kalan kedi yavrusu kurtarıldı" }
    };
    private static readonly Dictionary<QuestId, string> MissingFacts = new Dictionary<QuestId, string>
{
    { QuestId.BridgeRepaired, "Köprü hâlâ yıkık." },
    { QuestId.SnakeResolved,  "Yoldaki yılan hâlâ yolu kapatıyor." },
    { QuestId.DebrisCleared,  "Selin getirdiği çöpler hâlâ toplanmadı." },
    { QuestId.KittenRescued,  "Ağaçtaki kedi yavrusu hâlâ kurtarılmadı." }
};

    private static readonly Dictionary<QuestId, string[]> MissingKeywords = new Dictionary<QuestId, string[]>
{
    { QuestId.BridgeRepaired, new[] { "köprü" } },
    { QuestId.SnakeResolved,  new[] { "yılan" } },
    { QuestId.DebrisCleared,  new[] { "çöp"} },
    { QuestId.KittenRescued,  new[] { "kedi", "yavru" } }
};

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool IsCompleted(QuestId id) => completed.Contains(id);

    public bool AllCompleted =>
        Enum.GetValues(typeof(QuestId)).Cast<QuestId>().All(completed.Contains);

    public void Complete(QuestId id)
    {
        if (!completed.Add(id)) return; // aynı görev tekrar bildirilirse yok say

        Debug.Log($"[Quest] Tamamlandı: {id}");
        OnQuestCompleted?.Invoke(id);

        if (AllCompleted)
        {
            Debug.Log("[Quest] Tüm görevler tamam.");
            OnAllQuestsCompleted?.Invoke();
        }
    }

    // Basri Amca'nın isteğine eklenecek gerçek oyun durumu özeti
    public string BuildStatusForPrompt()
    {
        var done = new List<string>();
        var missing = new List<string>();

        foreach (QuestId id in Enum.GetValues(typeof(QuestId)))
        {
            if (completed.Contains(id)) done.Add(Descriptions[id]);
            else missing.Add(Descriptions[id]);
        }

        string doneText = done.Count > 0 ? string.Join(", ", done) : "hiçbiri";
        string missingText = missing.Count > 0 ? string.Join(", ", missing) : "yok";

        string rule = missing.Count > 0
            ? "Eksik görev olduğu için kapıyı AÇMA, OPEN_GATE seçme; ASK_MORE seç ve eksik işleri ima et."
            : "Tüm görevler tamam, oyuncu ikna edici ise kapıyı açabilirsin.";

        return $"Oyuncunun yaptığı işler: {doneText}. Eksik işler: {missingText}. {rule}";
    }
    /// <summary>
    /// Tamamlanmamış ilk görevi (enum sırasına göre) döndürür.
    /// Cümleyi kurmaz, yalnızca olguyu ve doğrulama anahtar kelimelerini verir.
    /// </summary>
    public bool TryGetMissingHint(out string fact, out string[] keywords)
    {
        foreach (QuestId id in System.Enum.GetValues(typeof(QuestId)))
        {
            if (completed.Contains(id)) continue;
            fact = MissingFacts[id];
            keywords = MissingKeywords[id];
            return true;
        }
        fact = null;
        keywords = null;
        return false;
    }
}
