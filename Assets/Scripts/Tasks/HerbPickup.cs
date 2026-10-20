using System;
using UnityEngine;

// Sorumluluk: Ormandaki tek bir şifalı ot. Oyuncu E tuşuyla toplar, ot envantere eklenir
// ve nesne kapanır. Sadece oyuncu toplayabilir (Fındık'ın sağ tık komutuyla toplanmaz).
public class HerbPickup : MonoBehaviour, IPlayerInteractable
{
    [Header("Ot")]
    [SerializeField] private string herbName = "Şifalı ot";   // Örn. Nane, Papatya, Adaçayı
    [SerializeField] private int amount = 1;

    [Header("Bağlantı")]
    [SerializeField] private PlayerInventory inventory;       // Boşsa sahnede otomatik bulunur

    public event Action<HerbPickup> Collected;

    public bool IsCollected { get; private set; }

    public string PlayerPrompt => $"{herbName} topla";
    public bool CanInteract => !IsCollected;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindAnyObjectByType<PlayerInventory>();
    }

    public void InteractByPlayer()
    {
        if (IsCollected) return;
        IsCollected = true;

        if (inventory != null)
            inventory.Add(ItemId.SifaliOt, amount);

        // Önce kendi mesajını göster; HerbField son otta "yeterli ot topladın" mesajıyla bunun üstüne yazar
        HintUI.Say($"{herbName} toplandı.");

        Collected?.Invoke(this);
        gameObject.SetActive(false);
    }
}
