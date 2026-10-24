using System.Collections;
using UnityEngine;

/// <summary>
/// Sorumluluk: Ara sahne bitince (ya da oyun başlayınca) kontrol tuşları panelini birkaç saniye
/// gösterip yavaşça kapatır. Hangi tuşların yazılı olduğunu bilmez, panelin içeriği sahnede düzenlenir.
/// </summary>
public class ControlsHint : MonoBehaviour
{
    [Tooltip("Boş bırakılırsa panel oyun başlar başlamaz gösterilir.")]
    [SerializeField] private IntroController intro;
    [SerializeField] private CanvasGroup panel;

    [Header("Süreler (sn)")]
    [SerializeField] private float showSeconds = 10f;
    [SerializeField] private float fadeSeconds = 0.6f;

    private bool shown;

    private void Awake()
    {
        if (panel == null)
        {
            Debug.LogError("ControlsHint: Panel (CanvasGroup) atanmamış.", this);
            enabled = false;
            return;
        }

        SetPanel(0f);
    }

    private void OnEnable()
    {
        if (intro != null) intro.Finished += Show;
    }

    private void OnDisable()
    {
        if (intro != null) intro.Finished -= Show;
    }

    private void Start()
    {
        if (intro == null) Show();
    }

    private void Show()
    {
        if (shown) return; // ara sahne bir kez biter, panel de bir kez açılır
        shown = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(showSeconds);
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        while (time < fadeSeconds)
        {
            time += Time.unscaledDeltaTime;
            SetPanel(Mathf.Lerp(from, to, Mathf.Clamp01(time / fadeSeconds)));
            yield return null;
        }

        SetPanel(to);
    }

    private void SetPanel(float alpha)
    {
        panel.alpha = alpha;
        panel.blocksRaycasts = false; // panel tıklamaları engellemesin
        panel.interactable = false;
    }
}