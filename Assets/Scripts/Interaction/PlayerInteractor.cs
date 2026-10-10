using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Sorumluluk: Oyuncunun yakınındaki en yakın IPlayerInteractable'ı bulur,
// ekranda "E - ..." ipucunu gösterir ve E'ye basılınca etkileşimi başlatır.
// Oyuncu (Player) nesnesine eklenir.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private Key interactKey = Key.E;

    private readonly Collider[] buffer = new Collider[16];

    private void Update()
    {
        // Diyalog kutusuna yazı yazarken "e" harfi etkileşimi tetiklemesin
        if (IsTyping())
        {
            HintUI.Prompt(null);
            return;
        }

        IPlayerInteractable current = FindNearest();
        HintUI.Prompt(current != null ? $"{interactKey} - {current.PlayerPrompt}" : null);

        if (current == null) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb[interactKey].wasPressedThisFrame)
            current.InteractByPlayer();
    }

    private IPlayerInteractable FindNearest()
    {
        Vector3 origin = transform.position + Vector3.up;
        int count = Physics.OverlapSphereNonAlloc(origin, interactRadius, buffer, ~0, QueryTriggerInteraction.Collide);

        IPlayerInteractable best = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            IPlayerInteractable candidate = buffer[i].GetComponentInParent<IPlayerInteractable>();
            if (candidate == null || !candidate.CanInteract) continue;

            Vector3 closest = buffer[i].bounds.ClosestPoint(origin);
            float sqrDistance = (closest - origin).sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = candidate;
            }
        }

        return best;
    }

    private static bool IsTyping()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected == null) return false;

        TMP_InputField field = selected.GetComponent<TMP_InputField>();
        return field != null && field.isFocused;
    }
}
