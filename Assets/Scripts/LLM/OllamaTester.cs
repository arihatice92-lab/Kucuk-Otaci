using System.Collections;
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
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Debug.Log("[OllamaTester] Mesaj gönderiliyor...");
        float baslangic = Time.realtimeSinceStartup;

        NpcResult result = null;
        yield return client.Send(testMessage, r => result = r);

        if (!result.Success)
        {
            Debug.LogWarning($"[OllamaTester] HATA: {result.Error} (Süre: {Time.realtimeSinceStartup - baslangic:F1} sn)");
            yield break;
        }

        NpcDecision hamKarar = result.Decision;
        result = NpcDecisionValidator.Validate(testMessage, result);

        bool yenidenUretildi = false;
        if (result.DecisionOverridden)
        {
            NpcDecision finalKarar = result.Decision;
            NpcMood finalMood = result.Mood;
            NpcResult yeni = null;
            yield return client.Regenerate(testMessage, finalKarar, r => yeni = r);

            if (yeni != null && yeni.Success)
            {
                yeni.Decision = finalKarar;
                yeni.Mood = finalMood;
                result = yeni;
                yenidenUretildi = true;
            }
            else
            {
                result = NpcResult.Fail("Cümle yeniden üretilemedi");
            }
        }

        float sure = Time.realtimeSinceStartup - baslangic;
        Debug.Log($"[OllamaTester] HAM: {hamKarar} | FINAL: {result.Decision} | Mood: {result.Mood} | " +
                  $"Yeniden üretildi: {yenidenUretildi} | Diyalog: {result.Dialogue} | Süre: {sure:F1} sn");

        if (result.Decision == NpcDecision.OpenGate)
        {
            if (villageGate != null) villageGate.Interact();
            else Debug.LogError("[OllamaTester] VillageGate referansı boş!");
        }
        else
        {
            Debug.LogWarning($"[OllamaTester] Kapı açılmadı. Karar: {result.Decision}");
        }
    }
}