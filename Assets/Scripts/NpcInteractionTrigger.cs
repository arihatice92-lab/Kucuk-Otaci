using UnityEngine;

public class NpcInteractionTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private VillageGate villageGate;
    [SerializeField] private OllamaClient ollamaClient;

    private bool isPlayerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("[Basri Amca]: Yanıma birisi geldi. Konuşmak için mesaj gönderebilirsin.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log("[Basri Amca]: Oyuncu uzaklaştı.");
        }
    }

    // Basri Amca ile konuşma fonksiyonu (UI veya test için çağrılır)
    public void TalkToBasri(string playerMessage)
    {   
        if (!isPlayerInRange)
{
       Debug.LogWarning("[Basri Amca]: Konuşmak için daha yakına gelmelisin.");
           return;
}

        if (ollamaClient == null)
        {
            Debug.LogError("OllamaClient atanmamış!");
            return;
        }

        Debug.Log($"[Oyuncu -> Basri Amca]: {playerMessage}");

        // Ollama'ya asenkron istek atılıyor
        StartCoroutine(ollamaClient.Send(playerMessage, OnNpcResponseReceived));
    }

    // Ollama'dan gelen cevabın işlendiği yer
    private void OnNpcResponseReceived(NpcResult result)
    {
        if (result == null || !result.Success)
        {
            Debug.LogWarning("[Basri Amca]: " + (result != null ? result.Dialogue : "Cevap alınamadı."));
            return;
        }

        // Konsola Basri Amca'nın cevabını ve ruh halini yazdır
        Debug.Log($"[Basri Amca ({result.Mood})]: {result.Dialogue}");

        // LLM kararına göre aksiyon al
        switch (result.Decision)
        {
            case NpcDecision.OpenGate:
                Debug.Log("[Sistem]: Basri Amca ikna oldu! Kapı açılıyor...");
                if (villageGate != null)
                {
                    villageGate.OpenGate();
                }
                break;

            case NpcDecision.Refuse:
                Debug.Log("[Sistem]: Basri Amca kapıyı açmayı kesin bir dille reddetti.");
                break;

            case NpcDecision.AskMore:
                Debug.Log("[Sistem]: Basri Amca henüz ikna olmadı, daha fazla bilgi istiyor.");
                break;
        }
    }

    public bool IsPlayerInRange() => isPlayerInRange;
}