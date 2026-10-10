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

    [Header("Etkileşim (sensör kararı)")]
    [SerializeField] private CompanionSensor sensor;
    [SerializeField] private float interactRange = 4f;
    [SerializeField] private float interactAngle = 25f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float interactTimeout = 4f;

    private Transform pendingTarget;
    private float interactStartTime;
    private float nextSensorLogTime;
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
        if (sensor == null) sensor = GetComponent<CompanionSensor>();
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
                pendingTarget = (command.Target as Component)?.transform;
                MoveTo(command.TargetPosition, 4.0f);
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

    private void UpdateInteracting()
    {
        // Sensör ya da hedef yoksa eski davranışa düş
        if (sensor == null || pendingTarget == null)
        {
            ExecuteInteraction();
            return;
        }

        // Hedefe doğru dön (sensörün baktığı noktaya)
        Vector3 aim = sensor.GetAimPoint(pendingTarget);
        Vector3 dir = aim - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        SensorObservation obs = sensor.Observe(pendingTarget);
        bool inRange = obs.Distance <= interactRange;
        bool facing = Mathf.Abs(obs.Angle) <= interactAngle;

        if (Time.time >= nextSensorLogTime)
        {
            nextSensorLogTime = Time.time + 0.5f;
            Debug.Log($"[Fındık Sensör] Hedef: {pendingTarget.name} | Mesafe: {obs.Distance:F1} m | Açı: {obs.Angle:F0}° | Görüş: {(obs.HasLineOfSight ? "açık" : "engelli: " + obs.BlockerName)}");
        }

        if (inRange && facing && obs.HasLineOfSight)
        {
            ExecuteInteraction();
            return;
        }

        if (Time.time - interactStartTime > interactTimeout)
        {
            Debug.LogWarning($"[Fındık FSM] Etkileşim iptal: menzil={inRange}, yön={facing}, görüş={obs.HasLineOfSight}");
            agent.updateRotation = true;
            pendingInteractable = null;
            pendingTarget = null;
            CurrentState = CompanionState.Idle;
        }
    }
    private void Update()
    {
        

        // 2. FSM Durum Kontrolleri
        switch (CurrentState)
        {
            case CompanionState.Interacting:
                UpdateInteracting();
                break;

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
                        interactStartTime = Time.time;
                        agent.updateRotation = false;
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

    private void MoveTo(Vector3 worldPosition, float searchRadius = 2.0f)
    {
        // Tıklanan nokta NavMesh dışında olabilir; en yakın yürünebilir noktaya çek
        if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navHit, searchRadius, NavMesh.AllAreas))
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
    

    private void ExecuteInteraction()
    {
        Debug.Log($"[Fındık FSM] Etkileşim yürütülüyor: {pendingTarget?.name}");
        agent.updateRotation = true;
        pendingInteractable?.Interact();
        pendingInteractable = null;
        pendingTarget = null;

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