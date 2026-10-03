using UnityEngine;

public class OllamaTester : MonoBehaviour
{
    [SerializeField] private OllamaClient client;
    [SerializeField] private VillageGate villageGate;
    [TextArea] public string testMessage;

    private float baslangic;
    private void Start()
    {
        if (client == null)
        {
            Debug.LogError("[OllamaTester] Client referansı atanmamış!");
            return;
        }

        Debug.Log("[OllamaTester] Mesaj gönderiliyor...");

        baslangic = Time.realtimeSinceStartup;

        // IEnumerator olduğu için StartCoroutine ile başlatılmalı!
        StartCoroutine(client.Send(testMessage, result =>
        {
            float sure = Time.realtimeSinceStartup - baslangic;

            if (!result.Success)
            {
                Debug.LogWarning($"[OllamaTester] HATA: {result.Error} (Süre: {sure:F1} sn)");
                return;
            }
            result = NpcDecisionValidator.Validate(testMessage, result);
            Debug.Log($"[OllamaTester] Karar: {result.Decision} | Ruh Hali: {result.Mood} | Diyalog: {result.Dialogue} | Süre: {sure:F1} sn");

            if (result.Decision == NpcDecision.OpenGate)
            {
                if (villageGate != null)
                    villageGate.Interact();
                else
                    Debug.LogError("[OllamaTester] VillageGate referansı boş!");
            }
            else
            {
                Debug.LogWarning($"[OllamaTester] Kapı açılmadı. Karar: {result.Decision}");
            }
        }));
    }
}