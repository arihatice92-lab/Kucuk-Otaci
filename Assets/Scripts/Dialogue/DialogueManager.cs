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

    // Unity arayüz tipli alanları Inspector'da göstermez. Alan adları değişmediği için sahnedeki
    // atamalar korunur. Alanlar Awake'te arayüze çevrilir (ILlmClient, IQuestProgress).
    [Header("Bağlantılar")]
    [Tooltip("ILlmClient uygulayan bileşen (ör. OllamaClient)")]
    [SerializeField] private MonoBehaviour client;
    [SerializeField] private NpcController npcController;

    [Header("Görevler")]
    [Tooltip("IQuestProgress uygulayan bileşen (ör. QuestManager)")]
    [SerializeField] private MonoBehaviour quests;

    private ILlmClient llm;
    private IQuestProgress progress;
    private bool isWaiting;

    private void Awake()
    {
        llm = client as ILlmClient;
        progress = quests as IQuestProgress;

        if (llm == null || progress == null)
        {
            Debug.LogError("[DialogueManager] 'client' ILlmClient, 'quests' IQuestProgress uygulamıyor ya da atanmamış. Diyalog devre dışı.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        thinkingIndicator.SetActive(false);
        playerInput.characterLimit = 200;
        StartCoroutine(llm.Warmup());
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
        yield return llm.Send(text, r => result = r);

        NpcDecision hamKarar = result.Decision;
        bool yenidenUretildi = false;
        

        result = NpcDecisionValidator.Validate(
            text,
            result,
            progress.IsCompleted(QuestId.HerbsCollected),
            progress.AllCompleted
        );

        // Kapıyı yalnızca eksik görevler engelliyorsa: eksik işin olgusunu LLM'ye ver, ipucu cümlesini o kursun
        string hintFact = null;
        string[] hintKeywords = null;
        bool needsHint = result.Success && result.DeniedByMissingQuests
                         && progress.TryGetMissingHint(out hintFact, out hintKeywords);

        // Unity kararı değiştirdiyse VEYA cümle karakter dışıysa (prompt sızıntısı) LLM cümleyi yeniden üretsin
        bool outOfCharacter = result.Success &&
            (NpcDecisionValidator.LooksOutOfCharacter(result.Dialogue) ||
             NpcDecisionValidator.ContradictsDecision(result.Decision, result.Dialogue) ||
             llm.IsSeedCopy(result.Dialogue));

        if (result.Success && (result.DecisionOverridden || outOfCharacter || needsHint))
        {
            NpcDecision finalDecision = result.Decision;
            NpcMood finalMood = result.Mood;
            NpcResult original = result;

            NpcResult regenerated = null;
            bool ok = false;
            NpcResult fallback = null;   // ipucu dışındaki tüm kontrolleri geçen son cümle

            if (needsHint) llm.SetGameState(hintFact);
            Debug.Log($"[Dialogue] İpucu modu: {needsHint} | Olgu: {hintFact}");

            for (int attempt = 0; attempt < 2 && !ok; attempt++)
            {
                NpcResult r2 = null;
                yield return llm.Regenerate(text, finalDecision, r => r2 = r);

                string d2 = r2 != null ? r2.Dialogue : null;
                bool valid = r2 != null && r2.Success
                             && !NpcDecisionValidator.LooksOutOfCharacter(d2)
                             && !NpcDecisionValidator.ContradictsDecision(finalDecision, d2)
                             && !llm.IsSeedCopy(d2);
                bool hasHint = !needsHint || MentionsAny(d2, hintKeywords);
                ok = valid && hasHint;

                if (ok) regenerated = r2;
                else
                {
                    if (valid) fallback = r2;
                    Debug.LogWarning($"[Dialogue] Reddedildi (deneme {attempt + 1}/2) | geçerli={valid} | ipucuYok={!hasHint} | Diyalog: \"{d2}\"");
                }
            }

            if (needsHint) llm.SetGameState("");
            if (!ok && fallback != null)
            {
                regenerated = fallback;
                ok = true;
                Debug.LogWarning("[Dialogue] İpucu üretilemedi, ipucusuz geçerli cümle kullanıldı.");
            }

            if (ok)
            {
                regenerated.Decision = finalDecision;
                regenerated.Mood = finalMood;
                regenerated.Validated = true;
                result = regenerated;
                yenidenUretildi = true;
            }
            else if (needsHint && !result.DecisionOverridden && !outOfCharacter)
            {
                // Yalnızca ipucu üretilemedi: orijinal (geçerli) LLM cümlesi kalır, kapı yine kilitli
                result = original;
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

    private static bool MentionsAny(string text, string[] keywords)
    {
        if (string.IsNullOrEmpty(text) || keywords == null) return false;
        string t = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        foreach (var k in keywords)
            if (t.Contains(k)) return true;
        return false;
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