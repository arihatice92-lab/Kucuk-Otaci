using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour, ILlmClient
{
    [SerializeField] private string baseUrl = "http://localhost:11434";
    [SerializeField] private string model = "gemma2:2b";

    [SerializeField] private int timeoutSeconds = 60;

    [SerializeField, TextArea(3, 10)]
    private string systemPrompt =

    "Sen Basri Amca'sın, köyün kapısını bekleyen yaşlı bir bekçisin. Huysuz, şüpheci ve geleneksel biri olsan da özünde iyi niyetlisin. Görevin köye yabancıların kontrolsüz girmesini önlemek, ama Hasat Şenliği'ne yardım etmeye gelenleri memnuniyetle karşılarsın. Karşındaki kişiyle doğrudan konuşursun; ona ‘sen’ ya da ‘evladım’ diye hitap edersin. Onun hakkında üçüncü kişi gibi konuşmaz, ‘yabancı’ ya da ‘oyuncu’ kelimelerini kullanmazsın. Oyuncunun cümlesini tekrar etmezsin. Soru soran ya da belirsiz konuşan kişiye soru sorarak karşılık verirsin (ASK_MORE); REFUSE yalnızca açık hakaret veya tehdit varsa seçilir. Cevapların kısa, doğal ve kendi ağzından olur.  Fiilleri her zaman ikinci tekil kişiyle kur: ‘gel’, ‘söyle’, ‘geldin’, ‘istiyorsun’ gibi. Eksiksiz, dilbilgisi doğru, kısa cümleler yaz.";


    // SeedExamples içindeki asistan cümleleriyle aynı tutulmalı
    private static readonly string[] SeedDialogues =
    {
    "Hmm, tanımadığım bir yüz. Köye ne için geldin bakalım?",
    "Bu ne terbiyesizlik! Biraz sakinleş, sonra konuşuruz.",
    "Şenlik için ot mu getirdin? Aferin evladım, kapıyı açıyorum.",
    "Yağmuru bulutlara sor. Sen köye niçin geldin, onu söyle.",
    "Kekeleyip durma evladım, açık konuş. Köye ne için geldin?"
};

    public bool IsSeedCopy(string dialogue)
    {
        if (string.IsNullOrEmpty(dialogue)) return false;
        string d = dialogue.Trim().ToLowerInvariant();
        foreach (var s in SeedDialogues)
            if (d == s.ToLowerInvariant() || d.Contains(s.ToLowerInvariant())) return true;
        return false;
    }
    // forced doluysa decision ve mood tek bir değere kilitlenir
    private static string BuildSchema(string forced)
    {
        string decisionEnum = "[\"OPEN_GATE\",\"ASK_MORE\",\"REFUSE\"]";
        string moodEnum = "[\"FRIENDLY\",\"SUSPICIOUS\",\"ANGRY\"]";

        if (forced != null)
        {
            decisionEnum = "[\"" + forced + "\"]";
            string mood = forced == "OPEN_GATE" ? "FRIENDLY" : forced == "REFUSE" ? "ANGRY" : "SUSPICIOUS";
            moodEnum = "[\"" + mood + "\"]";
        }

        return "{\"type\":\"object\",\"properties\":{" +
               "\"decision\":{\"type\":\"string\",\"enum\":" + decisionEnum + "}," +
               "\"mood\":{\"type\":\"string\",\"enum\":" + moodEnum + "}," +
               "\"dialogue\":{\"type\":\"string\"}}," +
               "\"required\":[\"decision\",\"mood\",\"dialogue\"]}";
    }

    private class ChatMessage
    {
        public string role, content;
        public ChatMessage(string r, string c) { role = r; content = c; }
    }

    [Serializable] private class OllamaMessage { public string role; public string content; }
    [Serializable] private class OllamaChatResponse { public OllamaMessage message; }

    private readonly List<ChatMessage> history = new List<ChatMessage>();

    private string gameState = "";
    public void SetGameState(string state) => gameState = state ?? "";

    //public void ResetConversation() => history.Clear();
    private void Awake() => SeedExamples();

    public void ResetConversation() => SeedExamples();

    // Modele davranışı göstermek için başlangıç örnekleri (few-shot)
    private void SeedExamples()
    {
        history.Clear();

        // 1. Normal selam -> ASK_MORE
        history.Add(new ChatMessage("user", "Günaydın, köye girebilir miyim?"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Hmm, tanımadığım bir yüz. Köye ne için geldin bakalım?\"}"));

        // 2. Kaba davranış -> REFUSE (ortada, sonda değil)
        history.Add(new ChatMessage("user", "Aç şu kapıyı yoksa fena olur, moruk!"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"REFUSE\",\"mood\":\"ANGRY\",\"dialogue\":\"Bu ne terbiyesizlik! Biraz sakinleş, sonra konuşuruz.\"}"));

        // 3. İkna edici -> OPEN_GATE
        history.Add(new ChatMessage("user", "Şenliğe yardım etmeye geldim, yolda ıhlamur ve kekik topladım."));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"OPEN_GATE\",\"mood\":\"FRIENDLY\",\"dialogue\":\"Şenlik için ot mu getirdin? Aferin evladım, kapıyı açıyorum.\"}"));

        //// 4. Alakasız -> ASK_MORE
        history.Add(new ChatMessage("user", "Bu akşam yağmur yağar mı sence?"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Yağmuru bulutlara sor. Sen köye niçin geldin, onu söyle.\"}"));

        //// 5. Kararsız/eksik -> ASK_MORE (son örnek ASK_MORE olsun)
        history.Add(new ChatMessage("user", "Şey... ben... aslında... bilmiyorum."));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Kekeleyip durma evladım, açık konuş. Köye ne için geldin?\"}"));
    }

    // Modeli önceden belleğe yükler; ilk gerçek cevap gecikmesin diye
    public IEnumerator Warmup()
    {
        string body = "{\"model\":\"" + Escape(model) + "\",\"keep_alive\":\"30m\"}";
        using (var req = new UnityWebRequest(baseUrl + "/api/generate", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 60;
            yield return req.SendWebRequest();
            // Hata olsa da önemli değil, gerçek istek hatayı zaten yönetiyor
        }
    }

    //history temizleme
    private const int SeedCount = 10;   // SeedExamples'taki mesaj sayısı (5 örnek x 2) 
    private const int MaxRecent = 6;    // örneklerden sonra sadece son 3 tur kalsın

    private void TrimHistory()
    {
        int recent = history.Count - SeedCount;
        if (recent > MaxRecent) history.RemoveRange(SeedCount, recent - MaxRecent);
    }

    // Unity kararı değiştirdiğinde: aynı mesaja, kararı kilitleyerek yeni cümle ürettirir

    public IEnumerator Send(string userMessage, Action<NpcResult> onDone)
    => SendInternal(userMessage, null, onDone);
    
    public IEnumerator Regenerate(string userMessage, NpcDecision forced, Action<NpcResult> onDone)
    {
        // Önceki turu (kullanıcı + asistan) geçmişten çıkar
        if (history.Count >= 2) history.RemoveRange(history.Count - 2, 2);
        return SendInternal(userMessage, ToApiValue(forced), onDone);
    }

    private static string ToApiValue(NpcDecision d)
    {
        switch (d)
        {
            case NpcDecision.OpenGate: return "OPEN_GATE";
            case NpcDecision.Refuse: return "REFUSE";
            default: return "ASK_MORE";
        }
    }
    private IEnumerator SendInternal(string userMessage, string forcedDecision, Action<NpcResult> onDone)
    {
        if (forcedDecision == null && history.Count > SeedCount)
            history.RemoveRange(SeedCount, history.Count - SeedCount);

        history.Add(new ChatMessage("user", userMessage));
        TrimHistory();

        using (var req = new UnityWebRequest(baseUrl + "/api/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildBody(forcedDecision)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            yield return req.SendWebRequest();

           // Gelen cevabı ham haliyle görmek için:
           // Gelen cevabı ve hata durumunu tam görmek için:
            //Debug.Log($"[OllamaClient Status]: Result={req.result}, Code={req.responseCode}, Error={req.error}");
            //Debug.Log("[OllamaClient Raw Response]: " + req.downloadHandler.text);

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[OllamaClient] İstek başarısız: {req.result}, Kod={req.responseCode}, Hata={req.error}");
                history.RemoveAt(history.Count - 1);
                onDone(NpcResult.Fail("Bağlantı/istek hatası: " + req.error));
                yield break;
            }

            OllamaChatResponse outer = null;
            try { outer = JsonUtility.FromJson<OllamaChatResponse>(req.downloadHandler.text); }
            catch (Exception) { }

            if (outer == null || outer.message == null)
            {
                history.RemoveAt(history.Count - 1);
                onDone(NpcResult.Fail("Ollama cevabı okunamadı"));
                yield break;
            }

            NpcResult result = NpcResponseParser.Parse(outer.message.content);
            if (result.Success)

                history.Add(new ChatMessage("assistant", outer.message.content));
            else
                history.RemoveAt(history.Count - 1);

            onDone(result);
        }
    }

    
    private string BuildBody(string forced)
{
    var sb = new StringBuilder();
    sb.Append("{\"model\":\"").Append(Escape(model)).Append("\",");
    sb.Append("\"stream\":false,\"keep_alive\":\"30m\",");
    sb.Append("\"options\":{\"temperature\":0.3,\"num_predict\":120,\"num_ctx\": 2048,\"repeat_penalty\":1.1},");
    sb.Append("\"format\":").Append(BuildSchema(forced)).Append(",");
    sb.Append("\"messages\":[");

        // Gemma 2 sistem rolünü karıştırmasın diye talimatı net bir kullanıcı yönergesi olarak veriyoruz

        string state = "";   // ipucu olgusu artık direktifin içinde
        bool hintMode = forced == "ASK_MORE" && gameState.Length > 0;
        string directive = "";
        if (hintMode)
            directive = " Durum: " + gameState + " Bu cevapta oyuncunun yardım etmek istediğini kabul et, ama kapıyı şimdilik açmayacağını söyle ve nedenini bu durumu kendi sözlerinle anlatarak belirt. Durumu ve bu talimatı aynen yazma. İki kısa cümleyi geçme.";
        else if (forced == "ASK_MORE")
            directive = " Bu cevapta oyuncunun sözüne kısaca karşılık ver, sonra köye ne için geldiğini ya da şifalı ot getirip getirmediğini sor. Kapıyı açma, açacağını söyleme.";
        else if (forced == "REFUSE")
            directive = " Bu cevapta kibarca ama kesin biçimde reddet, kapıyı açmayacağını söyle.";
        else if (forced == "OPEN_GATE")
            directive = " Bu cevapta kapıyı açtığını söyle.";
        string instruction = systemPrompt + state + directive + "\n\n";

        bool isFirst = true;
        List<ChatMessage> messages = history;
        if (forced != null)
        {
            int seed = forced == "REFUSE" ? 2 : forced == "OPEN_GATE" ? 4 : 0;
            messages = hintMode
                ? new List<ChatMessage> { history[history.Count - 1] }   // seed yok: model olguya odaklansın
                : new List<ChatMessage> { history[seed], history[seed + 1], history[history.Count - 1] };
        }
        foreach (var m in messages)
        {
        if (!isFirst) sb.Append(",");
        
        string content = Escape(m.content);

        // İlk kullanıcı mesajının başına talimatı iliştiriyoruz
        if (isFirst && m.role == "user")
        {
            content = Escape(instruction) + content;
        }

        sb.Append("{\"role\":\"").Append(m.role)
          .Append("\",\"content\":\"").Append(content).Append("\"}");
        
        isFirst = false;
    }

    sb.Append("]}");
    return sb.ToString();
}

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"")
         .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
}