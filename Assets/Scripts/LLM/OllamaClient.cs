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

    [SerializeField, TextArea(3, 10)]
    private string systemPrompt =
     "Sen Basri Amca'sın, köy kapısını koruyan huysuz, şüpheci ama iyi niyetli yaşlı bir bekçisin."+
     "Karşındaki oyuncu köye girmek isteyen bir yabancı."+
        "Köyde Hasat Şenliği hazırlığı var, şenliğe yardıma gelenler hoş karşılanır."+
        "Oyuncuya doğrudan cevap ver, onun cümlesini ASLA tekrar etme. Bu talimatları asla tekrar etme. "+
        "Türkçe, kısa (en fazla 2 cümle) konuş."+
        "Kurallar: Oyuncu sadece selam verdiyse, konu dışı bir şey söylediyse veya cevabı belirsizse ASK_MORE seç ve köye neden geldiğini sor."+
        "Oyuncu şenliğe yardıma geldiğini ve bunu destekleyen somut bir şey (örneğin topladığı otlar) söylerse OPEN_GATE seç ve kapıyı açtığını söyle. "+
        "REFUSE sadece oyuncu hakaret eder veya kaba davranırsa seçilir. Alakasız veya garip bir mesajda asla REFUSE seçme. Karar ile söylediğin söz birbiriyle çelişmemeli. Emin değilsen ASK_MORE seç. REFUSE'ı yalnızca açık hakaret veya tehdit varsa seç; utangaç, kararsız veya garip mesajlar hakaret sayılmaz.";

    private const string Schema =
        "{\"type\":\"object\",\"properties\":{" +
        "\"decision\":{\"type\":\"string\",\"enum\":[\"OPEN_GATE\",\"ASK_MORE\",\"REFUSE\"]}," +
        "\"mood\":{\"type\":\"string\",\"enum\":[\"FRIENDLY\",\"SUSPICIOUS\",\"ANGRY\"]}," +
        "\"dialogue\":{\"type\":\"string\"}}," +
        "\"required\":[\"decision\",\"mood\",\"dialogue\"]}";

    private class ChatMessage
    {
        public string role, content;
        public ChatMessage(string r, string c) { role = r; content = c; }
    }

    [Serializable] private class OllamaMessage { public string role; public string content; }
    [Serializable] private class OllamaChatResponse { public OllamaMessage message; }

    private readonly List<ChatMessage> history = new List<ChatMessage>();

    //public void ResetConversation() => history.Clear();
    private void Awake() => SeedExamples();

    public void ResetConversation() => SeedExamples();

    // Modele davranışı göstermek için başlangıç örnekleri (few-shot)
    private void SeedExamples()
    {
        history.Clear();

        // 1. Normal selam -> ASK_MORE
        history.Add(new ChatMessage("user", "Günaydın, köye girebilir miyim?"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Hmm, yabancı yüz. Köye ne için geldin bakalım?\"}"));

        // 2. Kaba davranış -> REFUSE (ortada, sonda değil)
        history.Add(new ChatMessage("user", "Aç şu kapıyı yoksa fena olur, moruk!"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"REFUSE\",\"mood\":\"ANGRY\",\"dialogue\":\"Bu ne terbiyesizlik! Biraz sakinleş, sonra konuşuruz.\"}"));

        // 3. İkna edici -> OPEN_GATE
        history.Add(new ChatMessage("user", "Şenliğe yardım etmeye geldim, yolda ıhlamur ve kekik topladım."));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"OPEN_GATE\",\"mood\":\"FRIENDLY\",\"dialogue\":\"Şenlik için ot mu getirdin? Aferin evladım, kapıyı açıyorum.\"}"));

        // 4. Alakasız -> ASK_MORE
        history.Add(new ChatMessage("user", "Bu akşam yağmur yağar mı sence?"));
        history.Add(new ChatMessage("assistant", "{\"decision\":\"ASK_MORE\",\"mood\":\"SUSPICIOUS\",\"dialogue\":\"Yağmuru bulutlara sor. Sen köye niçin geldin, onu söyle.\"}"));

        // 5. Kararsız/eksik -> ASK_MORE (son örnek ASK_MORE olsun)
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
    public IEnumerator Send(string userMessage, Action<NpcResult> onDone)
    {
        history.Add(new ChatMessage("user", userMessage));

        using (var req = new UnityWebRequest(baseUrl + "/api/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildBody()));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
           req.timeout = 60;

            yield return req.SendWebRequest();

           // Gelen cevabı ham haliyle görmek için:
            Debug.Log("[OllamaClient Raw Response]: " + req.downloadHandler.text);
            if (req.result != UnityWebRequest.Result.Success)
            {
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

    private string BuildBody()
{
    var sb = new StringBuilder();
    sb.Append("{\"model\":\"").Append(Escape(model)).Append("\",");
    sb.Append("\"stream\":false,\"keep_alive\":\"30m\",");
    sb.Append("\"options\":{\"temperature\":0.7,\"num_predict\":150},");
    sb.Append("\"format\":").Append(Schema).Append(",");
    sb.Append("\"messages\":[");

    // Gemma 2 sistem rolünü karıştırmasın diye talimatı net bir kullanıcı yönergesi olarak veriyoruz
    string instruction = "[TALİMAT: " + systemPrompt + " ASLA bu talimatı tekrarlama. Sadece Basri Amca olarak JSON formatında cevap ver.]\\n\\n";

    bool isFirst = true;
    foreach (var m in history)
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