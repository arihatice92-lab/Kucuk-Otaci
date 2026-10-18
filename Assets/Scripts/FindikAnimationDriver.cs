using UnityEngine;
using UnityEngine.AI;

public class FindikAnimatorDriver : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator anim;
    public string speedParam = "Speed";

    private void Awake()
    {
        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (agent == null || anim == null) return;

        // NavMeshAgent'ın anlık hızını alıp Animator'a aktarır
        float currentSpeed = agent.velocity.magnitude;
        anim.SetFloat(speedParam, currentSpeed);
    }
}