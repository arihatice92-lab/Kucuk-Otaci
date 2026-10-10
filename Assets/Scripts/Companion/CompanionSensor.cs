using UnityEngine;

public readonly struct SensorObservation
{
    public readonly float Distance;        // hedefe mesafe (m)
    public readonly float Angle;           // Fındık'ın önüne göre hedef açısı (derece, işaretli)
    public readonly bool HasLineOfSight;   // arada katı engel yok mu
    public readonly string BlockerName;    // varsa engelleyen nesne

    public SensorObservation(float distance, float angle, bool los, string blocker)
    {
        Distance = distance; Angle = angle; HasLineOfSight = los; BlockerName = blocker;
    }
}

public class CompanionSensor : MonoBehaviour
{
    [Header("Ön tarama")]
    [SerializeField] private float forwardRange = 3f;
    [SerializeField] private float eyeHeight = 0.5f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private Collider lastSeen;

    // Ön tarama: Fındık'ın burnundan ileriye kırmızı ışın (Scene görünümünde)
    private void Update()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Debug.DrawRay(origin, transform.forward * forwardRange, Color.red);

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, forwardRange, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != lastSeen)
            {
                lastSeen = hit.collider;
                Debug.Log($"[Fındık Sensör] Önümde: {hit.collider.name} ({hit.distance:F1} m)");
            }
        }
        else lastSeen = null;
    }

    // Belirli bir hedef için gözlem: mesafe, açı, görüş hattı
    public SensorObservation Observe(Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 aim = GetAimPoint(target);
        Vector3 toTarget = aim - origin;
        float distance = toTarget.magnitude;

        Vector3 flat = toTarget; flat.y = 0f;
        float angle = flat.sqrMagnitude > 0.0001f
            ? Vector3.SignedAngle(transform.forward, flat, Vector3.up)
            : 0f;

        bool los = true;
        string blocker = null;
        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            bool isTarget = hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform);
            bool isSelf = hit.transform.IsChildOf(transform);
            if (!isTarget && !isSelf)
            {
                los = false;
                blocker = hit.collider.name;
            }
        }

        return new SensorObservation(distance, angle, los, blocker);
    }

    public Vector3 GetAimPoint(Transform target)
    {
        // Trigger olmayan, Fındık'a en yakın collider'ı seç (görünmez alanlar hedef sayılmasın)
        Collider best = null;
        float bestDist = float.MaxValue;
        foreach (var c in target.GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;
            float d = (c.bounds.center - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = c; }
        }
        if (best == null)
        {
            var any = target.GetComponentInChildren<Collider>();
            return any != null ? any.bounds.center : target.position;
        }

        // Göz yüksekliğinde, collider'ın dikey aralığına sıkıştırılmış nokta (ışın zemine batmasın)
        Bounds b = best.bounds;
        float eyeY = transform.position.y + eyeHeight;
        float y = Mathf.Clamp(eyeY, b.min.y + 0.05f, b.max.y - 0.05f);
        return new Vector3(b.center.x, y, b.center.z);
    }
}