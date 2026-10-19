using UnityEngine;

/// <summary>
/// Oyuncunun yatay hızını ölçüp Animator'daki "Speed" parametresine yazar.
/// Hareket kodundan bağımsızdır: konum değişimine bakar.
/// </summary>
public class PlayerAnimatorDriver : MonoBehaviour
{
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float damping = 12f;

    private Animator[] animators;
    private int speedHash;
    private Vector3 lastPosition;
    private float smoothedSpeed;

    private void Awake()
    {
        speedHash = Animator.StringToHash(speedParameter);
        animators = GetComponentsInChildren<Animator>(true);
        lastPosition = transform.position;

        if (animators.Length == 0)
            Debug.LogError("PlayerAnimatorDriver: Player altında Animator bulunamadı.", this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        float speed = delta.magnitude / dt;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 1f - Mathf.Exp(-damping * dt));

        foreach (Animator animator in animators)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetFloat(speedHash, smoothedSpeed);
        }
    }
}