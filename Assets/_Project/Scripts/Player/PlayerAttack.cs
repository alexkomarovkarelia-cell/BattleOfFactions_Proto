using UnityEngine;
using UnityEngine.InputSystem;

// PlayerAttack
// Этот скрипт отвечает ТОЛЬКО за логику базовой ближней атаки игрока.
//
// Что он делает:
// 1. Хранит параметры кулачного удара
// 2. Ищет врага в радиусе удара
// 3. Выбирает ОДНУ ближайшую цель
// 4. Наносит ей урон
//
// Важно:
// - Сейчас для удобства оставлен ВРЕМЕННЫЙ ввод через ЛКМ
// - Позже ввод вынесем в отдельный InputHandler
// - Тогда InputHandler будет вызывать метод TryAttack()
public class PlayerAttack : MonoBehaviour
{
    [Header("ВРЕМЕННЫЙ ввод (потом уберём в отдельный InputHandler)")]
    [SerializeField] private bool allowTemporaryMouseInput = true;
    // Пока true -> игрок может атаковать ЛКМ.
    // Позже, когда сделаем отдельный скрипт ввода, поставим false,
    // и атака будет вызываться не отсюда, а из InputHandler.

    [Header("Точка удара")]
    [SerializeField] private Transform attackPoint;
    // Это пустой объект перед игроком.
    // Из этой точки мы ищем врагов в радиусе.
    // Его нужно создать вручную как дочерний объект игрока.

    [Header("Слой врагов")]
    [SerializeField] private LayerMask enemyMask;
    // Здесь выбираем слой Enemy.
    // Скрипт будет искать только объекты на этом слое.

    [Header("Базовые параметры кулачного боя")]
    [SerializeField] private int baseDamage = 15;
    // Базовый урон кулаком

    [SerializeField] private float attackRange = 1.45f;
    // Радиус удара.
    // Если будет тяжело попадать — увеличим чуть позже.

    [SerializeField] private float attackCooldown = 0.30f;
    // Задержка между ударами.
    // Чем меньше число — тем быстрее игрок бьёт.

    // Время, когда игрок сможет ударить снова
    private float nextAttackTime = 0f;

    private void Update()
    {
        // ВРЕМЕННО:
        // Пока у нас нет отдельного InputHandler, разрешаем атаку через ЛКМ.
        // Позже этот блок можно будет отключить, поставив allowTemporaryMouseInput = false.
        if (allowTemporaryMouseInput)
        {
            HandleTemporaryMouseInput();
        }
    }

    private void HandleTemporaryMouseInput()
    {
        // Если мыши нет — просто выходим
        if (Mouse.current == null)
            return;

        // Если ЛКМ нажали в этот кадр — пробуем ударить
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    // Этот метод — главный для атаки.
    // Именно его потом должен вызывать отдельный InputHandler.
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