using UnityEngine;
using UnityEngine.InputSystem;

public class CompanionCommandInput : MonoBehaviour
{
    [SerializeField] private GameObject companion;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxRayDistance = 50f;
    [SerializeField] private Transform playerRoot;

    private ICompanionCommandReceiver receiver;

    private bool TryGetClickHit(out RaycastHit result)
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            Transform t = h.collider.transform;
            if (companion != null && t.IsChildOf(companion.transform)) continue; // Fındık'ın kendisi
            if (playerRoot != null && t.IsChildOf(playerRoot)) continue;         // oyuncu
            if (h.collider.isTrigger && h.collider.GetComponentInParent<IInteractable>() == null) continue; // etkileşimsiz trigger alanları

            result = h;
            return true;
        }

        result = default;
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
