using UnityEngine;

// LLM'den dönecek karar durumları 
public enum GateDecision
{
    Undecided,
    Denied,
    Open
}

public class NpcInteractionTrigger : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Açılacak köy kapısının VillageGate bileşeni")]
    [SerializeField] private VillageGate villageGate;

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        // Oyuncunun Basri Amca'nın alanına girip girmediğini kontrol et
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[Basri Amca] Oyuncu yaklaştı. Konuşma başlatılabilir.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[Basri Amca] Oyuncu alandan uzaklaştı.");
        }
    }

    // LLM'den "Open" kararı geldiğinde arkadaşının UI/LLM sistemi doğrudan bu metodu çağıracak
    public void HandleDecision(GateDecision decision)
    {
        if (decision == GateDecision.Open)
        {
            if (villageGate != null)
            {
                villageGate.OpenGate();
            }
            else
            {
                Debug.LogWarning("[Basri Amca] VillageGate referansı atanmamış!");
            }
        }
    }

    // Oyuncunun şu an konuşma mesafesinde olup olmadığını dışarıya bildirir
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }
}