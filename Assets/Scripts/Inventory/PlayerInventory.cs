using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemId
{
    Erzak,      // evden çıkarken alınan yol erzağı (yılana verilecek)
    SifaliOt    // ormanda toplanan şifalı otlar
}

// Sorumluluk: Oyuncunun taşıdığı eşyaları ve sayılarını tutar. Başka bir şey yapmaz.
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public struct StartItem
    {
        public ItemId item;
        public int count;
    }

    [Header("Oyuna başlarken oyuncunun yanında olan eşyalar")]
    [SerializeField] private List<StartItem> startItems = new List<StartItem>
    {
        new StartItem { item = ItemId.Erzak, count = 1 }
    };

    private readonly Dictionary<ItemId, int> items = new Dictionary<ItemId, int>();

    public event Action<ItemId, int> OnItemChanged;   // (eşya, yeni sayı)

    private void Awake()
    {
        foreach (StartItem s in startItems)
            Add(s.item, s.count);
    }

    public int GetCount(ItemId id) => items.TryGetValue(id, out int c) ? c : 0;

    public bool Has(ItemId id, int count = 1) => GetCount(id) >= count;

    public void Add(ItemId id, int count = 1)
    {
        if (count <= 0) return;
        items[id] = GetCount(id) + count;
        OnItemChanged?.Invoke(id, items[id]);
    }

    // Yeterli eşya varsa düşer ve true döner; yoksa hiçbir şey değişmez ve false döner.
    public bool TryConsume(ItemId id, int count = 1)
    {
        if (!Has(id, count)) return false;
        items[id] = GetCount(id) - count;
        OnItemChanged?.Invoke(id, items[id]);
        return true;
    }
}
