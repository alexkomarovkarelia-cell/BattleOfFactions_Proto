using System;
using UnityEngine;

// ObjectHealth
// БАЗОВЫЙ класс здоровья / прочности.
//
// Главная идея:
// - тут живёт ОБЩАЯ логика значения;
// - тут НЕ живёт UI;
// - тут НЕ живёт дроп;
// - тут НЕ живёт логика конкретного врага;
// - тут НЕ живёт Game Over.
//
// Этот класс нужен как фундамент для:
// - игрока;
// - врагов;
// - босса;
// - разрушаемых объектов;
//
// Важно:
// здесь уже есть событие OnHealthChanged.
// Это значит, что UI и другие системы могут подписываться
// на изменение здоровья отдельно, не вшиваясь в базу напрямую.

public abstract class ObjectHealth : MonoBehaviour, IDamageable
{
    [Header("Base Health Settings")]
    [SerializeField] protected int maxHealth = 100;

    // currentHealth оставляем protected,
    // чтобы наследники могли его читать при необходимости.
    [SerializeField] protected int currentHealth;

    // Флаг смерти / разрушения.
    // Нужен, чтобы объект не умирал несколько раз подряд.
    protected bool isDead = false;

    // Публичные свойства только для чтения извне.
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    // Событие изменения здоровья.
    // Передаём:
    // - текущее значение
    // - максимальное значение
    //
    // Сюда потом может подписаться:
    // - HUD игрока
    // - полоска HP босса
    // - полоска прочности объекта
    public event Action<int, int> OnHealthChanged;

    // Awake делаем virtual,
    // чтобы PlayerHealth / EnemyHealth могли расширять его,
    // но ОБЯЗАТЕЛЬНО вызывали base.Awake().
    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        // Сразу отправляем стартовое значение.
        // Важно: если подписчик подключится позже,
        // он сможет отдельно синхронизироваться в Start/OnEnable.
        RaiseHealthChanged();
    }

    // Общий вход для получения урона.
    public virtual void TakeDamage(int damage)
    {
        // Если уже мёртв - урон не принимаем.
        if (isDead) return;

        // Защита от странных вызовов.
        if (damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        // Определяем, смертельный ли удар.
        bool isLethal = currentHealth <= 0;

        // Сообщаем всем подписчикам,
        // что здоровье изменилось.
        RaiseHealthChanged();

        // Хук для наследников:
        // можно проиграть звук, вспышку, и т.д.
        OnDamageTaken(damage, isLethal);

        // Если здоровье закончилось - умираем.
        if (isLethal)
        {
            Die();
        }
    }

    // Общее лечение / восстановление.
    public virtual void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        RaiseHealthChanged();

        OnHealed(amount);
    }

    // Общая логика смерти / разрушения.
    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;

        // Наследник сам решает,
        // что делать после смерти / разрушения.
        OnDeath();
    }

    // Отдельный метод, который безопасно вызывает событие.
    protected void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Хук для наследников:
    // например проиграть звук удара или вспышку.
    protected virtual void OnDamageTaken(int damageAmount, bool isLethal)
    {
    }

    // Хук для наследников:
    // если потом захочешь особую реакцию на лечение.
    protected virtual void OnHealed(int healAmount)
    {
    }

    // Хук для наследников:
    // игрок -> Game Over
    // враг -> дроп / VFX / отключение AI
    // ящик -> разрушение
    protected virtual void OnDeath()
    {
    }

    // Вспомогательный метод для будущих случаев,
    // когда нужно поменять максимум HP, например от сложности.
    //
    // refillToMax = true
    // значит после смены maxHealth текущее здоровье
    // тоже сразу станет полным.
    protected void SetMaxHealth(int newMaxHealth, bool refillToMax)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillToMax)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        RaiseHealthChanged();
    }
}