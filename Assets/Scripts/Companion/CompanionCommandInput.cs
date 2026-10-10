using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CompanionCommandInput : MonoBehaviour
{
    [SerializeField] private GameObject companion;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxRayDistance = 50f;
    [SerializeField] private Transform playerRoot;

    private ICompanionCommandReceiver receiver;
    private static bool IsTyping()
    {
        var go = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (go == null) return false;
        var input = go.GetComponent<TMP_InputField>();
        return input != null && input.isFocused;
    }
    private bool TryGetClickHit(out RaycastHit result)
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] all = Physics.RaycastAll(ray, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(all, (a, b) => a.distance.CompareTo(b.distance));

        // 1) Etkileşilebilir nesneler arasından en küçük collider'ı olanı seç
        //    (kol, Bridge'in büyük trigger hacmine göre kazanır)
        bool found = false;
        RaycastHit best = default;
        float bestVolume = float.MaxValue;

        foreach (var h in all)
        {
            if (IsCompanionOrPlayer(h.collider.transform)) continue;
            if (h.collider.GetComponentInParent<IInteractable>() == null) continue;

            Vector3 s = h.collider.bounds.size;
            float volume = s.x * s.y * s.z;
            if (volume < bestVolume)
            {
                bestVolume = volume;
                best = h;
                found = true;
            }
        }

        if (found)
        {
            result = best;
            return true;
        }

        // 2) Etkileşilebilir nesne yoksa: ilk trigger olmayan, Fındık/oyuncu olmayan çarpışma (GoTo için)
        foreach (var h in all)
        {
            if (IsCompanionOrPlayer(h.collider.transform)) continue;
            if (h.collider.isTrigger) continue;

            result = h;
            return true;
        }

        result = default;
        return false;
    }

    private bool IsCompanionOrPlayer(Transform t)
    {
        if (companion != null && t.IsChildOf(companion.transform)) return true;
        if (playerRoot != null && t.IsChildOf(playerRoot)) return true;
        return false;
    }
    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (companion != null) receiver = companion.GetComponent<ICompanionCommandReceiver>();
        if (receiver == null) Debug.LogWarning("[CompanionCommandInput] Receiver bulunamadı.");
    }

    private void Update()
    {
        if (IsTyping()) return;
        if (receiver == null) return;
        var kb = Keyboard.current; var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.fKey.wasPressedThisFrame) receiver.Receive(CompanionCommand.Follow());
        if (kb.gKey.wasPressedThisFrame) receiver.Receive(CompanionCommand.Stay());

        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (!TryGetClickHit(out RaycastHit hit)) return;

            Debug.Log($"[Komut] Tıklanan nesne: {hit.collider.name} (nokta: {hit.point})");

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            receiver.Receive(interactable != null
                ? CompanionCommand.Interact(hit.point, interactable)
                : CompanionCommand.GoTo(hit.point));
        }
    }
}
