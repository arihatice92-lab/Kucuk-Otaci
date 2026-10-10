using System.Collections;
using TMPro;
using UnityEngine;

// Sorumluluk: Ekranda etkileşim ipucunu ("E - ...") ve kısa mesajları gösterir.
// Diğer scriptler HintUI.Prompt(...) ve HintUI.Say(...) ile çağırır; sahnede HintUI yoksa
// mesajlar Console'a yazılır, oyun çökmez.
public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [Header("Metin alanları (TextMeshPro)")]
    [SerializeField] private TMP_Text promptText;   // "E - Erzağı yılana ver"
    [SerializeField] private TMP_Text messageText;  // "Fındık köprüyü onardı!"

    private Coroutine messageRoutine;
    private string lastPrompt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        ApplyPrompt(null);
        if (messageText != null) messageText.gameObject.SetActive(false);
    }

    // ---- Kolay erişim (HintUI sahnede yoksa hata vermez) ----
    public static void Prompt(string text)
    {
        if (Instance != null) Instance.SetPrompt(text);
    }

    public static void Say(string text, float seconds = 3f)
    {
        if (Instance != null) Instance.ShowMessage(text, seconds);
        else Debug.Log("[Hint] " + text);
    }

    // ---- Etkileşim ipucu (sürekli görünür, null verilirse gizlenir) ----
    public void SetPrompt(string text)
    {
        if (text == lastPrompt) return;   // aynı metni her karede tekrar yazma
        ApplyPrompt(text);
    }

    private void ApplyPrompt(string text)
    {
        lastPrompt = text;
        if (promptText == null) return;

        bool hasText = !string.IsNullOrEmpty(text);
        promptText.gameObject.SetActive(hasText);
        if (hasText) promptText.text = text;
    }

    // ---- Kısa süreli mesaj ----
    public void ShowMessage(string text, float seconds = 3f)
    {
        if (messageText == null)
        {
            Debug.Log("[Hint] " + text);
            return;
        }

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }
}
