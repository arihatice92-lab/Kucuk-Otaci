using System.Globalization;
using UnityEngine;

// LLM'nin kararını oyun kurallarıyla doğrular (LLM öneri yapar, Unity karar verir)
public static class NpcDecisionValidator
{
    private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

    private static readonly string[] HelpKeywords =
        { "şenlik", "hasat", "yardım", "adaçay", "nane", "papatya", "şifalı", "bitki" };

    private static readonly string[] RudeKeywords =
        { "ihtiyar", "moruk", "çekil", "defol", "salak", "aptal", "gerizekalı",
          "kes sesini", "uğraşamam", "fena olur" };

    private static readonly string[] AskMoreLines =
    {
        "Hmm, yabancı yüz. Köye ne için geldin, açık söyle bakalım.",
        "Bu kadarıyla kapıyı açmam. Asıl niyetini anlat.",
        "Bir dur bakalım evladım. Kimsin, ne istiyorsun burada?",
        "Lafı dolandırma evladım, köye niçin geldin?",
        "Hı hı, peki. Ama asıl derdini anlatmadın henüz.",
        "Burası şaka yeri değil, ciddi ol da söyle bakalım."
    };

    private const string RefuseLine = "Bu ne terbiyesizlik! Sakinleş, sonra konuşuruz.";

    // hasHerbs: envanter sistemi hazır olunca InventoryManager'dan gelecek
    public static NpcResult Validate(string playerMessage, NpcResult result, bool hasHerbs = true)
    {
        if (result == null || !result.Success) return result;

        string msg = (playerMessage ?? "").ToLower(Tr);
        bool mentionsHelp = ContainsAny(msg, HelpKeywords);
        bool isRude = ContainsAny(msg, RudeKeywords);

        NpcDecision final = result.Decision;

        if (isRude)
            final = NpcDecision.Refuse;
        else if (final == NpcDecision.Refuse)
            final = NpcDecision.AskMore;               // kaba değil, ceza yok
        else if (final == NpcDecision.OpenGate && !(mentionsHelp && hasHerbs))
            final = NpcDecision.AskMore;               // gerekçe yok, kapı açılmaz

        if (final != result.Decision)
        {
            Debug.Log($"[Validator] LLM kararı {result.Decision} -> {final} olarak değiştirildi.");
            result.Decision = final;
            result.Mood = final == NpcDecision.Refuse ? NpcMood.Angry : NpcMood.Suspicious;
            result.Dialogue = final == NpcDecision.Refuse
                ? RefuseLine
                : AskMoreLines[Random.Range(0, AskMoreLines.Length)];
        }

        return result;
    }

    private static bool ContainsAny(string text, string[] keywords)
    {
        foreach (var k in keywords)
            if (text.Contains(k)) return true;
        return false;
    }
}