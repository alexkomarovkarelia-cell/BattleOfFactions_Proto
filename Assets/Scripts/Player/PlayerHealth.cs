using UnityEngine;

// PlayerHealth — здоровье игрока.
// Вешаем на объект Player.
public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private int maxHealth = 100; // максимум HP
    [SerializeField] private int currentHealth;   // текущее HP

    [Header("Ссылка на HUD (UI)")]
    [SerializeField] private HUDController hud;
    // HUDController у тебя на Canvas/HUD объекте и умеет показывать HP через SetHealth()

    // Чтобы другие скрипты могли прочитать здоровье (но не менять напрямую)
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        // Если HUD не назначен вручную — попробуем найти на сцене автоматически
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        // На старте ставим полное здоровье
        currentHealth = maxHealth;

        // ✅ Сразу обновляем UI, чтобы в начале игры показало правильные значения
        UpdateHud();
    }

    // Лечение (медкит, будущие умения)
    public void Heal(int amount)
    {
        // Защита: если 0 или отрицательное — не лечим
        if (amount <= 0) return;

        currentHealth += amount;

        // Ограничиваем сверху: нельзя выше maxHealth
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        // ✅ Обновляем UI после лечения
        UpdateHud();
    }

    // Получение урона (враг, ловушка и т.п.)
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        currentHealth -= damage;
        // ✅ Игрок получил урон — играем hurt
        SFXPlayer.I?.PlayHurtPlayer();

        // Ограничиваем снизу: нельзя ниже 0
        if (currentHealth < 0)
            currentHealth = 0;

        // ✅ Обновляем UI после урона
        UpdateHud();

        // Если HP стало 0 — умираем
        if (currentHealth == 0)
            Die();
    }

    // Вынесли обновление HUD в отдельный метод, чтобы не повторять код
    private void UpdateHud()
    {
        // hud?. означает: "вызвать, только если hud не null"
        // У тебя в HUDController метод называется SetHealth(float current, float max)
        hud?.SetHealth(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");

       

        // 1) Показать UI поражения
        if (hud != null)
        {
            hud.ShowGameOver();
        }
        else
        {
            // на всякий случай, если ссылка не назначилась
            HUDController foundHud = FindFirstObjectByType<HUDController>();
            if (foundHud != null) foundHud.ShowGameOver();
        }

        // 2) Тут позже можно отключить управление игроком, если нужно
    }


    // Тест кнопками (можно оставить на время теста, потом убрать)
}
