using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI (arkadaşın kuracak)")]
    [SerializeField] private TMP_Text npcText;
    [SerializeField] private TMP_InputField playerInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private GameObject thinkingIndicator;

    [Header("Bağlantılar")]
    [SerializeField] private OllamaClient client;
    [SerializeField] private NpcController npcController;

    private bool isWaiting;

    private void Start()
    {
        thinkingIndicator.SetActive(false);
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
            npcText.text = $"Basri Amca seninle şu an konuşmak istemiyor. ({Mathf.CeilToInt(npcController.RemainingCooldown)} sn)";
            return;
        }

        StartCoroutine(SendRoutine(text));
    }

    private IEnumerator SendRoutine(string text)
    {
        SetWaiting(true);
        playerInput.text = "";

        NpcResult result = null;
        yield return client.Send(text, r => result = r);

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