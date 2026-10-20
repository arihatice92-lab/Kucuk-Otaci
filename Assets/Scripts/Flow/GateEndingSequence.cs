using System.Collections;
using UnityEngine;

/// <summary>
/// Sorumluluk: Kapı açılırken kamerayı kapıya çevirir, oyunu kilitler ve kapı açıldıktan
/// kısa süre sonra Festival sahnesine geçer. Kapının nasıl açıldığını bilmez, olaylarını dinler.
/// </summary>
public class GateEndingSequence : MonoBehaviour
{
    [SerializeField] private VillageGate gate;
    [SerializeField] private Camera gameCamera;
    [Tooltip("Kamerayı kapıya bakacak şekilde duran boş nesne (Align With View ile ayarlanır).")]
    [SerializeField] private Transform cameraPoint;
    [SerializeField] private IntroGameplayLock gameplayLock;

    [Header("Süreler (sn)")]
    [Tooltip("Basri Amca'nın son cümlesi okunsun diye kapı açılmaya başlayınca beklenir.")]
    [SerializeField] private float lookDelay = 1.5f;
    [SerializeField] private float cameraMoveSeconds = 1.5f;
    [Tooltip("Kapı tamamen açıldıktan sonra Festival'e geçmeden önce beklenir.")]
    [SerializeField] private float holdAfterOpenSeconds = 2f;

    private bool started;
    private bool opened;

    private void OnEnable()
    {
        if (gate == null || gameCamera == null || cameraPoint == null)
        {
            Debug.LogError("GateEndingSequence: Gate, Game Camera ya da Camera Point atanmamış.", this);
            return;
        }

        gate.OpeningStarted += HandleOpeningStarted;
        gate.Opened += HandleOpened;
    }

    private void OnDisable()
    {
        if (gate == null) return;

        gate.OpeningStarted -= HandleOpeningStarted;
        gate.Opened -= HandleOpened;
    }

    private void HandleOpeningStarted()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private void HandleOpened() => opened = true;

    private IEnumerator Run()
    {
        // Festival sahnesi yoksa oyunu kilitleme, kapı açık kalsın ve oyun devam etsin.
        if (!Application.CanStreamedLevelBeLoaded(SceneLoader.Festival))
        {
            Debug.LogError($"[Bitiş] '{SceneLoader.Festival}' sahnesi Build Profiles listesinde yok, bitiş atlanıyor.", this);
            yield break;
        }

        yield return new WaitForSeconds(lookDelay);

        if (gameplayLock != null) gameplayLock.Lock();
        yield return MoveCameraToGate();

        yield return new WaitUntil(() => opened);
        yield return new WaitForSeconds(holdAfterOpenSeconds);

        SceneLoader.Load(SceneLoader.Festival);
    }

    private IEnumerator MoveCameraToGate()
    {
        Transform cam = gameCamera.transform;
        Vector3 startPosition = cam.position;
        Quaternion startRotation = cam.rotation;

        float time = 0f;
        while (time < cameraMoveSeconds)
        {
            time += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / cameraMoveSeconds));
            cam.SetPositionAndRotation(
                Vector3.Lerp(startPosition, cameraPoint.position, k),
                Quaternion.Slerp(startRotation, cameraPoint.rotation, k));
            yield return null;
        }
    }

    // Test için: Play'deyken bileşenin başlığına sağ tıkla, "Test: Kapıyı aç" seç.
    // Basri Amca ile konuşmadan bitişi denemeni sağlar.
    [ContextMenu("Test: Kapıyı aç")]
    private void TestOpenGate()
    {
        if (gate != null) gate.OpenGate();
    }
}