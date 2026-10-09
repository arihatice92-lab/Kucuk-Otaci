using UnityEngine;
using UnityEngine.AI;
//Fındık yürürken burnundan ileriye doğru kırmızı bir çizgi çıktığını ve nesneleri gördüğünde alt kısımdaki Console paneline log düştüğü görülecek.
public enum CompanionState
{
    Idle,
    Following,
    MovingToTarget,
    Interacting,
    ReturningToPlayer
}


[RequireComponent(typeof(NavMeshAgent))]
public class CompanionController : MonoBehaviour, ICompanionCommandReceiver
{
    [Header("Referanslar")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject targetObject;

    [Header("Sensör (Raycast) Ayarları")]
    [SerializeField] private float sensorRange = 3.0f; // Algılama mesafesi
    [SerializeField] private LayerMask obstacleLayer;  // Algılanacak katman

    [Header("Takip Ayarları")]
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float followRepathInterval = 0.25f;
    [SerializeField] private float arrivalTolerance = 1.2f;
    private Vector3 destination;

    private NavMeshAgent agent;
    private IInteractable pendingInteractable;
    private float nextRepathTime;
    private Collider lastSeenCollider;

    public CompanionState CurrentState { get; private set; } = CompanionState.Idle;

    private float stateEnteredTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // ---- Komut girişi (ICompanionCommandReceiver) ----
    public void Receive(CompanionCommand command)
    {
        Debug.Log($"[Fındık FSM] Komut alındı: {command.Type}");
        switch (command.Type)
        {
            case CompanionCommandType.Follow:
                pendingInteractable = null;
                CurrentState = CompanionState.Following;
                nextRepathTime = 0f;
                break;

            case CompanionCommandType.Stay:
                pendingInteractable = null;
                StopMoving();
                CurrentState = CompanionState.Idle;
                break;

            case CompanionCommandType.GoTo:
                pendingInteractable = null;
                MoveTo(command.TargetPosition);
                break;

            case CompanionCommandType.Interact:
                pendingInteractable = command.Target;
                MoveTo(command.TargetPosition);
                break;
        }
    }

    // Eski kullanım: Inspector'daki targetObject'e git ve etkileşime gir
    public void OrderGoToTarget()
    {
        if (targetObject == null) return;
        var interactable = targetObject.GetComponentInParent<IInteractable>();
        Receive(interactable != null
            ? CompanionCommand.Interact(targetObject.transform.position, interactable)
            : CompanionCommand.GoTo(targetObject.transform.position));
    }

    private void Update()
    {
        // 1. Raycast Sensör Kontrolü (Sürekli önünü tarar)
        PerformSensorScan();

        // 2. FSM Durum Kontrolleri
        switch (CurrentState)
        {
            case CompanionState.Following:
                UpdateFollow();
                break;

            case CompanionState.MovingToTarget:
                if (HasStopped())
                {
                    if (!IsNear(destination, agent.stoppingDistance + arrivalTolerance))
                    {
                        Debug.LogWarning("[Fındık FSM] Hedefe ulaşamadım (yol kapalı olabilir).");
                        pendingInteractable = null;
                        CurrentState = CompanionState.Idle;
                    }
                    else if (pendingInteractable != null)
                    {
                        CurrentState = CompanionState.Interacting;
                        ExecuteInteraction();
                    }
                    else
                    {
                        CurrentState = CompanionState.Idle;
                        Debug.Log("[Fındık FSM] Hedefe vardım.");
                    }
                }
                break;

            case CompanionState.ReturningToPlayer:
                {
                    if (playerTransform == null)
                    {
                        CurrentState = CompanionState.Idle;
                        break;
                    }

                    float toPlayer = Vector3.Distance(transform.position, playerTransform.position);
                    if (toPlayer <= followDistance)
                    {
                        StopMoving();
                        CurrentState = CompanionState.Idle;
                        Debug.Log("[Fındık FSM] Görev tamamlandı, oyuncunun yanındayım.");
                    }
                    else if (Time.time - stateEnteredTime > 0.3f && HasStopped())
                    {
                        CurrentState = CompanionState.Idle;
                        Debug.LogWarning($"[Fındık FSM] Oyuncuya ulaşamadım. pathStatus={agent.pathStatus}, mesafe={toPlayer:F1} m");
                    }
                    else if (Time.time >= nextRepathTime)
                    {
                        nextRepathTime = Time.time + followRepathInterval;
                        agent.isStopped = false;
                        agent.SetDestination(playerTransform.position);
                    }
                    break;
                }
        }
    }

    private void UpdateFollow()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= followDistance)
        {
            StopMoving();
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + followRepathInterval;
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }
    }

    private void MoveTo(Vector3 worldPosition)
    {
        // Tıklanan nokta NavMesh dışında olabilir; en yakın yürünebilir noktaya çek
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[Fındık FSM] Hedef yürünebilir alanda değil, komut yok sayıldı.");
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(navHit.position);
        destination = navHit.position;
        CurrentState = CompanionState.MovingToTarget;
    }

    private void StopMoving()
    {
        if (!agent.isOnNavMesh) return;
        agent.ResetPath();
        agent.isStopped = true;
    }

    private bool HasStopped()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    private bool IsNear(Vector3 target, float tolerance)
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target; b.y = 0f;
        return Vector3.Distance(a, b) <= tolerance;
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
            // Aynı nesneyi her karede tekrar loglama
            if (hit.collider != lastSeenCollider)
            {
                lastSeenCollider = hit.collider;
                Debug.Log($"[Fındık Sensör] Algılanan Nesne: {hit.collider.gameObject.name} (Mesafe: {hit.distance:F1}m)");
            }
        }
        else
        {
            lastSeenCollider = null;
        }
    }

    private void ExecuteInteraction()
    {
        pendingInteractable?.Interact();
        pendingInteractable = null;

        CurrentState = CompanionState.ReturningToPlayer;
        if (playerTransform != null)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            stateEnteredTime = Time.time;
            nextRepathTime = Time.time + followRepathInterval;
        }
        else
        {
            CurrentState = CompanionState.Idle;
        }
    }
}