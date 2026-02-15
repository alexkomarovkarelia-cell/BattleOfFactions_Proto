using UnityEngine;

// Этот скрипт висит на игроке.
// Его задача: принять лут (монеты/аптечка/предметы) и применить его.
public class PlayerLootReceiver : MonoBehaviour
{
    [Header("Ссылки (на другие компоненты)")]
    [SerializeField] private HUDController hud;         // UI: показывает монеты и т.д.
    [SerializeField] private PlayerHealth playerHealth; // Здоровье игрока
    [SerializeField] private SFXPlayer sfx;             // ✅ Звуки (монеты/лечилка)

    [Header("Данные игрока")]
    [SerializeField] private int coins = 0; // текущее количество монет

    private void Awake()
    {
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // ✅ Ищем SFXPlayer на сцене (объект SFX)
        if (sfx == null)
            sfx = FindFirstObjectByType<SFXPlayer>();

        if (hud == null)
            Debug.LogWarning("HUDController не найден. Монеты будут считаться, но UI не обновится.");

        if (playerHealth == null)
            Debug.LogWarning("PlayerHealth не найден на игроке. Лечение работать не будет.");
    }

    private void Start()
    {
        hud?.SetCoins(coins);
    }

    public void Receive(LootKind kind, int amount)
    {
        if (amount <= 0) return;

        switch (kind)
        {
            case LootKind.Coins:
                coins += amount;
                hud?.SetCoins(coins);

                // ✅ звук монеты
                sfx?.PlayCoin();
                break;

            case LootKind.Medkit:
                if (playerHealth != null)
                    playerHealth.Heal(amount);

                // ✅ звук лечения
                sfx?.PlayMedkit();
                break;

            case LootKind.Item:
                break;

            default:
                Debug.LogWarning("Не обработан LootKind: " + kind);
                break;
        }
    }
}
