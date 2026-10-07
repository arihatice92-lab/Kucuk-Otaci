using System;
using UnityEngine;

public class NpcController : MonoBehaviour
{
    [SerializeField] private float refuseCooldownSeconds = 15f;

    private float refusedUntil;

    // Kapıyı açan sınıf (VillageGate) buna abone olacak
    public event Action OnGateShouldOpen;
    public event Action<NpcResult> OnDecisionMade;

    public bool CanTalk => Time.time >= refusedUntil;
    public float RemainingCooldown => Mathf.Max(0f, refusedUntil - Time.time);

    public void Handle(NpcResult result)
    {
        if (result == null || !result.Success) return;

        switch (result.Decision)
        {
            case NpcDecision.OpenGate:
                if (!result.Validated)
                {
                    Debug.LogWarning("[NpcController] Doğrulanmamış OpenGate kararı yok sayıldı.");
                    break;
                }
                OnGateShouldOpen?.Invoke();
                break;
            case NpcDecision.Refuse:
                refusedUntil = Time.time + refuseCooldownSeconds;
                break;
            case NpcDecision.AskMore:
                break; // Diyalog devam ediyor, ekstra iş yok
        }

        OnDecisionMade?.Invoke(result);
    }
}