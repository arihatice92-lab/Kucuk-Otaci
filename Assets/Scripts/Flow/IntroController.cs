using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroController : MonoBehaviour
{
    [SerializeField] private Camera introCamera;
    [SerializeField] private TypewriterText subtitle;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private IntroShot[] shots;
    [SerializeField] private float fadeDuration = 1f;

    private bool finished;

    private IEnumerator Start()
    {
        if (!IsConfigured())
        {
            Debug.LogError("[Intro] Ayarlar eksik, ara sahne atlanıyor.");
            Finish();
            yield break;
        }

        foreach (IntroShot shot in shots)
        {
            yield return PlayShot(shot);
        }

        Finish();
    }

    private void Update()
    {
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
        SceneLoader.Load(SceneLoader.Village);
    }
}