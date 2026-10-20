using UnityEngine;

// Açıklama: Baraj şalteri mekanizması. Fındık etkileşime geçtiğinde sel suyunu
//           kaldırarak fiziksel engeli çözer ve "sel suyu çekildi" görevini tamamlar.
public class DamLever : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject floodWater;
    [SerializeField] private string doneMessage = "Baraj kolu indirildi, sel suyu çekiliyor.";

    public bool IsDone { get; private set; }

    public string InteractionPrompt => "Baraj Kolunu İndir";

    public void Interact()
    {
        if (IsDone) return; // kol bir kez çalışır, tekrar çağrılırsa yok say
        IsDone = true;

        Debug.Log("[Mekanik] Fındık baraj kolunu indirdi! Su çekiliyor...");

        if (floodWater != null)
        {
            floodWater.SetActive(false);
            Debug.Log("[Mekanik] Yol açıldı, oyuncu ormana doğru ilerleyebilir.");
        }
        else
        {
            Debug.LogWarning("DamLever: floodWater atanmamış, su kapatılamadı.", this);
        }

        if (QuestManager.Instance != null)
            QuestManager.Instance.Complete(QuestId.FloodReceded);

        if (!string.IsNullOrEmpty(doneMessage))
            HintUI.Say(doneMessage);
    }
}