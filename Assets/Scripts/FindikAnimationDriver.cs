using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Fındık'ın gerçek yatay hızını (konum değişiminden) ölçüp Animator'daki "Speed"
/// parametresine yazar. NavMeshAgent.velocity yerine gerçek hareket kullanılır; böylece
/// Fındık engele takılıp yerinde dururken yürüme animasyonu oynamaz.
/// Agent ve Animator Inspector'dan atanmadıysa hiyerarşide kendisi bulur.
/// </summary>
public class FindikAnimatorDriver : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator anim;
    [SerializeField] private string speedParam = "Speed";

    [Header("Ayar")]
    [Tooltip("Bu hızın altı durma sayılır.")]
    [SerializeField] private float deadZone = 0.25f;
    [SerializeField] private float damping = 10f;

    private int speedHash;
    private Vector3 lastPosition;
    private float smoothedSpeed;

    private void Awake()
    {
        speedHash = Animator.StringToHash(speedParam);

        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (agent == null || anim == null)
        {
            Debug.LogError("FindikAnimatorDriver: NavMeshAgent veya Animator bulunamadı.", this);
            return;
        }

        lastPosition = agent.transform.position;
    }

    private void Update()
    {
        if (agent == null || anim == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 delta = agent.transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = agent.transform.position;

        float measured = delta.magnitude / dt;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, measured, 1f - Mathf.Exp(-damping * dt));

        float value = smoothedSpeed < deadZone ? 0f : smoothedSpeed;
        anim.SetFloat(speedHash, value);
    }
}