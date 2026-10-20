using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sorumluluk: Ara sahne boyunca oyuncu kontrolünü, kamera takibini ve oyun arayüzünü kapatır,
/// ara sahne bitince eski haline getirir. Ara sahnenin nasıl oynadığını bilmez.
/// </summary>
public class IntroGameplayLock : MonoBehaviour
{
    [SerializeField] private IntroController intro;

    [Header("Ara sahne boyunca kapatılacak bileşenler")]
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Ara sahne boyunca gizlenecek nesneler (HUD, diyalog, komut girişi)")]
    [SerializeField] private GameObject[] hideDuringIntro;

    private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
    private readonly List<GameObject> hiddenObjects = new List<GameObject>();

    private void OnEnable()
    {
        if (intro == null)
        {
            Debug.LogError("IntroGameplayLock: Intro atanmamış.", this);
            return;
        }

        intro.Started += Lock;
        intro.Finished += Unlock;
    }

    private void OnDisable()
    {
        if (intro == null) return;

        intro.Started -= Lock;
        intro.Finished -= Unlock;
    }

    private void Lock()
    {
        DisableBehaviour(cameraFollow);
        DisableBehaviour(playerMovement);
        DisableBehaviour(playerInteractor);

        foreach (GameObject go in hideDuringIntro)
        {
            if (go == null || !go.activeSelf) continue;

            go.SetActive(false);
            hiddenObjects.Add(go);
        }
    }

    private void Unlock()
    {
        foreach (Behaviour behaviour in disabledBehaviours)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        disabledBehaviours.Clear();

        foreach (GameObject go in hiddenObjects)
        {
            if (go != null) go.SetActive(true);
        }
        hiddenObjects.Clear();
    }

    private void DisableBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled) return;

        behaviour.enabled = false;
        disabledBehaviours.Add(behaviour);
    }
}