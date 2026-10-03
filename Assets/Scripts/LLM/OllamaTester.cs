using UnityEngine;

public class OllamaTester : MonoBehaviour
{
    [SerializeField] private OllamaClient client;
    [SerializeField] private VillageGate villageGate;
    [TextArea] public string testMessage;

    private void Start()
    {
        if (client == null)
        {
            Debug.LogError("[OllamaTester] Client referansı atanmamış!");
            return;
        }

        Debug.Log("[OllamaTester] Mesaj gönderiliyor...");

        // IEnumerator olduğu için StartCoroutine ile başlatılmalı!
        StartCoroutine(client.Send(testMessage, result =>
        {
            Debug.Log($"[OllamaTester] Karar: {result.Decision} | Ruh Hali: {result.Mood} | Diyalog: {result.Dialogue}");

            if (result.Decision == NpcDecision.OpenGate)
            {
                Debug.Log("[OllamaTester] Karar OpenGate geldi, kapı Interact çağrılıyor!");
                if (villageGate != null)
                {
                    villageGate.Interact();
                }
                else
                {
                    Debug.LogError("[OllamaTester] VillageGate referansı boş!");
                }
            }
            else
            {
                Debug.LogWarning($"[OllamaTester] Kapı açılmadı. Karar: {result.Decision}");
            }
        }));
    }
}