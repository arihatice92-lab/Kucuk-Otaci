using System.Globalization;
using UnityEngine;

// LLM'nin kararını oyun kurallarıyla doğrular (LLM öneri yapar, Unity karar verir)
public static class NpcDecisionValidator
{
    private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

    private static readonly string[] HelpKeywords =
    { "şenlik", "hasat", "bayram", "hazırlık", "yardım", "elim iş tut", "çalış", "katkı",
          "adaçay", "nane", "papatya", "otlar", "bitki", "çiçek", "şifalı", "getirdim", "topladım" };

    private static readonly string[] RudeKeywords =
        { "ihtiyar", "moruk", "çekil", "defol", "salak", "aptal", "gerizekalı", "kes sesini",
          "uğraşamam", "fena olur", "gıcık", "sevmedim", "sevmiyorum", "nefret", "kırarım",
          "ahmak", "enayi", "yaşlı adam", "pişman", "göreceksin", "bedelini", "yakarım"};

    // hasHerbs: envanter/bitki durumu
    // questsDone: önceki görevlerin tamamlanma durumu
    // questsDone varsayılan olarak false olmalı (bilgi verilmezse kapı açılmasın)
    // hasHerbs: envanter sistemi hazır olunca InventoryManager'dan gelecek
    public static NpcResult Validate(string playerMessage, NpcResult result, bool hasHerbs = true, bool questsCompleted = true)
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
        else if (final == NpcDecision.OpenGate && !(mentionsHelp && hasHerbs && questsCompleted))
            final = NpcDecision.AskMore;               // gerekçe yok, kapı açılmaz

        if (final != result.Decision)
        {
            Debug.Log($"[Validator] LLM kararı {result.Decision} -> {final} olarak değiştirildi.");
            result.Decision = final;
            result.Mood = final == NpcDecision.Refuse ? NpcMood.Angry : NpcMood.Suspicious;
            result.DecisionOverridden = true;   // cümleyi LLM yeniden üretecek
        }
        result.Validated = true;
        return result;
    }


    private static bool ContainsAny(string text, string[] keywords)
    {
        foreach (var k in keywords)
            if (text.Contains(k)) return true;
        return false;
    }
}