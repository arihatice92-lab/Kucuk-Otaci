using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Sorumluluk: Görev listesini ekranda gösterir. Tamamlanan görevler yeşil ve üstü çizili görünür.
/// Görev durumunu QuestManager'dan okur, kendisi görev tutmaz.
/// </summary>
public class QuestHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private static readonly Dictionary<QuestId, string> Titles = new Dictionary<QuestId, string>
    {
        { QuestId.BridgeRepaired, "Köprüyü onar" },
        { QuestId.SnakeResolved,  "Yılana erzak ver" },
        { QuestId.DebrisCleared,  "Selin çöplerini topla" },
        { QuestId.KittenRescued,  "Kediyi kurtar" },
        { QuestId.HerbsCollected, "Şifalı otları topla" }
    };

    private QuestManager quests;

    private void Start()
    {
        quests = QuestManager.Instance;

        if (label == null || quests == null)
        {
            Debug.LogError("QuestHUD: Label atanmamış ya da sahnede QuestManager yok.", this);
            enabled = false;
            return;
        }

        quests.OnQuestCompleted += HandleQuestCompleted;
        Refresh();
    }

    private void OnDestroy()
    {
        if (quests != null)
            quests.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void HandleQuestCompleted(QuestId id) => Refresh();

    private void Refresh()
    {
        var text = new StringBuilder();
        int doneCount = 0;

        foreach (var pair in Titles)
        {
            if (quests.IsCompleted(pair.Key))
            {
                doneCount++;
                text.AppendLine($"<color=#7CFC7C><s>{pair.Value}</s></color>");
            }
            else
            {
                text.AppendLine($"• {pair.Value}");
            }
        }

        label.text = $"<b>Görevler ({doneCount}/{Titles.Count})</b>\n{text}";
    }
}