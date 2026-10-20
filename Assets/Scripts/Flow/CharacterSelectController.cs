using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private CharacterOption girl;
    [SerializeField] private CharacterOption boy;
    [SerializeField] private Button confirmButton;

    private CharacterOption selected;

    private void OnEnable()
    {
        if (girl == null || boy == null || confirmButton == null)
        {
            Debug.LogError("[CharacterSelect] Inspector alanları eksik, sahne çalışmayacak.");
            enabled = false;
            return;
        }

        girl.Clicked += OnCharacterClicked;
        boy.Clicked += OnCharacterClicked;
        confirmButton.onClick.AddListener(Confirm);
        confirmButton.interactable = false;
    }

    private void OnDisable()
    {
        if (girl != null) girl.Clicked -= OnCharacterClicked;
        if (boy != null) boy.Clicked -= OnCharacterClicked;
        if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
    }

    private void OnCharacterClicked(CharacterOption chosen)
    {
        CharacterOption other = chosen == girl ? boy : girl;

        selected = chosen;
        chosen.SetState(OptionState.Selected);
        other.SetState(OptionState.Dimmed);
        confirmButton.interactable = true;
    }

    private void Confirm()
    {
        if (selected == null) return;

        confirmButton.interactable = false; // çift tıklamayı engeller
        GameSession.SelectCharacter(selected.Character);
        Debug.Log("[CharacterSelect] Seçilen karakter: " + selected.Character);
        SceneLoader.Load(SceneLoader.Village);
    }
}