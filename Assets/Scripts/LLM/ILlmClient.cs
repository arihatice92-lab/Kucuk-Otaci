using System;
using System.Collections;

public interface ILlmClient
{
    IEnumerator Send(string userMessage, Action<NpcResult> onDone);
    IEnumerator Regenerate(string userMessage, NpcDecision forced, Action<NpcResult> onDone);
}