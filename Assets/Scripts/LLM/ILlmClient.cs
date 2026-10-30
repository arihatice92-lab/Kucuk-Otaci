using System;
using System.Collections;

// DialogueManager somut OllamaClient yerine bu arayüze bağımlıdır (Dependency Inversion).
// Başka bir yerel LLM istemcisi yazılırsa DialogueManager değişmeden kullanılabilir.
public interface ILlmClient
{
    IEnumerator Send(string userMessage, Action<NpcResult> onDone);
    IEnumerator Regenerate(string userMessage, NpcDecision forced, Action<NpcResult> onDone);

    // Modeli önceden belleğe yükler (ilk cevap gecikmesin diye)
    IEnumerator Warmup();

    // Yeniden üretimde modele iletilecek tek satırlık olguyu ayarlar (boş string temizler)
    void SetGameState(string state);

    // Cümle, few-shot örneklerinden birinin kopyası mı?
    bool IsSeedCopy(string dialogue);
}