using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sorumluluk: Oyuncu meydanın ortasına varınca konfetiyi patlatır, final yazısını ve
/// Tekrar Oyna / Çıkış panelini gösterir. Tetikleyicinin nasıl çalıştığını bilmez, olayını dinler.
/// </summary>
public class FestivalEndingController : MonoBehaviour
{
    [SerializeField] private PlazaCenterTrigger trigger;
    [SerializeField] private ParticleSystem confetti;

    [Header("Oyuncu (bitişte hareketi durdurulur)")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Final paneli")]
    [SerializeField] private CanvasGroup endPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button quitButton;
    [TextArea]
    [SerializeField] private string message = "Hasat Şenliği başladı! Köyü kurtardın.";

    [Header("Süreler (sn)")]
    [SerializeField] private float panelDelay = 2f;
    [SerializeField] private float fadeSeconds = 1f;

    private bool configured;

    private void Awake()
    {
        configured = trigger != null && confetti != null && endPanel != null
                     && messageText != null && replayButton != null && quitButton != null;

        if (!configured)
        {
            Debug.LogError("FestivalEndingController: Inspector'daki kutulardan biri boş, bitiş çalışmayacak.", this);
            return;
        }

        messageText.text = message;
        HidePanel();

        replayButton.onClick.AddListener(Replay);
        quitButton.onClick.AddListener(Quit);
    }

    private void OnEnable()
    {
        if (trigger != null) trigger.Reached += HandleReached;
    }

    private void OnDisable()
    {
        if (trigger != null) trigger.Reached -= HandleReached;
    }

    private void OnDestroy()
    {
        if (!configured) return;

        replayButton.onClick.RemoveListener(Replay);
        quitButton.onClick.RemoveListener(Quit);
    }

    private void HandleReached()
    {
        if (!configured) return;

        if (playerMovement != null) playerMovement.enabled = false;
        confetti.Play();
        StartCoroutine(ShowPanel());
    }

    private IEnumerator ShowPanel()
    {
        yield return new WaitForSeconds(panelDelay);

        // Fare imleci menüdeki gibi görünür olsun.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        endPanel.blocksRaycasts = true;
        endPanel.interactable = true;

        float time = 0f;
        while (time < fadeSeconds)
        {
            time += Time.deltaTime;
            endPanel.alpha = Mathf.Clamp01(time / fadeSeconds);
            yield return null;
        }

        endPanel.alpha = 1f;
    }

    private void HidePanel()
    {
        endPanel.alpha = 0f;
        endPanel.blocksRaycasts = false;
        endPanel.interactable = false;
    }

    private void Replay()
    {
        replayButton.interactable = false; // çift tıklamayı engelle
        SceneLoader.Load(SceneLoader.MainMenu);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}