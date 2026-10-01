// Açıklama: Sahnedeki etkileşime girilebilir tüm nesneler için temel arayüz.
// OOP Prensibi: Polimorfizm ve Bağımlılıkların Tersine Çevrilmesi (DIP).
public interface IInteractable
{
    string InteractionPrompt { get; }
    void Interact();
}