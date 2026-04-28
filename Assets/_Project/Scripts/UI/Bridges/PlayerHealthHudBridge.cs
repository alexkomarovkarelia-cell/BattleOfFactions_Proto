using UnityEngine;

// PlayerHealthHudBridge
// Это ПЕРВЫЙ маленький мост между игровыми данными и HUD.
//
// Важно:
// - он НЕ хранит здоровье;
// - он НЕ решает боевую логику;
// - он НЕ является главным сборщиком всего интерфейса;
//
// Его задача только одна:
// слушать OnHealthChanged у PlayerHealth
// и передавать данные в HUDController.SetHealth(...).
//
// Позже по такому же принципу можно сделать:
// - ArenaHudBridge
// - CoinsHudBridge
// - AbilityHudBridge
// - TargetHudBridge
//
// То есть это первый локальный bridge/presenter слой.

public class PlayerHealthHudBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HUDController hud;

    private void Awake()
    {
        // Если PlayerHealth не назначен вручную —
        // пробуем найти его на этом же объекте.
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // Если HUD не назначен вручную —
        // ищем его на сцене.
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        // На случай если событие прошло раньше,
        // один раз синхронизируем HUD руками при старте.
        if (playerHealth != null && hud != null)
        {
            hud.SetHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (hud == null) return;

        hud.SetHealth(currentHealth, maxHealth);
    }
}