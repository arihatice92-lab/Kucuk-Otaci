// Oyuncunun kendisinin (E tuşuyla) etkileştiği nesneler için arayüz.
// IInteractable ayrı tutuldu: o arayüz Fındık'ın sağ tık komutlarıyla çalışır.
// Yılan gibi sadece oyuncunun yapabileceği işler bu arayüzü kullanır,
// böylece sağ tıkla Fındık yanlışlıkla yılana gönderilmez.
public interface IPlayerInteractable
{
    string PlayerPrompt { get; }   // Ekranda "E - ..." yanında görünen metin
    bool CanInteract { get; }      // false ise ipucu gösterilmez
    void InteractByPlayer();
}
