using UnityEngine;

// Этот скрипт висит на игроке.
// Его задача: принять лут (монеты/аптечка/предметы) и применить его.
// ВАЖНО (после Блока B):
// - ЗВУК и VFX подбора делает САМ ПРЕДМЕТ ЛУТА (WorldPickup + AudioSource 3D + VFX)
// - PlayerLootReceiver хранит только ЛОГИКУ (монеты/лечение/инвентарь)
public class PlayerLootReceiver : MonoBehaviour
{
    [Header("Ссылки (на другие компоненты)")]
    [SerializeField] private HUDController hud;         // UI: показывает монеты и т.д.
    [SerializeField] private PlayerHealth playerHealth; // Здоровье игрока

    [Header("Данные игрока")]
    [SerializeField] private int coins = 0; // текущее количество монет

    private void Awake()
    {
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

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
                // Звук монеты НЕ здесь. Он в WorldPickup (3D).
                break;

            case LootKind.Medkit:
                if (playerHealth != null)
                    playerHealth.Heal(amount);
                // Звук лечения НЕ здесь. Он в WorldPickup (3D).
                break;

            case LootKind.Item:
                // Потом добавим инвентарь
                break;

            default:
                Debug.LogWarning("Не обработан LootKind: " + kind);
                break;
        }
    }
}