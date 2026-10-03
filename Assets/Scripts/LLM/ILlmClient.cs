using System;
using System.Collections;

public interface ILlmClient
{
    IEnumerator Send(string userMessage, Action<NpcResult> onDone);
}