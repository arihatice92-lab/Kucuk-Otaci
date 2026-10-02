using UnityEngine;

public class OllamaTester : MonoBehaviour
{
    [SerializeField] private OllamaClient client;
    [SerializeField, TextArea] private string testMessage = "Merhaba amca, ben şenlik için geldim.";

    private void Start()
    {
        StartCoroutine(client.Send(testMessage, result =>
        {
            if (result.Success)
                Debug.Log($"[{result.Decision} / {result.Mood}] {result.Dialogue}");
            else
                Debug.LogWarning($"HATA: {result.Error}");
        }));
    }
}
