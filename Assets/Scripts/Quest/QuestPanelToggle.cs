using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sorumluluk: Tab tuşuyla görev listesini açıp kapatır. Listenin içeriğini bilmez.
/// Kapatılacak nesnenin kendisine değil, her zaman açık kalan bir nesneye (ör. HUDCanvas) eklenmelidir,
/// aksi halde nesne kapanınca bu script de çalışmayı bırakır ve liste bir daha açılamaz.
/// </summary>
public class QuestPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Key toggleKey = Key.Tab;

    private void Awake()
    {
        if (panel == null)
        {
            Debug.LogError("QuestPanelToggle: Panel atanmamış.", this);
            enabled = false;
            return;
        }

        if (panel == gameObject)
        {
            Debug.LogError("QuestPanelToggle: Bu script kapatılacak nesnenin kendisinde olamaz, HUDCanvas gibi başka bir nesneye ekle.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Diyalog kutusuna yazarken Tab tuşu listeyi açıp kapatmasın.
        if (UIInputState.IsTyping) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[toggleKey].wasPressedThisFrame)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}