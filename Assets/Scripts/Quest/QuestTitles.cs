using System.Collections.Generic;

/// <summary>
/// Sorumluluk: Görevlerin oyuncuya gösterilen adlarını tek yerde tutar.
/// Görev listesi (QuestHUD) ve bildirimler (QuestNotifier) aynı adları buradan okur.
/// Yeni görev eklenirse sadece buraya satır eklenir.
/// </summary>
public static class QuestTitles
{
    private static readonly Dictionary<QuestId, string> titles = new Dictionary<QuestId, string>
    {
        { QuestId.BridgeRepaired, "Köprüyü onar" },
        { QuestId.SnakeResolved,  "Yılana erzak ver" },
        { QuestId.DebrisCleared,  "Selin çöplerini topla" },
        { QuestId.KittenRescued,  "Kediyi kurtar" },
        { QuestId.HerbsCollected, "Şifalı otları topla" },
        { QuestId.FloodReceded,   "Sel suyunu çek" },
        { QuestId.TreeCleared,    "Devrilen ağacı kaldır" }
    };

    public static IReadOnlyDictionary<QuestId, string> All => titles;

    public static string Of(QuestId id) =>
        titles.TryGetValue(id, out string title) ? title : id.ToString();
}