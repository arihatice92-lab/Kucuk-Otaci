using UnityEngine;
using UnityEngine.AI;

public class FindikAnimatorDriver : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public string speedParam = "Speed";
    public float smoothing = 10f;
    public bool debugLog = false;

    int speedHash;
    Vector3 lastPos;
    float speed;

    void Awake()
    {
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        speedHash = Animator.StringToHash(speedParam);
    }

    void Start() { lastPos = transform.position; }

    void Update()
    {
        if (animator == null) return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float measured = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;

        float fromAgent = agent != null ? agent.velocity.magnitude : 0f;
        float target = Mathf.Max(measured, fromAgent);
        if (target < 0.05f) target = 0f;

        speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-smoothing * dt));
        animator.SetFloat(speedHash, speed);

        if (debugLog)
            Debug.Log($"[Fındık Anim] ölçülen={measured:F2} agent={fromAgent:F2} gönderilen={speed:F2}");
    }
}