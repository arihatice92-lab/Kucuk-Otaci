using UnityEngine;

/// <summary>
/// Karakter seçim ekranında seçilen karakterin modelini oyuncunun üzerinde gösterir.
/// Seçilmeyen model kapatılır.
/// </summary>
public class PlayerModelSelector : MonoBehaviour
{
    [SerializeField] private GameObject girlModel;
    [SerializeField] private GameObject boyModel;

    private void Start()
    {
        if (girlModel == null || boyModel == null)
        {
            Debug.LogError("PlayerModelSelector: GirlModel veya BoyModel atanmamış.", this);
            return;
        }

        bool isGirl = GameSession.SelectedCharacter == PlayerCharacter.Girl;
        girlModel.SetActive(isGirl);
        boyModel.SetActive(!isGirl);
    }
}