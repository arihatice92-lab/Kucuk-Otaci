using System;
using UnityEngine;

public static class NpcResponseParser
{

    private static bool HasLetter(string s)
    {
        foreach (char c in s)
            if (char.IsLetter(c)) return true;
        return false;
    }

    private static bool LooksLikeLeakedInstruction(string s)
    {
        string t = s.Trim();
        return t.Contains("OPEN_GATE") || t.Contains("ASK_MORE") || t.Contains("REFUSE") || t.EndsWith(":");
    }

    // Cevap uzunluk sınırında kesildiyse (done_reason: length) son tam cümleye kadar olan kısmı kurtarır.
    // decision ve mood alanları şemada dialogue'dan önce geldiği için eksiksizdir.
    private static string TryRepairTruncated(string content)
    {
        int key = content.IndexOf("\"dialogue\"");
        if (key < 0) return null;

        int colon = content.IndexOf(':', key);
        int open = colon < 0 ? -1 : content.IndexOf('"', colon + 1);
        if (open < 0) return null;

        string body = content.Substring(open + 1);
        int cut = body.LastIndexOfAny(new[] { '.', '?', '!' });
        if (cut < 5) return null;                       // kurtarılacak tam cümle yok

        body = body.Substring(0, cut + 1).Replace("\\", "").Replace("\"", "'");
        return content.Substring(0, open + 1) + body + "\"}";
    }
    public static NpcResult Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return NpcResult.Fail("Boş cevap");

        NpcResponse raw;
        try { raw = JsonUtility.FromJson<NpcResponse>(content); }
        catch (Exception e)
        {
            string repaired = TryRepairTruncated(content);
            if (repaired == null) return NpcResult.Fail("Geçersiz JSON: " + e.Message);

            try { raw = JsonUtility.FromJson<NpcResponse>(repaired); }
            catch (Exception e2) { return NpcResult.Fail("Geçersiz JSON: " + e2.Message); }

            Debug.LogWarning("[Parser] Kesik JSON, son tam cümleye kadar onarıldı.");
        }


        if (raw == null || string.IsNullOrWhiteSpace(raw.dialogue) || !HasLetter(raw.dialogue))
            return NpcResult.Fail("dialogue alanı eksik veya anlamsız");

        if (LooksLikeLeakedInstruction(raw.dialogue))
            return NpcResult.Fail("Prompt sızıntısı");

        NpcDecision decision;
        switch (raw.decision)
        {
            case "OPEN_GATE": decision = NpcDecision.OpenGate; break;
            case "ASK_MORE": decision = NpcDecision.AskMore; break;
            case "REFUSE": decision = NpcDecision.Refuse; break;
            default: return NpcResult.Fail("Beklenmeyen decision: " + raw.decision);
        }

        NpcMood mood;
        switch (raw.mood)
        {
            case "FRIENDLY": mood = NpcMood.Friendly; break;
            case "SUSPICIOUS": mood = NpcMood.Suspicious; break;
            case "ANGRY": mood = NpcMood.Angry; break;
            default: return NpcResult.Fail("Beklenmeyen mood: " + raw.mood);
        }

        return new NpcResult
        {
            Success = true,
            Decision = decision,
            Mood = mood,
            Dialogue = raw.dialogue.Trim()
        };
    }
}