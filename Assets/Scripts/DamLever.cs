using UnityEngine;
// Açıklama: Baraj şalteri mekanizması. Fındık etkileşime geçtiğinde sel suyunu
//           kaldırarak fiziksel engeli çözer.


public class DamLever : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject floodWater;
    public string InteractionPrompt => "Baraj Kolunu İndir";

    public void Interact()
    {
        Debug.Log("[Mekanik] Fındık baraj kolunu indirdi! Su çekiliyor...");
        
        if (floodWater != null)
        {
            floodWater.SetActive(false);
            Debug.Log("[Mekanik] Yol açıldı, oyuncu ormana doğru ilerleyebilir.");
        }
    }
}