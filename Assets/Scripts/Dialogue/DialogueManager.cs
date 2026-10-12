using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{    [Header("UI")]
    [SerializeField] private TMP_Text npcText;
    [SerializeField] private TMP_InputField playerInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private GameObject thinkingIndicator;

    [Header("Bağlantılar")]
    [SerializeField] private OllamaClient client;
    [SerializeField] private NpcController npcController;

    [Header("Görevler")]
    [SerializeField] private QuestManager quests;

    private bool isWaiting;

    private void Start()
    {
        thinkingIndicator.SetActive(false);
        playerInput.characterLimit = 200;
        Application.targetFrameRate = 30;
        StartCoroutine(client.Warmup());
    }

    private void OnEnable()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        playerInput.onSubmit.AddListener(OnInputSubmitted);
    }

    private void OnDisable()
    {
        sendButton.onClick.RemoveListener(OnSendClicked);
        playerInput.onSubmit.RemoveListener(OnInputSubmitted);
    }

    private void OnInputSubmitted(string _) => OnSendClicked();

    private void OnSendClicked()
    {
        if (isWaiting) return;

        string text = playerInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (!npcController.CanTalk)
        {
            npcText.text = $"[Basri Amca şu an seninle konuşmak istemiyor. ({Mathf.CeilToInt(npcController.RemainingCooldown)} sn)]";
            return;
        }

        StartCoroutine(SendRoutine(text));
    }

    private IEnumerator SendRoutine(string text)
    {
        SetWaiting(true);
        playerInput.text = "";
        float start = Time.realtimeSinceStartup;


        NpcResult result = null;
        yield return client.Send(text, r => result = r);

        NpcDecision hamKarar = result.Decision;
        bool yenidenUretildi = false;
        

        result = NpcDecisionValidator.Validate(
            text,
            result,
            true,
            quests.AllCompleted //test aşamasında true girilebilir, girilmezse aşamalar tamamlanmadığı için kapı hiç açılmıyor.
        );

        // Unity kararı değiştirdiyse VEYA cümle karakter dışıysa (prompt sızıntısı) LLM cümleyi yeniden üretsin
        bool outOfCharacter = result.Success && NpcDecisionValidator.LooksOutOfCharacter(result.Dialogue) ||
     NpcDecisionValidator.ContradictsDecision(result.Decision, result.Dialogue) ||
     client.IsSeedCopy(result.Dialogue);

        if (result.Success && (result.DecisionOverridden || outOfCharacter ))
        {
            NpcDecision finalDecision = result.Decision;
            NpcMood finalMood = result.Mood;

            NpcResult regenerated = null;
            bool ok = false;

            for (int attempt = 0; attempt < 2 && !ok; attempt++)
            {
                NpcResult r2 = null;
                yield return client.Regenerate(text, finalDecision, r => r2 = r);

                ok = r2 != null && r2.Success && !NpcDecisionValidator.LooksOutOfCharacter(r2.Dialogue) && !NpcDecisionValidator.ContradictsDecision(finalDecision, r2.Dialogue) && !client.IsSeedCopy(r2.Dialogue);
                if (ok) regenerated = r2;
                else Debug.LogWarning($"[Dialogue] Yeniden üretim geçersiz ya da karakter dışı (deneme {attempt + 1}/2)");
            }

            if (ok)
            {
                regenerated.Decision = finalDecision;
                regenerated.Mood = finalMood;
                regenerated.Validated = true;
                result = regenerated;
                yenidenUretildi = true;
            }
            else
            {
                result = NpcResult.Fail("Cümle yeniden üretilemedi");
            }
        }

        Debug.Log($"[Dialogue] HAM: {hamKarar} | FINAL: {result.Decision} | Yeniden üretildi: {yenidenUretildi} | " +
                  $"Süre: {Time.realtimeSinceStartup - start:F1} sn | Diyalog ({result.Dialogue?.Length ?? 0} krk): \"{result.Dialogue}\"");
        npcText.text = result.Dialogue;
        npcController.Handle(result);

        SetWaiting(false);
    }

    private void SetWaiting(bool waiting)
    {
        isWaiting = waiting;
        thinkingIndicator.SetActive(waiting);
        sendButton.interactable = !waiting;
        playerInput.interactable = !waiting;
        if (!waiting) playerInput.ActivateInputField();
    }
}
