using UnityEngine;

// SpawnZone
// Это простая зона спавна для арены.
//
// MVP-идея:
// - зона задаётся BoxCollider'ом
// - спавнер может запросить случайную точку внутри зоны
// - пока без сложных проверок стен, ловушек и NavMesh
//
// Позже сюда можно будет добавить:
// - приоритет зоны
// - разрешённые типы врагов
// - запрет для босса / элиты
// - вес выбора
// - безопасную дистанцию до игрока
[DisallowMultipleComponent]
public class SpawnZone : MonoBehaviour
{
    [Header("Основная информация")]
    [SerializeField] private string zoneId = "zone_01";

    [Header("Границы зоны")]
    [SerializeField] private BoxCollider zoneCollider;

    [Header("Отладка")]
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private bool drawGizmo = true;

    public string ZoneId => zoneId;
    public BoxCollider ZoneCollider => zoneCollider;

    private void Reset()
    {
        // Если добавили скрипт на объект —
        // пробуем автоматически найти BoxCollider.
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider>();
    }

    private void Awake()
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider>();
    }

    // Вернуть случайную точку внутри BoxCollider зоны.
    // Пока без дополнительных проверок.
    public Vector3 GetRandomPointInside()
    {
        if (zoneCollider == null)
        {
            Debug.LogWarning($"SpawnZone: на зоне {name} не назначен BoxCollider.");
            return transform.position;
        }

        Bounds bounds = zoneCollider.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        // Y берём с позиции объекта зоны.
        // Если позже понадобится точнее — можно будет делать raycast в пол.
        float y = transform.position.y;

        return new Vector3(randomX, y, randomZ);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo || zoneCollider == null)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
    }
}