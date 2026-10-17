using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TypewriterText : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 28f;

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        Clear();
    }

    public IEnumerator Type(string text)
    {
        label.text = text;
        label.maxVisibleCharacters = 0;
        label.ForceMeshUpdate();

        int total = label.textInfo.characterCount;
        float shown = 0f;

        while (label.maxVisibleCharacters < total)
        {
            shown += charactersPerSecond * Time.deltaTime;
            label.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
    }

    public void Clear()
    {
        label.text = "";
        label.maxVisibleCharacters = 0;
    }
}