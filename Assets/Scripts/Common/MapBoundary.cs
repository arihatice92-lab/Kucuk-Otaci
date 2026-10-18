using UnityEngine;

/// <summary>
/// Haritanın çevresine görünmez duvarlar kurar, oyuncunun sahne dışına düşmesini engeller.
/// Köşeleri Inspector'dan (Min / Max, dünya XZ koordinatı) girilir.
/// </summary>
public class MapBoundary : MonoBehaviour
{
    [Header("Sınır (dünya XZ)")]
    [SerializeField] private Vector2 min = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 max = new Vector2(50f, 50f);

    [Header("Duvar")]
    [SerializeField] private float wallHeight = 20f;
    [SerializeField] private float wallThickness = 2f;
    [SerializeField] private float baseY = -5f;

    private void Awake()
    {
        if (max.x <= min.x || max.y <= min.y)
        {
            Debug.LogError("MapBoundary: Max değerleri Min'den büyük olmalı.", this);
            return;
        }

        float width = max.x - min.x;
        float length = max.y - min.y;
        float centerX = (min.x + max.x) * 0.5f;
        float centerZ = (min.y + max.y) * 0.5f;
        float centerY = baseY + wallHeight * 0.5f;
        float half = wallThickness * 0.5f;

        // Duvarlar sınırın hemen dışına kurulur, içte kalan alan serbesttir.
        CreateWall("Wall_North", new Vector3(centerX, centerY, max.y + half),
            new Vector3(width + wallThickness * 2f, wallHeight, wallThickness));
        CreateWall("Wall_South", new Vector3(centerX, centerY, min.y - half),
            new Vector3(width + wallThickness * 2f, wallHeight, wallThickness));
        CreateWall("Wall_East", new Vector3(max.x + half, centerY, centerZ),
            new Vector3(wallThickness, wallHeight, length));
        CreateWall("Wall_West", new Vector3(min.x - half, centerY, centerZ),
            new Vector3(wallThickness, wallHeight, length));
    }

    private void CreateWall(string wallName, Vector3 worldCenter, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.position = worldCenter;

        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = size;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((min.x + max.x) * 0.5f, baseY + wallHeight * 0.5f, (min.y + max.y) * 0.5f);
        Vector3 size = new Vector3(max.x - min.x, wallHeight, max.y - min.y);
        Gizmos.DrawWireCube(center, size);
    }
}