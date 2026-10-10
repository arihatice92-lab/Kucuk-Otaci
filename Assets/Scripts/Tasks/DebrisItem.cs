using System;
using UnityEngine;

// Sorumluluk: Selin getirdiği tek bir çöp parçası. Hem oyuncu (E tuşu) hem de
// Fındık (sağ tık komutu) tarafından toplanabilir. Toplanınca kendini kapatır ve olay duyurur.
public class DebrisItem : MonoBehaviour, IInteractable, IPlayerInteractable
{
    public event Action<DebrisItem> Collected;

    public bool IsCollected { get; private set; }

    // Fındık için (IInteractable)
    public string InteractionPrompt => "Çöpü topla";
    public void Interact() => Collect();

    // Oyuncu için (IPlayerInteractable)
    public string PlayerPrompt => "Çöpü topla";
    public bool CanInteract => !IsCollected;
    public void InteractByPlayer() => Collect();

    private void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;

        Collected?.Invoke(this);
        gameObject.SetActive(false);
    }
}
