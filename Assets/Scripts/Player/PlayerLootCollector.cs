using UnityEngine;

// Хранит монеты и вызывает лечение через PlayerHealth.Heal()
public class PlayerLootCollector : MonoBehaviour
{
    [Header("Данные игрока")]
    public int coins = 0; // сколько монет собрано

    [Header("Ссылка на здоровье")]
    public PlayerHealth playerHealth;

    private void Awake()
    {
        // Если в инспекторе не назначили — найдём на этом же объекте
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    // Добавляем монеты
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        Debug.Log($"Монеты: +{amount}. Всего: {coins}");
    }

    // Лечим игрока (используем твой метод Heal)
    public void Heal(int amount)
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerLootCollector: не найден PlayerHealth!");
            return;
        }

        playerHealth.Heal(amount);
    }
}
