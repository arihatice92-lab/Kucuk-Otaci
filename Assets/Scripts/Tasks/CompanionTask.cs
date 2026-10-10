using UnityEngine;
using UnityEngine.Events;

// Sorumluluk: Fındık'ın etkileşimiyle tamamlanan bir görev nesnesi.
// Aynı sınıf köprü onarımı, devrilen ağacın kaldırılması ve kedi kurtarma için kullanılır;
// farkı yalnızca Inspector'daki ayarlardır (SOLID - tek sınıf, ayarla çeşitlenen davranış).
// Oyuncu Fındık'a sağ tıkla komut verince Fındık nesneye gider ve Interact() çağrılır.
public class CompanionTask : MonoBehaviour, IInteractable
{
    [Header("Etkileşim")]
    [SerializeField] private string interactionPrompt = "Fındık görevi";
    [SerializeField] private string doneMessage = "";   // Tamamlanınca ekranda gösterilir (boşsa gösterilmez)

    [Header("Görev")]
    [SerializeField] private bool completesQuest = true;   // Ağaç gibi ön adımlarda kapat
    [SerializeField] private QuestId quest;

    [Header("Tamamlanınca")]
    [SerializeField] private GameObject[] disableOnDone;   // Örn. yıkık köprü, engel, ağaçtaki kedi
    [SerializeField] private GameObject[] enableOnDone;    // Örn. onarılmış köprü, yerdeki kedi
    [SerializeField] private UnityEvent onCompleted;       // İleride ses ve animasyon bağlamak için

    public bool IsDone { get; private set; }

    public string InteractionPrompt => interactionPrompt;

    public void Interact()
    {
        if (IsDone) return;
        IsDone = true;

        foreach (GameObject go in disableOnDone)
            if (go != null) go.SetActive(false);

        foreach (GameObject go in enableOnDone)
            if (go != null) go.SetActive(true);

        if (completesQuest && QuestManager.Instance != null)
            QuestManager.Instance.Complete(quest);

        if (!string.IsNullOrEmpty(doneMessage))
            HintUI.Say(doneMessage);

        onCompleted?.Invoke();
        Debug.Log($"[Görev] {name} tamamlandı.");
    }
}
