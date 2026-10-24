using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sorumluluk: Görev tamamlanınca ekranın üstünde kısa bildirim gösterir; tüm görevler bitince
/// "Basri Amca ile konuş" uyarısını gösterir. Görev durumunu kendisi tutmaz, QuestManager olaylarını dinler.
/// Mesajlar sırayla gösterilir, üst üste binmez.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class QuestNotifier : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    [Header("Süreler (sn)")]
    [SerializeField] private float questSeconds = 3f;
    [SerializeField] private float allDoneSeconds = 6f;
    [SerializeField] private float fadeSeconds = 0.4f;

    [Header("Metinler")]
    [SerializeField] private string questDonePrefix = "Görev tamamlandı: ";
    [SerializeField] private string allDoneMessage = "Tüm görevler tamamlandı! Basri Amca ile konuş.";

    private readonly Queue<KeyValuePair<string, float>> queue = new Queue<KeyValuePair<string, float>>();

    private CanvasGroup group;
    private QuestManager quests;
    private bool playing;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (messageText == null) messageText = GetComponent<TMP_Text>();

        SetAlpha(0f);
    }

    private void Start()
    {
        quests = QuestManager.Instance;

        if (messageText == null || quests == null)
        {
            Debug.LogError("QuestNotifier: Metin kutusu atanmamış ya da sahnede QuestManager yok.", this);
            enabled = false;
            return;
        }

        quests.OnQuestCompleted += HandleQuestCompleted;
        quests.OnAllQuestsCompleted += HandleAllCompleted;
    }

    private void OnDestroy()
    {
        if (quests == null) return;

        quests.OnQuestCompleted -= HandleQuestCompleted;
        quests.OnAllQuestsCompleted -= HandleAllCompleted;
    }

    private void HandleQuestCompleted(QuestId id) =>
        Enqueue(questDonePrefix + QuestTitles.Of(id), questSeconds);

    private void HandleAllCompleted() =>
        Enqueue(allDoneMessage, allDoneSeconds);

    private void Enqueue(string message, float seconds)
    {
        // Nesne kapalıyken (ara sahne, bitiş sekansı) coroutine başlatılamaz.
        if (!isActiveAndEnabled) return;

        queue.Enqueue(new KeyValuePair<string, float>(message, seconds));
        if (!playing) StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        playing = true;

        while (queue.Count > 0)
        {
            KeyValuePair<string, float> item = queue.Dequeue();
            messageText.text = item.Key;

            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(item.Value);
            yield return Fade(1f, 0f);
        }

        playing = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        while (time < fadeSeconds)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(time / fadeSeconds)));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        group.alpha = alpha;
        group.blocksRaycasts = false; // bildirim tıklamaları engellemesin
        group.interactable = false;
    }
}