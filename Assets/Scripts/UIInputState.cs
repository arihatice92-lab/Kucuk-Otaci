using TMPro;
using UnityEngine.EventSystems;


/// Kullanıcının UI üzerinde (özellikle TMP yazı kutularında) aktif olarak
/// yazı yazıp yazmadığını kontrol eden yardımcı statik sınıf.
/// Oyuncu hareketi ve tuş komutlarının yazı yazarken tetiklenmesini engeller.

public static class UIInputState
{
   
    /// Eğer oyuncu şu anda bir Input Field'a odaklanmış ve yazı yazıyorsa true döner.
  
    public static bool IsTyping
    {
        get
        {
            // EventSystem yoksa veya hiyerarşide seçili bir UI nesnesi bulunmuyorsa yazmıyordur
            var sel = EventSystem.current != null 
                ? EventSystem.current.currentSelectedGameObject 
                : null;
                
            if (sel == null) return false;
            
            // Seçili nesnenin bir TextMeshPro InputField bileşeni olup olmadığını ve
            // imlecin o kutunun içinde odaklı (focused) olup olmadığını kontrol eder
            var field = sel.GetComponent<TMP_InputField>();
            return field != null && field.isFocused;
        }
    }
}