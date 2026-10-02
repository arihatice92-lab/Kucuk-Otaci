using System;
using UnityEngine;

public static class NpcResponseParser
{
    public static NpcResult Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return NpcResult.Fail("Boş cevap");

        NpcResponse raw;
        try { raw = JsonUtility.FromJson<NpcResponse>(content); }
        catch (Exception e) { return NpcResult.Fail("Geçersiz JSON: " + e.Message); }

        if (raw == null || string.IsNullOrWhiteSpace(raw.dialogue))
            return NpcResult.Fail("dialogue alanı eksik");

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