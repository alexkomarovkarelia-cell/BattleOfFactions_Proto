using UnityEngine;

// LootDropper висит на враге.
// EnemyHealth при смерти вызывает Drop(), и здесь создаются pickup'ы по LootTable.
public class LootDropper : MonoBehaviour
{
    [Header("Таблица дропа")]
    public LootTable lootTable;

    [Header("Точка дропа (если пусто — берём позицию врага)")]
    public Transform dropPoint;

    [Header("Разброс вокруг точки")]
    public float scatterRadius = 0.5f;

    public void Drop()
    {
        if (lootTable == null || lootTable.entries == null) return;

        Vector3 basePos = dropPoint != null ? dropPoint.position : transform.position;

        foreach (var entry in lootTable.entries)
        {
            if (entry == null || entry.pickupPrefab == null) continue;

            // Проверяем шанс
            if (Random.value > entry.chance)
                continue;

            // Выбираем amount
            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

            // Позиция со случайным разбросом по XZ
            Vector2 rnd = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = basePos + new Vector3(rnd.x, 0f, rnd.y);

            // Создаём префаб
            GameObject go = Instantiate(entry.pickupPrefab, spawnPos, Quaternion.identity);

            // Передаём данные в WorldPickup (тип + количество/сила)
            WorldPickup pickup = go.GetComponent<WorldPickup>();
            if (pickup != null)
            {
                pickup.Init(entry.kind, amount);
            }
            else
            {
                Debug.LogWarning("На prefab нет WorldPickup: " + entry.pickupPrefab.name);
            }
        }
    }
}
