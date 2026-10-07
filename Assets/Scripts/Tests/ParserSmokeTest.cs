using UnityEngine;

// TEST aracı: JSON ayrıştırıcıyı ve kapı kuralını Ollama'ya ihtiyaç duymadan sınar.
// Kullanım: Bir nesneye ekle, component başlığının sağındaki ⋮ menüsünden
// "Testleri Çalıştır"ı seç. Sonuçlar Console'da görünür.
// Raporda "bozuk JSON ve karar doğrulama testleri" kanıtı olarak kullanılabilir.
public class ParserSmokeTest : MonoBehaviour
{
    private int passed;
    private int failed;

    private const string PersuasiveMsg = "Şenliğe yardım etmeye geldim, ot topladım.";
    private const string RudeMsg = "Aç şu kapıyı yoksa fena olur, moruk!";
    private const string IrrelevantMsg = "Bu akşam yağmur yağar mı sence?";

    [ContextMenu("Testleri Çalıştır")]
    public void RunAll()
    {
        passed = 0;
        failed = 0;

        RunParserTests();
        RunValidatorTests();

        Debug.Log($"[SmokeTest] SONUÇ: {passed} geçti, {failed} kaldı.");
    }

    // ---------- Parser (LLM'den gelen JSON'u ayrıştırma) ----------

    private void RunParserTests()
    {
        ExpectParse("P1 geçerli OPEN_GATE",
            "{\"decision\":\"OPEN_GATE\",\"mood\":\"FRIENDLY\",\"dialogue\":\"Gir evladım.\"}",
            true, NpcDecision.OpenGate);

        ExpectParse("P2 geçerli ASK_MORE",
            "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Ne için geldin?\"}",
            true, NpcDecision.AskMore);

        ExpectParse("P3 geçerli REFUSE",
            "{\"decision\":\"REFUSE\",\"mood\":\"ANGRY\",\"dialogue\":\"Terbiyesizlik!\"}",
            true, NpcDecision.Refuse);

        ExpectParse("P4 boş metin", "", false);
        ExpectParse("P5 sadece boşluk", "   ", false);
        ExpectParse("P6 JSON olmayan düz metin", "merhaba ben Basri Amca", false);
        ExpectParse("P7 bozuk JSON", "{bozuk json", false);
        ExpectParse("P8 boş nesne", "{}", false);

        ExpectParse("P9 dialogue alanı eksik",
            "{\"decision\":\"OPEN_GATE\",\"mood\":\"FRIENDLY\"}", false);

        ExpectParse("P10 bilinmeyen decision",
            "{\"decision\":\"BILMIYORUM\",\"mood\":\"FRIENDLY\",\"dialogue\":\"x\"}", false);

        ExpectParse("P11 bilinmeyen mood",
            "{\"decision\":\"ASK_MORE\",\"mood\":\"SEVINCLI\",\"dialogue\":\"x\"}", false);

        ExpectParse("P12 küçük harfli decision (katı kural)",
            "{\"decision\":\"open_gate\",\"mood\":\"FRIENDLY\",\"dialogue\":\"x\"}", false);

        ExpectParse("P13 markdown kod bloğuna sarılmış JSON",
            "```json\n{\"decision\":\"OPEN_GATE\",\"mood\":\"FRIENDLY\",\"dialogue\":\"x\"}\n```", false);

        ExpectParse("P14 fazladan alanı olan geçerli JSON",
            "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"x\",\"ekstra\":\"y\"}",
            true, NpcDecision.AskMore);
    }

    private void ExpectParse(string name, string json, bool expectSuccess, NpcDecision? expectedDecision = null)
    {
        NpcResult r = NpcResponseParser.Parse(json);

        bool ok = r != null && r.Success == expectSuccess;
        if (ok && expectSuccess && expectedDecision.HasValue)
            ok = r.Decision == expectedDecision.Value;

        Report(name, ok, ok ? "" : $"Success={r?.Success}, Decision={r?.Decision}, Error={r?.Error}");
    }

    // ---------- Validator (Unity'nin son sözü) ----------

    private void RunValidatorTests()
    {
        ExpectValidate("V1 görevler eksik, LLM OPEN_GATE dedi -> kapı açılmaz",
            PersuasiveMsg, NpcDecision.OpenGate, true, false, NpcDecision.AskMore, true);

        ExpectValidate("V2 görevler tamam, ikna edici -> kapı açılır",
            PersuasiveMsg, NpcDecision.OpenGate, true, true, NpcDecision.OpenGate, false);

        ExpectValidate("V3 görevler tamam ama ot yok -> kapı açılmaz",
            PersuasiveMsg, NpcDecision.OpenGate, false, true, NpcDecision.AskMore, true);

        ExpectValidate("V4 kaba mesaj -> reddedilir",
            RudeMsg, NpcDecision.OpenGate, true, true, NpcDecision.Refuse, true);

        ExpectValidate("V5 alakasız mesaj, LLM OPEN_GATE dedi -> kapı açılmaz",
            IrrelevantMsg, NpcDecision.OpenGate, true, true, NpcDecision.AskMore, true);

        ExpectValidate("V6 kaba olmayan mesajda LLM REFUSE dedi -> ASK_MORE'a çevrilir",
            PersuasiveMsg, NpcDecision.Refuse, true, true, NpcDecision.AskMore, true);

        // Varsayılan parametre: görev bilgisi verilmezse kapı kapalı kalmalı (güvenli taraf)
        NpcResult r = NpcDecisionValidator.Validate(PersuasiveMsg, Make(NpcDecision.OpenGate));
        Report("V7 görev bilgisi verilmezse kapı kapalı kalır",
            r.Decision == NpcDecision.AskMore, $"gelen {r.Decision}");
    }

    private void ExpectValidate(string name, string msg, NpcDecision llm, bool hasHerbs,
                                bool questsDone, NpcDecision expected, bool expectOverridden)
    {
        NpcResult r = NpcDecisionValidator.Validate(msg, Make(llm), hasHerbs, questsDone);
        bool ok = r.Decision == expected && r.DecisionOverridden == expectOverridden;
        Report(name, ok, $"beklenen {expected}/override={expectOverridden}, gelen {r.Decision}/override={r.DecisionOverridden}");
    }

    private static NpcResult Make(NpcDecision d) => new NpcResult
    {
        Success = true,
        Decision = d,
        Mood = NpcMood.Friendly,
        Dialogue = "test"
    };

    // ---------- Raporlama ----------

    private void Report(string name, bool ok, string detail)
    {
        if (ok)
        {
            passed++;
            Debug.Log($"[SmokeTest] GEÇTİ: {name}");
        }
        else
        {
            failed++;
            Debug.LogError($"[SmokeTest] KALDI: {name} | {detail}");
        }
    }
}
