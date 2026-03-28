using UnityEngine;

// PlayerAttack
// Этот скрипт отвечает ТОЛЬКО за логику базовой ближней атаки игрока.
//
// Что он делает:
// 1. Хранит параметры кулачного удара
// 2. Ищет врага в радиусе удара
// 3. Выбирает ОДНУ ближайшую цель
// 4. Наносит ей урон
//
// ВАЖНО:
// - Этот скрипт больше НЕ читает мышь и клавиатуру
// - Вызов атаки идёт только через PlayerInputHandler -> TryAttack()
public class PlayerAttack : MonoBehaviour
{
    [Header("Точка удара")]
    [SerializeField] private Transform attackPoint;
    // Это пустой объект перед игроком.
    // Из этой точки мы ищем врагов в радиусе.

    [Header("Слой врагов")]
    [SerializeField] private LayerMask enemyMask;
    // Здесь выбираем слой Enemy.

    [Header("Базовые параметры кулачного боя")]
    [SerializeField] private int baseDamage = 15;
    // Базовый урон кулаком

    [SerializeField] private float attackRange = 1.45f;
    // Радиус удара

    [SerializeField] private float attackCooldown = 0.30f;
    // Задержка между ударами

    // Время, когда игрок сможет ударить снова
    private float nextAttackTime = 0f;

    // Главный метод атаки.
    // Его вызывает только PlayerInputHandler.
    public void TryAttack()
    {
        // Если кулдаун ещё не закончился — удар пока нельзя
        if (Time.time < nextAttackTime)
            return;

        // Если точка удара не назначена — предупреждаем в консоли и выходим
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен attackPoint!");
            return;
        }

        // Выполняем сам удар
        DoAttack();

        // Ставим время следующего удара
        nextAttackTime = Time.time + attackCooldown;
    }

    private void DoAttack()
    {
        // Ищем всех врагов в радиусе удара
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);

        // Если никого не нашли — просто выходим
        if (hits == null || hits.Length == 0)
            return;

        // Для кулачного боя нам не нужен удар по всем сразу.
        // Выбираем ТОЛЬКО ОДНУ ближайшую цель.
        EnemyHealth closestEnemy = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            // Ищем EnemyHealth у объекта врага или у его родителя
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            // Если враг уже мёртв — пропускаем
            if (enemyHealth.IsDead)
                continue;

            // Считаем расстояние от точки удара до врага
            float distanceSqr = (enemyHealth.transform.position - attackPoint.position).sqrMagnitude;

            // Если этот враг ближе, чем предыдущий найденный — запоминаем его
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestEnemy = enemyHealth;
            }
        }

        // Если нашли ближайшего врага — наносим ему урон
        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(baseDamage);
        }
    }

    // Эти методы нужны, чтобы потом другие скрипты могли:
    // 1. читать текущие параметры
    // 2. временно усиливать/ослаблять атаку
    // Это пригодится для способностей, статов и оружия.
    public int GetBaseDamage()
    {
        return baseDamage;
    }

    public float GetAttackRange()
    {
        return attackRange;
    }

    public float GetAttackCooldown()
    {
        return attackCooldown;
    }

    public void SetAttackCooldown(float newCooldown)
    {
        attackCooldown = newCooldown;
    }

    // Рисуем радиус удара в Scene, когда объект выделен
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}