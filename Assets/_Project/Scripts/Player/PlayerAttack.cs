using UnityEngine;

// PlayerAttack
// Этот скрипт отвечает за базовую ближнюю атаку игрока.
//
// Что он делает теперь:
// 1. Если под курсором есть валидный враг — пытается ударить ИМЕННО ЕГО
// 2. Перед ударом доворачивается к этой цели
// 3. Сообщает системе показа, что начался manual focus
// 4. Если под курсором цели нет — делает старый обычный удар вперёд
//
// ВАЖНО:
// - мы не делаем автоподход
// - мы не ищем новую цель сами
// - мы не включаем soft lock здесь
// - это именно обычная ручная ЛКМ-атака,
//   но уже более удобная и понятная
public class PlayerAttack : MonoBehaviour
{
    [Header("Точка удара")]
    [SerializeField] private Transform attackPoint;
    // Пустой объект перед игроком.
    // Из этой точки мы ищем врагов в радиусе удара.

    [Header("Слой врагов")]
    [SerializeField] private LayerMask enemyMask;
    // Здесь выбираем слой Enemy.

    [Header("Базовые параметры кулачного боя")]
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private float attackRange = 1.45f;
    [SerializeField] private float attackCooldown = 0.30f;

    [Header("Связи с системой таргета")]
    [SerializeField] private PlayerTargeting playerTargeting;
    // Сюда нужен PlayerTargeting с игрока.
    // Через него берём цель под курсором.

    [SerializeField] private TargetDisplayResolver targetDisplayResolver;
    // Сюда нужен TargetDisplayResolver с игрока.
    // Через него сообщаем, что ручная атака по цели началась.

    [Header("Поворот к цели")]
    [SerializeField] private bool rotateToHoveredTarget = true;
    // Если true — при ударе по цели под курсором
    // игрок сначала доворачивается к ней.

    // Время следующего разрешённого удара
    private float nextAttackTime = 0f;

    // Rigidbody игрока.
    // Используем, чтобы аккуратно менять поворот,
    // если он есть на объекте.
    private Rigidbody rb;

    private void Awake()
    {
        if (playerTargeting == null)
            playerTargeting = GetComponent<PlayerTargeting>();

        if (targetDisplayResolver == null)
            targetDisplayResolver = GetComponent<TargetDisplayResolver>();

        rb = GetComponent<Rigidbody>();
    }

    // Главный метод атаки.
    // Его вызывает PlayerInputHandler.
    public void TryAttack()
    {
        // Если кулдаун ещё не закончился — удар пока нельзя
        if (Time.time < nextAttackTime)
            return;

        // Если точка удара не назначена — предупреждаем и выходим
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен attackPoint!");
            return;
        }

        // Если под курсором есть валидная цель —
        // пытаемся ударить именно её
        if (playerTargeting != null && playerTargeting.HasHoveredTarget())
        {
            EnemyHealth hoveredTarget = playerTargeting.HoveredTarget;
            TryAttackHoveredTarget(hoveredTarget);
        }
        else
        {
            // Если цели под курсором нет —
            // работает старый обычный удар вперёд
            DoDefaultAttack();
        }

        // Ставим время следующего удара
        nextAttackTime = Time.time + attackCooldown;
    }

    // =========================================================
    // УДАР ПО ЦЕЛИ ПОД КУРСОРОМ
    // =========================================================

    private void TryAttackHoveredTarget(EnemyHealth target)
    {
        if (target == null)
        {
            DoDefaultAttack();
            return;
        }

        if (target.IsDead)
            return;

        // Сообщаем resolver'у:
        // игрок вручную действует по этой цели.
        //
        // Это важно даже без soft/hard режима.
        // Позже через это сможет открываться
        // расширенная информация о цели.
        targetDisplayResolver?.ReportManualFocus(target);

        // Доворачиваемся к цели, если включено
        if (rotateToHoveredTarget)
        {
            RotateInstantlyToTarget(target.transform.position);
        }

        // ВАЖНО:
        // если цель под курсором есть, но она не в досягаемости,
        // мы НЕ переключаемся на удар "по ближайшему кому попало".
        //
        // Логика такая:
        // - навёлся на конкретного врага
        // - значит пытался ударить именно его
        // - если он вне радиуса, удар не наносится
        if (IsSpecificTargetInsideAttackRange(target))
        {
            target.TakeDamage(baseDamage);
        }
    }

    // =========================================================
    // СТАРЫЙ ОБЫЧНЫЙ УДАР ВПЕРЁД
    // =========================================================

    private void DoDefaultAttack()
    {
        // Ищем всех врагов в радиусе удара
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);

        if (hits == null || hits.Length == 0)
            return;

        // Берём только одну ближайшую цель
        EnemyHealth closestEnemy = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            if (enemyHealth.IsDead)
                continue;

            float distanceSqr = (enemyHealth.transform.position - attackPoint.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestEnemy = enemyHealth;
            }
        }

        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(baseDamage);
        }
    }

    // =========================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // =========================================================

    private bool IsSpecificTargetInsideAttackRange(EnemyHealth target)
    {
        if (target == null)
            return false;

        // Ищем всех врагов внутри текущей сферы удара
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);

        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            if (enemyHealth.IsDead)
                continue;

            // Если внутри сферы находится ИМЕННО нужная цель —
            // значит её можно ударить
            if (enemyHealth == target)
                return true;
        }

        return false;
    }

    private void RotateInstantlyToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        // Если цель слишком близко или направление почти нулевое —
        // поворачивать не надо
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        // Если у игрока есть Rigidbody —
        // задаём поворот через него
        if (rb != null)
        {
            rb.rotation = targetRotation;
        }
        else
        {
            // Если Rigidbody нет —
            // просто через transform
            transform.rotation = targetRotation;
        }
    }

    // =========================================================
    // ГЕТТЕРЫ / СЕТТЕРЫ
    // =========================================================

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

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}