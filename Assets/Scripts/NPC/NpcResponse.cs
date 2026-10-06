using System;

public enum NpcDecision { OpenGate, AskMore, Refuse }
public enum NpcMood { Friendly, Suspicious, Angry }

// Modelin döndürdüğü JSON'un ham hâli
[Serializable]
public class NpcResponse
{
    public string decision;
    public string mood;
    public string dialogue;
}

// Oyunun geri kalanının kullanacağı sonuç
public class NpcResult
{
    public bool Success;
    public NpcDecision Decision;
    public NpcMood Mood;
    public string Dialogue;
    public string Error;
    public bool Validated;   // Validator'dan geçti mi

    public bool DecisionOverridden;   // Unity LLM'in kararını değiştirdi mi

    public static NpcResult Fail(string error) => new NpcResult
    {
        Success = false,
        Error = error,
        Decision = NpcDecision.AskMore,
        Mood = NpcMood.Suspicious,
        Dialogue = "Basri Amca şu anda cevap veremiyor."
    };
}