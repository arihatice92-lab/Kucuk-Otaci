using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sorumluluk: Köy sahnesinin içinde kamera çekimlerini sırayla oynatır, altyazıyı yazar,
/// Esc ile atlamaya izin verir. Oyun mantığını kilitlemek IntroGameplayLock'un işidir;
/// bu sınıf yalnızca Started / Finished olaylarıyla haber verir.
/// </summary>
public class IntroController : MonoBehaviour
{
    [SerializeField] private Camera introCamera;
    [SerializeField] private TypewriterText subtitle;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private IntroShot[] shots;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Köyde oynarken")]
    [Tooltip("Geliştirirken kapatırsan ara sahne hiç oynamaz.")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Ara sahne Canvas'ını ve seslerini tutan nesne. Bittikten sonra kapatılır.")]
    [SerializeField] private GameObject introRoot;
    [SerializeField] private AudioSource[] audioToFadeOut;

    public event Action Started;
    public event Action Finished;

    private bool finished;

    private IEnumerator Start()
    {
        if (!playOnStart)
        {
            Close();
            yield break;
        }

        if (!IsConfigured())
        {
            Debug.LogError("[Intro] Ayarlar eksik, ara sahne atlanıyor.", this);
            Close();
            yield break;
        }

        fade.alpha = 1f;
        fade.blocksRaycasts = true;
        Started?.Invoke();

        foreach (IntroShot shot in shots)
        {
            yield return PlayShot(shot);
        }

        Finish();
    }

    private void Update()
    {
        if (finished) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StopAllCoroutines();
            Finish();
        }
    }

    private bool IsConfigured()
    {
        if (introCamera == null || subtitle == null || fade == null) return false;
        if (shots == null || shots.Length == 0) return false;

        foreach (IntroShot shot in shots)
        {
            if (shot == null || shot.CameraStart == null) return false;
        }
        return true;
    }

    private IEnumerator PlayShot(IntroShot shot)
    {
        Transform end = shot.CameraEnd != null ? shot.CameraEnd : shot.CameraStart;
        Coroutine typing = StartCoroutine(subtitle.Type(shot.Subtitle));

        float time = 0f;
        while (time < shot.Duration)
        {
            time += Time.deltaTime;

            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / shot.Duration));
            introCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(shot.CameraStart.position, end.position, k),
                Quaternion.Slerp(shot.CameraStart.rotation, end.rotation, k));

            float visible = Mathf.Min(
                Mathf.Clamp01(time / fadeDuration),
                Mathf.Clamp01((shot.Duration - time) / fadeDuration));
            fade.alpha = 1f - visible;

            yield return null;
        }

        StopCoroutine(typing);
        subtitle.Clear();
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        subtitle.Clear();
        Finished?.Invoke();           // oyuncu kontrolü ve arayüz geri gelir
        StartCoroutine(FadeOut());    // ekran siyahtan açılır, sesler kısılır
    }

    private IEnumerator FadeOut()
    {
        fade.alpha = 1f;

        float[] startVolumes = new float[audioToFadeOut != null ? audioToFadeOut.Length : 0];
        for (int i = 0; i < startVolumes.Length; i++)
        {
            if (audioToFadeOut[i] != null) startVolumes[i] = audioToFadeOut[i].volume;
        }

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / fadeDuration);

            fade.alpha = 1f - k;
            for (int i = 0; i < startVolumes.Length; i++)
            {
                if (audioToFadeOut[i] != null) audioToFadeOut[i].volume = startVolumes[i] * (1f - k);
            }

            yield return null;
        }

        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        Close();
    }

    private void Close()
    {
        finished = true;
        if (introRoot != null) introRoot.SetActive(false);
    }
}