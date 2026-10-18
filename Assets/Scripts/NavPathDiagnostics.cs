using UnityEngine;
using UnityEngine.AI;

public class NavPathDiagnostics : MonoBehaviour
{
    [Header("Zorunlu")]
    public NavMeshAgent agent;
    public Transform target;            // karşı kıyıya koyduğun boş obje

    [Header("İsteğe bağlı: köprünün iki ucu")]
    public Transform bridgeStart;
    public Transform bridgeEnd;

    public float sampleRadius = 1.5f;

    NavMeshPath path;

    void Reset() { agent = GetComponent<NavMeshAgent>(); }

    [ContextMenu("Yol Testi")]
    public void RunTest()
    {
        if (agent == null || target == null)
        {
            Debug.LogWarning("[NavDiag] Agent ya da Target atanmamış.");
            return;
        }

        Debug.Log($"[NavDiag] AGENT: tip={agent.agentTypeID}, yarıçap={agent.radius}, yükseklik={agent.height}, " +
                  $"areaMask={agent.areaMask}, autoTraverseOffMeshLink={agent.autoTraverseOffMeshLink}, " +
                  $"stoppingDistance={agent.stoppingDistance}, navmesh üzerinde={agent.isOnNavMesh}");

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[NavDiag] Fındık NavMesh üzerinde değil. Başlangıç noktası bake alanının dışında.");
            return;
        }

        LogSample("Fındık", agent.transform.position);
        LogSample("Hedef", target.position);
        if (bridgeStart != null) LogSample("Köprü başı", bridgeStart.position);
        if (bridgeEnd != null) LogSample("Köprü sonu", bridgeEnd.position);

        path = new NavMeshPath();
        bool found = agent.CalculatePath(target.position, path);
        Debug.Log($"[NavDiag] YOL: bulundu={found}, durum={path.status}, köşe sayısı={path.corners.Length}");

        if (path.corners.Length > 0)
        {
            Vector3 last = path.corners[path.corners.Length - 1];
            Debug.Log($"[NavDiag] Son köşe={last}, hedefe uzaklık={Vector3.Distance(last, target.position):F2} m");
        }

        if (path.status == NavMeshPathStatus.PathPartial)
            Debug.LogWarning("[NavDiag] Kısmi yol: Fındık hedefe ulaşamıyor, en yakın noktaya gidip duracak. " +
                             "Köprü NavMesh'e girmemiş ya da iki yakaya bağlanmıyor.");
        else if (path.status == NavMeshPathStatus.PathInvalid)
            Debug.LogError("[NavDiag] Geçersiz yol.");
        else
            Debug.Log("[NavDiag] Yol tam. Sorun NavMesh'te değil, CompanionController tarafında (varış kontrolü, ResetPath vb.).");
    }

    void LogSample(string label, Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            Debug.Log($"[NavDiag] {label}: NavMesh var, uzaklık={hit.distance:F2} m, alan maskesi={hit.mask} (1=Walkable, 2=Not Walkable)");
        else
            Debug.LogWarning($"[NavDiag] {label}: {sampleRadius} m içinde NavMesh YOK ({pos}).");
    }

    void OnDrawGizmos()
    {
        if (path == null || path.corners.Length < 2) return;
        Gizmos.color = path.status == NavMeshPathStatus.PathComplete ? Color.green :
                       path.status == NavMeshPathStatus.PathPartial ? Color.yellow : Color.red;
        for (int i = 0; i < path.corners.Length - 1; i++)
            Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
    }
}