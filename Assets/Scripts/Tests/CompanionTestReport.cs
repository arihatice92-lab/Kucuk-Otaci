using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

// Tek bir denemenin sonucu (hedef + başlangıç noktası + sonuç).
public readonly struct CompanionTrialResult
{
    public readonly string Target;
    public readonly int StartIndex;
    public readonly int Repeat;
    public readonly bool Success;
    public readonly float Seconds;      // komuttan etkileşimin yürütülmesine kadar geçen süre
    public readonly string FailReason;  // başarısızsa nedeni, aksi halde boş

    public CompanionTrialResult(string target, int startIndex, int repeat, bool success, float seconds, string failReason)
    {
        Target = target; StartIndex = startIndex; Repeat = repeat;
        Success = success; Seconds = seconds; FailReason = failReason;
    }
}

// Sonuçları özetler ve Markdown / CSV metnine çevirir. Unity'ye bağımlı değildir (tek sorumluluk: raporlama).
public class CompanionTestReport
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly List<CompanionTrialResult> results = new();
    private readonly Dictionary<string, int> skippedStarts = new();

    public int TrialCount => results.Count;

    public void Add(CompanionTrialResult r) => results.Add(r);

    public void SetSkippedStarts(string target, int count) => skippedStarts[target] = count;

    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Hedef | Başlangıç noktası | Deneme | Başarılı | Başarı oranı | Ort. süre (sn) | En kısa / en uzun (sn) |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var group in results.GroupBy(r => r.Target))
        {
            sb.AppendLine(Row(group.Key, group.ToList()));
        }

        sb.AppendLine(Row("**Toplam**", results));
        return sb.ToString();
    }

    private string Row(string label, List<CompanionTrialResult> list)
    {
        int total = list.Count;
        int ok = list.Count(r => r.Success);
        int starts = list.Select(r => r.StartIndex).Distinct().Count();
        var times = list.Where(r => r.Success).Select(r => r.Seconds).ToList();

        string rate = total == 0 ? "-" : $"%{100f * ok / total:F0}";
        string avg = times.Count == 0 ? "-" : times.Average().ToString("F1", Tr);
        string range = times.Count == 0 ? "-" : $"{times.Min().ToString("F1", Tr)} / {times.Max().ToString("F1", Tr)}";
        return $"| {label} | {starts} | {total} | {ok} | {rate} | {avg} | {range} |";
    }

    public string FailureSummary()
    {
        var failed = results.Where(r => !r.Success).ToList();
        if (failed.Count == 0) return "Başarısız deneme yok.";

        var sb = new StringBuilder("Başarısız denemeler:\n");
        foreach (var r in failed)
            sb.AppendLine($"- {r.Target}, başlangıç {r.StartIndex + 1}, tekrar {r.Repeat + 1}: {r.FailReason}");
        return sb.ToString();
    }

    public string SkippedSummary()
    {
        if (skippedStarts.Count == 0) return "";
        var sb = new StringBuilder("Yolu olmayan (atlanan) başlangıç noktaları:\n");
        foreach (var kv in skippedStarts) sb.AppendLine($"- {kv.Key}: {kv.Value}");
        return sb.ToString();
    }

    public string ToCsv()
    {
        var sb = new StringBuilder("hedef,baslangic,tekrar,basarili,sure_sn,neden\n");
        foreach (var r in results)
        {
            sb.AppendLine(string.Join(",",
                r.Target, r.StartIndex + 1, r.Repeat + 1, r.Success ? 1 : 0,
                r.Seconds.ToString("F2", Inv), r.FailReason ?? ""));
        }
        return sb.ToString();
    }
}
