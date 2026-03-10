using System.Collections;
using UnityEngine;

// EnemyHealth — отвечает только за здоровье врага и смерть.
// НЕ двигает, НЕ атакует, НЕ хранит шансы дропа.
[RequireComponent(typeof(Collider))]


public class EnemyHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 50; // максимум HP
    [SerializeField] private int currentHealth;  // текущее HP

    [Header("Смерть")]
    [SerializeField] private float destroyDelay = 2f; // через сколько секунд удалить объект

    [Header("VFX (эффекты)")]
    [SerializeField] private GameObject deathVfxPrefab;
    // Сюда перетащишь префаб эффекта смерти (например VFX_EnemyDeath)

    [SerializeField] private Vector3 deathVfxOffset = new Vector3(0f, 0.5f, 0f);
    // Смещение эффекта вверх, чтобы не "в полу". Можно менять в инспекторе.

    [Header("Визуальная реакция (мигание)")]
    [SerializeField] private float flashTime = 0.1f;

    private bool isDead = false;

    private Renderer rend;
    private Color originalColor;

    // Чтобы другие скрипты могли узнать, жив враг или нет
    public bool IsDead => isDead;

    private CharacterSFX3D sfx3D;
    // Базовое значение HP, чтобы мы могли честно умножать от "оригинала"
    private int baseMaxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
        //Звук 3Д
        sfx3D = GetComponent<CharacterSFX3D>();
        baseMaxHealth = maxHealth; // запоминаем исходное значение из инспектора
    }

    // Это будет вызывать игрок (меч/пуля/удар)
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (damage <= 0) return;

        currentHealth -= damage;

        // Лёгкая реакция: мигнуть белым
        if (rend != null)
            StartCoroutine(DamageFlash());

        // ✅ Если умер — уходим в Die (там будет PlayDeath)
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // ✅ Если выжил — играем звук получения удара
        sfx3D?.PlayHit();
    }

    // Спавн эффекта смерти
    private void SpawnDeathVfx()
    {
        // Если в инспекторе ничего не назначено — просто ничего не делаем
        if (deathVfxPrefab == null) return;

        // Где появится эффект: позиция врага + смещение
        Vector3 pos = transform.position + deathVfxOffset;

        // Создаём эффект в мире
        // Важно: сам префаб эффекта должен сам уничтожаться (Loop OFF + Stop Action = Destroy)
        Instantiate(deathVfxPrefab, pos, Quaternion.identity);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 0) СНАЧАЛА спавним VFX смерти (пока враг ещё существует)
        SpawnDeathVfx();

        // 1) Отключаем движение и атаку, чтобы “труп” ничего не делал
        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null)
            chase.enabled = false;

        EnemyMeleeAttack attack = GetComponent<EnemyMeleeAttack>();
        if (attack != null)
            attack.enabled = false;

        // 2) Визуально делаем серым
        if (rend != null)
            rend.material.color = Color.gray;

        // 3) Отключаем коллайдер
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4) Отключаем физику (если есть Rigidbody)
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 5) Дроп лута (через отдельный компонент LootDropper)
        GetComponent<LootDropper>()?.Drop();

        // 6) Сообщаем системе волн/спавнеру (если этот компонент есть)
        GetComponent<EnemyDeathNotifier>()?.NotifyKilled();

        // 7) Удаляем врага через задержку
        Destroy(gameObject, destroyDelay);

        sfx3D?.PlayDeath(); // ✅ 3D смерть
    }

    private IEnumerator DamageFlash()
    {
        rend.material.color = Color.white;
        yield return new WaitForSeconds(flashTime);

        // Если не умер — вернём исходный цвет
        if (!isDead)
            rend.material.color = originalColor;
    }
    // Вызывается спавнером сразу после Instantiate врага
    public void ApplyDifficulty(float hpMultiplier)
    {
        // Защита от странных значений
        if (hpMultiplier <= 0f) hpMultiplier = 1f;

        // Пересчитываем максимум и сразу текущее HP
        maxHealth = Mathf.Max(1, Mathf.CeilToInt(baseMaxHealth * hpMultiplier));
        currentHealth = maxHealth;
    }
}
