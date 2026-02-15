using UnityEngine;

// PlayerHealth — здоровье игрока.
// Вешаем на объект Player.
public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Ссылка на HUD (UI)")]
    [SerializeField] private HUDController hud;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private CharacterSFX3D sfx3D; // 3D звук игрока

    private void Awake()
    {
        // 1) Берём 3D звук с префаба игрока (если есть)
        sfx3D = GetComponent<CharacterSFX3D>();

        // 2) Ищем HUD, если не назначен вручную
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        // 3) Стартовое здоровье
        currentHealth = maxHealth;

        // 4) Обновляем UI
        UpdateHud();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        UpdateHud();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        currentHealth -= damage;

        // ✅ 3D звук получения урона игроком
        sfx3D?.PlayHit();

        if (currentHealth < 0)
            currentHealth = 0;

        UpdateHud();

        if (currentHealth == 0)
            Die();
    }

    private void UpdateHud()
    {
        hud?.SetHealth(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");

        // ✅ Показать UI поражения
        hud?.ShowGameOver();

        // Позже можно отключить управление игроком
    }
}
