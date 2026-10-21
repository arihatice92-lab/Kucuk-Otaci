using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sorumluluk: Sahneler arası geçişte ekranı karartıp sahneyi yükler, sonra ekranı açar.
/// Oyun başlarken kendisi oluşur ve sahneler arasında kalır; sahneye eklenmesi gerekmez.
/// Dışarıdan yalnızca SceneLoader.Load üzerinden kullanılır.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const float FadeSeconds = 0.8f;

    private static SceneTransition instance;

    private CanvasGroup fade;
    private bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var host = new GameObject("SceneTransition");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SceneTransition>();
    }

    private void Awake()
    {
        BuildOverlay();
    }

    public static void Load(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[Geçiş] '{sceneName}' sahnesi Build Profiles listesinde yok.");
            return;
        }

        if (instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        instance.Begin(sceneName);
    }

    private void Begin(string sceneName)
    {
        if (loading) return; // geçiş sürerken ikinci istek yok sayılır
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        loading = true;
        fade.blocksRaycasts = true;

        yield return Fade(0f, 1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        Time.timeScale = 1f; // duraklatılmış bir sahneden gelinse bile yeni sahne akar

        yield return Fade(1f, 0f);

        fade.blocksRaycasts = false;
        loading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float time = 0f;
        while (time < FadeSeconds)
        {
            time += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / FadeSeconds));
            yield return null;
        }

        fade.alpha = to;
    }

    private void BuildOverlay()
    {
        var canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        fade = canvasObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        fade.blocksRaycasts = false;
        fade.interactable = false;

        var imageObject = new GameObject("Black");
        imageObject.transform.SetParent(canvasObject.transform, false);

        var image = imageObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}