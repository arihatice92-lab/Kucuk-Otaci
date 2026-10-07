using UnityEngine;
using UnityEngine.InputSystem;

// GEÇİCİ test aracı: gerçek görev mekanikleri yazılana kadar görevleri tuşla tamamlar.
// F1 köprü, F2 yılan, F3 çöp, F4 kedi.
// (Sayı tuşlarını kullanmadık, çünkü diyalog kutusuna yazı yazarken tetiklenirdi.)
// Sadece Unity Editor'de çalışır; build'de etkisizdir.
public class QuestDebugKeys : MonoBehaviour
{
    [SerializeField] private QuestManager quests;

#if UNITY_EDITOR
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || quests == null) return;

        if (kb.f1Key.wasPressedThisFrame) quests.Complete(QuestId.BridgeRepaired);
        if (kb.f2Key.wasPressedThisFrame) quests.Complete(QuestId.SnakeResolved);
        if (kb.f3Key.wasPressedThisFrame) quests.Complete(QuestId.DebrisCleared);
        if (kb.f4Key.wasPressedThisFrame) quests.Complete(QuestId.KittenRescued);
    }
#endif
}
