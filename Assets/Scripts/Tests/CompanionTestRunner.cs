using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Fındık'ın otonom davranışını tekrarlanabilir biçimde ölçer.
//
// Her hedef için sabit tohumlu (seed) başlangıç noktaları üretilir. Fındık her noktaya ışınlanır,
// aynı Interact komutu verilir ve komuttan etkileşimin yürütülmesine kadar geçen süre ölçülür.
// Hedefe ulaşılamazsa ya da süre dolarsa deneme başarısız sayılır.
//
// Kullanım: Sahneye boş bir nesne ekleyip bu bileşeni ata, hedefleri listeye sürükle, Play'e bas, F10'a bas.
// Test sırasında fare ve klavyeyle oyuna müdahale etme. Test bitince sonuç Console'a ve dosyaya yazılır.
public class CompanionTestRunner : MonoBehaviour
{
    [Serializable]
    public class TestTarget
    {
        public string label;
        [Tooltip("IInteractable uygulayan bileşen (DamLever, CompanionTask vb.)")]
        public MonoBehaviour target;
    }

    [Header("Referanslar")]
    [SerializeField] private CompanionController companion;
    [Tooltip("Başlangıç noktalarının etrafında üretileceği merkez (genelde oyuncu ya da başlangıç noktası)")]
    [SerializeField] private Transform startCenter;
    [Tooltip("Test sırasında kapatılacak girdi bileşenleri (ör. CompanionCommandInput, PlayerMovement)")]
    [SerializeField] private Behaviour[] disableDuringTest;

    [Header("Hedefler (bu sırayla denenir)")]
    [SerializeField] private List<TestTarget> targets = new();

    [Header("Deney ayarları")]
    [SerializeField] private int seed = 42;
    [SerializeField] private int startsPerTarget = 6;
    [SerializeField] private int repeatsPerStart = 2;
    [SerializeField] private float minStartRadius = 8f;
    [SerializeField] private float maxStartRadius = 30f;
    [SerializeField] private float minTripDistance = 6f;
    [SerializeField] private float trialTimeoutSeconds = 30f;
    [Range(1f, 4f)] [SerializeField] private float timeScale = 1f;

    private readonly CompanionTestReport report = new();
    private bool running;
    private string status = "";

    private void Awake()
    {
        if (companion == null) companion = FindFirstObjectByType<CompanionController>();
    }

    private void Update()
    {
        if (!running && Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            StartTest();
    }

    [ContextMenu("Testi başlat")]
    public void StartTest()
    {
        if (running) return;
        if (companion == null) { Debug.LogError("[FındıkTest] CompanionController bulunamadı."); return; }
        StartCoroutine(RunAll());
    }

    private IEnumerator RunAll()
    {
        running = true;
        float oldScale = Time.timeScale;
        Time.timeScale = timeScale;
        SetInputEnabled(false);

        var agent = companion.GetComponent<NavMeshAgent>();
        Vector3 center = (startCenter != null ? startCenter : companion.transform).position;
        List<Vector3> pool = BuildStartPool(center);
        Debug.Log($"[FındıkTest] Başladı. Tohum={seed}, aday başlangıç noktası={pool.Count}, hedef={targets.Count}");

        foreach (var t in targets)
        {
            var interactable = t.target as IInteractable;
            if (interactable == null)
            {
                Debug.LogWarning($"[FındıkTest] '{t.label}' IInteractable değil, atlandı.");
                continue;
            }

            Vector3 targetPos = t.target.transform.position;
            List<Vector3> starts = PickValidStarts(pool, targetPos, out int skipped);
            report.SetSkippedStarts(t.label, skipped);
            Debug.Log($"[FındıkTest] {t.label}: geçerli başlangıç={starts.Count}, atlanan={skipped}");

            for (int s = 0; s < starts.Count; s++)
            {
                for (int rep = 0; rep < repeatsPerStart; rep++)
                {
                    status = $"{t.label}  başlangıç {s + 1}/{starts.Count}  tekrar {rep + 1}/{repeatsPerStart}";
                    CompanionTrialResult result = default;
                    yield return RunTrial(agent, t.label, interactable, targetPos, starts[s], s, rep, r => result = r);
                    report.Add(result);
                    Debug.Log($"[FındıkTest] {t.label} | başlangıç {s + 1} | tekrar {rep + 1} | " +
                              $"{(result.Success ? "BAŞARILI" : "BAŞARISIZ: " + result.FailReason)} | {result.Seconds:F1} sn");
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        Finish(agent, oldScale);
    }

    private IEnumerator RunTrial(NavMeshAgent agent, string label, IInteractable interactable, Vector3 targetPos,
                                 Vector3 start, int startIndex, int repeat, Action<CompanionTrialResult> done)
    {
        // Temiz başlangıç: komutu iptal et, ajanı ışınla, dönüşü geri aç
        companion.Receive(CompanionCommand.Stay());
        if (agent.isOnNavMesh) agent.ResetPath();
        agent.updateRotation = true;
        if (!agent.Warp(start))
        {
            done(new CompanionTrialResult(label, startIndex, repeat, false, 0f, "Işınlanma başarısız"));
            yield break;
        }
        yield return new WaitForSeconds(0.3f);

        float t0 = Time.time;
        bool sawInteracting = false;
        companion.Receive(CompanionCommand.Interact(targetPos, interactable));

        while (Time.time - t0 < trialTimeoutSeconds)
        {
            yield return null;
            CompanionState state = companion.CurrentState;
            if (state == CompanionState.Interacting) sawInteracting = true;

            // Etkileşim yürütülünce durum ReturningToPlayer (ya da hemen Idle) olur
            if (sawInteracting && (state == CompanionState.ReturningToPlayer || state == CompanionState.Idle))
            {
                done(new CompanionTrialResult(label, startIndex, repeat, true, Time.time - t0, null));
                yield break;
            }

            // Hiç etkileşime girmeden Idle'a dönmek: hedefe ulaşılamadı
            if (!sawInteracting && state == CompanionState.Idle && Time.time - t0 > 0.5f)
            {
                done(new CompanionTrialResult(label, startIndex, repeat, false, Time.time - t0, "Hedefe ulaşılamadı"));
                yield break;
            }
        }

        done(new CompanionTrialResult(label, startIndex, repeat, false, Time.time - t0,
            sawInteracting ? "Etkileşim zaman aşımı" : "Süre doldu, hedefe varılamadı"));
    }

    // Merkez etrafında, sabit tohumla, NavMesh üzerinde aday noktalar üretir (her çalıştırmada aynı).
    private List<Vector3> BuildStartPool(Vector3 center)
    {
        var rnd = new System.Random(seed);
        var pool = new List<Vector3>();
        for (int i = 0; i < 400 && pool.Count < 60; i++)
        {
            float angle = (float)(rnd.NextDouble() * Math.PI * 2);
            float radius = minStartRadius + (float)rnd.NextDouble() * (maxStartRadius - minStartRadius);
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                pool.Add(hit.position);
        }
        return pool;
    }

    // Hedefe NavMesh üzerinde tam yolu olan ve yeterince uzak noktaları seçer.
    private List<Vector3> PickValidStarts(List<Vector3> pool, Vector3 targetPos, out int skipped)
    {
        var valid = new List<Vector3>();
        skipped = 0;
        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit targetHit, 5f, NavMesh.AllAreas))
        {
            skipped = pool.Count;
            return valid;
        }

        var path = new NavMeshPath();
        foreach (Vector3 p in pool)
        {
            if (valid.Count >= startsPerTarget) break;

            Vector3 a = p; a.y = 0f;
            Vector3 b = targetPos; b.y = 0f;
            if (Vector3.Distance(a, b) < minTripDistance) { skipped++; continue; }

            bool hasPath = NavMesh.CalculatePath(p, targetHit.position, NavMesh.AllAreas, path)
                           && path.status == NavMeshPathStatus.PathComplete;
            if (hasPath) valid.Add(p); else skipped++;
        }
        return valid;
    }

    private void Finish(NavMeshAgent agent, float oldScale)
    {
        companion.Receive(CompanionCommand.Stay());
        agent.updateRotation = true;
        Time.timeScale = oldScale;
        SetInputEnabled(true);
        running = false;
        status = "Test bitti";

        string md = report.ToMarkdown();
        string dir = Application.persistentDataPath;
        File.WriteAllText(Path.Combine(dir, "findik_test_sonuc.csv"), report.ToCsv());
        File.WriteAllText(Path.Combine(dir, "findik_test_sonuc.md"), md + "\n" + report.FailureSummary() + "\n" + report.SkippedSummary());

        Debug.Log("[FındıkTest] BİTTİ. Sonuç tablosu:\n" + md + "\n" + report.FailureSummary() + "\n" + report.SkippedSummary()
                  + $"\nDosyalar: {dir}");
    }

    private void SetInputEnabled(bool enabled)
    {
        if (disableDuringTest == null) return;
        foreach (var b in disableDuringTest)
            if (b != null) b.enabled = enabled;
    }

    private void OnGUI()
    {
        if (!running && status == "") return;
        GUI.Label(new Rect(10, 10, 700, 24), $"[Fındık Testi] {status}");
    }
}
