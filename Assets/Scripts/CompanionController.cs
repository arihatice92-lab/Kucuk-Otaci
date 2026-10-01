using UnityEngine;
using UnityEngine.AI;
//Fındık yürürken burnundan ileriye doğru kırmızı bir çizgi çıktığını ve nesneleri gördüğünde alt kısımdaki Console paneline log düştüğü görülecek.
public enum CompanionState
{
    Idle,
    MovingToTarget,
    Interacting,
    ReturningToPlayer
}

[RequireComponent(typeof(NavMeshAgent))]
public class CompanionController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject targetObject;

    [Header("Sensör (Raycast) Ayarları")]
    [SerializeField] private float sensorRange = 3.0f; // Algılama mesafesi
    [SerializeField] private LayerMask obstacleLayer;  // Algılanacak katman

    private NavMeshAgent agent;
    public CompanionState CurrentState { get; private set; } = CompanionState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        OrderGoToTarget();
    }

    public void OrderGoToTarget()
    {
        if (targetObject != null)
        {
            CurrentState = CompanionState.MovingToTarget;
            agent.SetDestination(targetObject.transform.position);
        }
    }

    private void Update()
    {
        // 1. Raycast Sensör Kontrolü (Sürekli önünü tarar)
        PerformSensorScan();

        // 2. FSM Durum Kontrolleri
        switch (CurrentState)
        {
            case CompanionState.MovingToTarget:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    CurrentState = CompanionState.Interacting;
                    ExecuteInteraction();
                }
                break;

            case CompanionState.ReturningToPlayer:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    CurrentState = CompanionState.Idle;
                    Debug.Log("[Fındık FSM] Görev tamamlandı, oyuncunun yanındayım.");
                }
                break;
        }
    }

    // Raycast ile çevre algılama (Sensör mekanizması)
    private void PerformSensorScan()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f; // Yer seviyesinden biraz yukarıdan fırlat
        Vector3 direction = transform.forward;

        // Sahne ekranında ışını kırmızı olarak çiz (Görsel doğrulama)
        Debug.DrawRay(origin, direction * sensorRange, Color.red);

        // Fiziksel ışın fırlatma kontrolü
        if (Physics.Raycast(origin, direction, out RaycastHit hit, sensorRange))
        {
            // Önünde bir nesne gördüğünde
            Debug.Log($"[Fındık Sensör] Algılanan Nesne: {hit.collider.gameObject.name} (Mesafe: {hit.distance:F1}m)");
        }
    }

    private void ExecuteInteraction()
    {
        if (targetObject != null && targetObject.TryGetComponent<IInteractable>(out var interactable))
        {
            interactable.Interact();
        }

        CurrentState = CompanionState.ReturningToPlayer;
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }
}