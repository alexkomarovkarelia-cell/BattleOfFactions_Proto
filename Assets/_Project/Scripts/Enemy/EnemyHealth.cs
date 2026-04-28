using System.Collections;
using UnityEngine;

// EnemyHealth
// Теперь это здоровье КОНКРЕТНО врага,
// построенное поверх общей базы ObjectHealth.
//
// Общая логика здоровья уже живёт в ObjectHealth.
// Здесь остаётся только то,
// что относится именно к врагу:
// - вспышка при попадании
// - VFX смерти
// - отключение AI
// - дроп
// - уведомление системе волн
// - удаление объекта

[RequireComponent(typeof(Collider))]
public class EnemyHealth : ObjectHealth
{
    [Header("Смерть")]
    [SerializeField] private float destroyDelay = 2f;

    [Header("VFX (эффекты)")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private Vector3 deathVfxOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Визуальная реакция (мигание)")]
    [SerializeField] private float flashTime = 0.1f;

    private Renderer rend;
    private Color originalColor;
    private CharacterSFX3D sfx3D;

    // Базовое значение maxHealth из инспектора.
    // Нужно, чтобы ApplyDifficulty честно умножал
    // исходное, а не уже изменённое значение.
    private int baseMaxHealth;

    protected override void Awake()
    {
        // Сначала инициализируем базовое здоровье
        base.Awake();

        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        sfx3D = GetComponent<CharacterSFX3D>();

        // Запоминаем исходный maxHealth
        baseMaxHealth = MaxHealth;
    }

    // Реакция врага на получение урона
    protected override void OnDamageTaken(int damageAmount, bool isLethal)
    {
        // Если удар НЕ смертельный —
        // мигаем и играем звук попадания.
        //
        // Это сохраняет поведение, близкое к твоему текущему.
        if (!isLethal)
        {
            if (rend != null)
                StartCoroutine(DamageFlash());

            sfx3D?.PlayHit();
        }
    }

    // Реакция врага на смерть
    protected override void OnDeath()
    {
        // 0) Сначала эффект смерти
        SpawnDeathVfx();

        // 1) Отключаем движение
        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null)
            chase.enabled = false;

        // 2) Отключаем ближнюю атаку
        EnemyMeleeAttack attack = GetComponent<EnemyMeleeAttack>();
        if (attack != null)
            attack.enabled = false;

        // 3) Визуально красим в серый
        if (rend != null)
            rend.material.color = Color.gray;

        // 4) Отключаем коллайдер
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // 5) Отключаем физику
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // 6) Дроп лута
        GetComponent<LootDropper>()?.Drop();

        // 7) Сообщаем системе волн / смерти врага
        GetComponent<EnemyDeathNotifier>()?.NotifyKilled();

        // 8) Звук смерти
        sfx3D?.PlayDeath();

        // 9) Удаляем объект через задержку
        Destroy(gameObject, destroyDelay);
    }

    // Спавн эффекта смерти
    private void SpawnDeathVfx()
    {
        if (deathVfxPrefab == null) return;

        Vector3 pos = transform.position + deathVfxOffset;
        Instantiate(deathVfxPrefab, pos, Quaternion.identity);
    }

    private IEnumerator DamageFlash()
    {
        rend.material.color = Color.white;
        yield return new WaitForSeconds(flashTime);

        // Если враг не умер — возвращаем исходный цвет
        if (!IsDead)
            rend.material.color = originalColor;
    }

    // Вызывается спавнером после создания врага
    public void ApplyDifficulty(float hpMultiplier)
    {
        if (hpMultiplier <= 0f) hpMultiplier = 1f;

        int newMaxHealth = Mathf.Max(1, Mathf.CeilToInt(baseMaxHealth * hpMultiplier));

        // Меняем maxHealth через базовый метод
        // и сразу восстанавливаем HP до полного.
        SetMaxHealth(newMaxHealth, true);
    }
}