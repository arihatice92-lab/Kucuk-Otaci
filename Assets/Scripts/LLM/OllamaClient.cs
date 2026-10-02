using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour, ILlmClient
{
    [SerializeField] private string baseUrl = "http://localhost:11434";
    [SerializeField] private string model = "gemma2:2b";
    [SerializeField] private int timeoutSeconds = 30;
    [SerializeField, TextArea(6, 14)]
    private string systemPrompt =
        "Sen Basri Amca'sın, köy kapısını koruyan huysuz ama iyi niyetli bir bekçisin.";

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

    public void ResetConversation() => history.Clear();

    public IEnumerator Send(string userMessage, Action<NpcResult> onDone)
    {
        history.Add(new ChatMessage("user", userMessage));

        using (var req = new UnityWebRequest(baseUrl + "/api/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildBody()));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            yield return req.SendWebRequest();

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
        sb.Append("\"options\":{\"temperature\":0.3},");
        sb.Append("\"format\":").Append(Schema).Append(",");
        sb.Append("\"messages\":[");
        sb.Append("{\"role\":\"system\",\"content\":\"").Append(Escape(systemPrompt)).Append("\"}");
        foreach (var m in history)
            sb.Append(",{\"role\":\"").Append(m.role)
              .Append("\",\"content\":\"").Append(Escape(m.content)).Append("\"}");
        sb.Append("]}");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"")
         .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
}