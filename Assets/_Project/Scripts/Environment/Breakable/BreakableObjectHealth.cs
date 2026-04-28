using UnityEngine;

// BreakableObjectHealth
// Это простая прочность разрушаемого объекта.
//
// Важно:
// - скрипт построен на общей базе ObjectHealth;
// - значит объект получает урон тем же принципом,
//   что игрок и враги;
// - но реакция на "ноль" у него СВОЯ:
//   не смерть, а разрушение.
//
// На текущем этапе это наш первый тест,
// что общая система годится и для неживых объектов.

[RequireComponent(typeof(Collider))]
public class BreakableObjectHealth : ObjectHealth
{
    [Header("Destroy Settings")]
    [SerializeField] private float destroyDelay = 0f;
    // Через сколько секунд удалить объект после разрушения.
    // 0 = удалить сразу.

    [Header("Optional VFX")]
    [SerializeField] private GameObject breakVfxPrefab;
    [SerializeField] private Vector3 breakVfxOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Optional Visual Feedback")]
    [SerializeField] private bool tintGrayOnBreak = true;
    // Если true — перед удалением объект станет серым.

    private Renderer rend;
    private Color originalColor;

    protected override void Awake()
    {
        // Сначала инициализируем базовую прочность
        base.Awake();

        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    // Реакция объекта на получение урона.
    // Пока оставим пусто или можно позже добавить вспышку/звук.
    protected override void OnDamageTaken(int damageAmount, bool isLethal)
    {
        // На этом этапе можно оставить пустым.
        // Позже сюда легко добавить:
        // - мигание,
        // - звук удара по объекту,
        // - тряску,
        // - пыль.
    }

    // Реакция на "смерть" для прочности.
    // Для неживого объекта это разрушение.
    protected override void OnDeath()
    {
        // 1) Спавним эффект разрушения, если он назначен
        SpawnBreakVfx();

        // 2) Если нужно — делаем объект серым перед удалением
        if (tintGrayOnBreak && rend != null)
            rend.material.color = Color.gray;

        // 3) Отключаем коллайдер, чтобы по объекту
        // больше нельзя было бить/сталкиваться
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // 4) Если есть Rigidbody — отключаем физику
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // 5) Удаляем объект
        Destroy(gameObject, destroyDelay);
    }

    private void SpawnBreakVfx()
    {
        if (breakVfxPrefab == null)
            return;

        Vector3 pos = transform.position + breakVfxOffset;
        Instantiate(breakVfxPrefab, pos, Quaternion.identity);
    }
}