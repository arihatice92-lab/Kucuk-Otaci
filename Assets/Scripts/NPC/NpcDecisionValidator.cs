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
    private static readonly string[] OutOfCharacterWords =
        {
    "gelen yabancı", "yabancıyı", "yabancıya", "oyuncu", "karşındaki", "sistem", "prompt", "npc", "yapay zeka", "json", "talimat", "cümle kur", "kısa cümle", "kendi ağzınla", "kendi sözlerinle", "durum:", "aynen yaz", "bekçisin", "bekçisi", "huysuz", "şüpheci", "geleneksel", "özünde", "iyi niyetli"};

    private static readonly string[] OpenPhrases =
{ "kapı açık", "kapıyı açıyorum", "kapıyı açtım", "içeri gir", "gir içeri", "geçebilirsin", "buyur gir" };

    private static readonly string[] ClosedPhrases =
    { "açmam", "açmıyorum", "giremezsin", "girmek yok", "geçemezsin" };

    public static bool ContradictsDecision(NpcDecision decision, string dialogue)
    {
        if (string.IsNullOrEmpty(dialogue)) return false;

        string d = dialogue.ToLower(Tr);
        d = d.Replace("açık değil", "").Replace("açılmaz", ""); // "kapı açık değil" olumsuz, çelişki değil

        var list = decision == NpcDecision.OpenGate ? ClosedPhrases : OpenPhrases;
        foreach (var p in list)
            if (d.Contains(p)) return true;
        return false;
    }
    public static bool LooksOutOfCharacter(string dialogue)
    {
        if (string.IsNullOrEmpty(dialogue)) return false;

        string d = dialogue.ToLower(Tr);
        foreach (var w in OutOfCharacterWords)
            if (d.Contains(w)) return true;
        return false;
    }
    // hasHerbs: envanter/bitki durumu
    // questsDone: önceki görevlerin tamamlanma durumu
    // questsDone varsayılan olarak false olmalı (bilgi verilmezse kapı açılmasın)
    // hasHerbs: envanter sistemi hazır olunca InventoryManager'dan gelecek
    public static NpcResult Validate(string playerMessage, NpcResult result, bool hasHerbs = true, bool questsDone = false)
    {
        if (result == null || !result.Success) return result;

        string msg = (playerMessage ?? "").ToLower(Tr);

        // Olumlu kalıplar hakaret sayılmasın ("pişman olmayacaksın" bir güvence cümlesi)
        msg = msg.Replace("pişman olmayacaksın", "")
                 .Replace("pişman olmazsın", "")
                 .Replace("pişman olmam", "")
                 .Replace("pişman etmem", "");

        bool mentionsHelp = ContainsAny(msg, HelpKeywords);
        bool isRude = ContainsAny(msg, RudeKeywords);
        // Oyuncu kaba değil, gerekçesi ve otu var; kapıyı yalnızca eksik görevler engelliyor
        bool deniedByQuests = !isRude && mentionsHelp && hasHerbs && !questsDone;
        result.DeniedByMissingQuests = deniedByQuests;

        NpcDecision final = result.Decision;

        if (isRude)
            final = NpcDecision.Refuse;
        else if (final == NpcDecision.Refuse)
            final = NpcDecision.AskMore;               // kaba değil, ceza yok
        else if (final == NpcDecision.OpenGate && !(mentionsHelp && hasHerbs && questsDone))
            final = NpcDecision.AskMore;               // gerekçe veya görevler eksikse kapı açılmaz

        if (final != result.Decision)
        {
            Debug.Log($"[Validator] LLM kararı {result.Decision} -> {final} olarak değiştirildi.");
            result.Decision = final;
            result.Mood = final == NpcDecision.Refuse ? NpcMood.Angry : NpcMood.Suspicious;
            result.DecisionOverridden = true;   // cümleyi LLM yeniden üretecek
        }

        if (deniedByQuests && final != NpcDecision.AskMore)
            result.DeniedByMissingQuests = false;   // güvenlik: bayrak yalnızca AskMore ile birlikte geçerli

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