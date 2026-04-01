using UnityEngine;

// PlayerAttack
// Этот скрипт отвечает за базовую ближнюю атаку игрока.
//
// Что он умеет на текущем этапе:
// 1. Если под курсором есть валидный враг — пытается ударить ИМЕННО ЕГО
// 2. Перед ударом доворачивается к этой цели
// 3. Сообщает системе показа, что начался manual focus
// 4. Если под курсором цели нет — делает старый обычный удар вперёд
// 5. Даёт отдельный публичный метод для soft lock,
//    чтобы тот мог атаковать конкретную цель
//
// ВАЖНО:
// - здесь нет soft lock логики
// - здесь нет выбора режима
// - здесь нет автоподхода
// - это только базовая атака
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
    // Через него сообщаем, что начался manual focus.

    [Header("Поворот к цели")]
    [SerializeField] private bool rotateToHoveredTarget = true;
    // Если true — при ручной атаке по цели под курсором
    // игрок сначала доворачивается к ней.

    [SerializeField] private bool rotateToSpecificTarget = true;
    // Если true — при soft lock атаке по конкретной цели
    // игрок тоже доворачивается к ней.

    private float nextAttackTime = 0f;
    private Rigidbody rb;

    private void Awake()
    {
        if (playerTargeting == null)
            playerTargeting = GetComponent<PlayerTargeting>();

        if (targetDisplayResolver == null)
            targetDisplayResolver = GetComponent<TargetDisplayResolver>();

        rb = GetComponent<Rigidbody>();
    }

    // Главный метод обычной ручной атаки.
    // Его вызывает PlayerInputHandler по ЛКМ.
    public void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен attackPoint!");
            return;
        }

        bool attackUsed = false;

        // Если под курсором есть валидная цель —
        // пробуем ударить именно её
        if (playerTargeting != null && playerTargeting.HasHoveredTarget())
        {
            EnemyHealth hoveredTarget = playerTargeting.HoveredTarget;
            attackUsed = TryAttackHoveredTarget(hoveredTarget);
        }
        else
        {
            // Если цели под курсором нет —
            // используем старый вариант удара вперёд
            attackUsed = DoDefaultAttack();
        }

        if (attackUsed)
            nextAttackTime = Time.time + attackCooldown;
    }

    // Публичный метод для атаки по КОНКРЕТНОЙ цели.
    // Его будет использовать мягкий режим.
    //
    // reportManualFocus:
    // true  = если хотим сообщить resolver'у о ручном фокусе
    // false = если это soft lock и manual focus здесь не нужен
    public void TryAttackSpecificTarget(EnemyHealth target, bool reportManualFocus)
    {
        if (Time.time < nextAttackTime)
            return;

        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен attackPoint!");
            return;
        }

        bool attackUsed = TryAttackSpecificTargetInternal(
            target,
            rotateToSpecificTarget,
            reportManualFocus);

        if (attackUsed)
            nextAttackTime = Time.time + attackCooldown;
    }

    // Удобный метод для других систем:
    // находится ли конкретная цель внутри текущего радиуса удара.
    public bool IsTargetInsideAttackRange(EnemyHealth target)
    {
        if (target == null)
            return false;

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

            if (enemyHealth == target)
                return true;
        }

        return false;
    }

    // =========================================================
    // РУЧНАЯ АТАКА ПО ЦЕЛИ ПОД КУРСОРОМ
    // =========================================================

    private bool TryAttackHoveredTarget(EnemyHealth target)
    {
        return TryAttackSpecificTargetInternal(
            target,
            rotateToHoveredTarget,
            true);
    }

    // =========================================================
    // ОБЩАЯ ЛОГИКА АТАКИ ПО КОНКРЕТНОЙ ЦЕЛИ
    // =========================================================

    private bool TryAttackSpecificTargetInternal(
        EnemyHealth target,
        bool shouldRotateToTarget,
        bool reportManualFocus)
    {
        if (target == null)
            return false;

        if (target.IsDead)
            return false;

        // Если это ручная атака —
        // сообщаем resolver'у, что начался manual focus
        if (reportManualFocus)
        {
            targetDisplayResolver?.ReportManualFocus(target);
        }

        // При необходимости доворачиваемся к цели
        if (shouldRotateToTarget)
        {
            RotateInstantlyToTarget(target.transform.position);
        }

        // Если конкретная цель реально в радиусе удара —
        // наносим урон именно ей
        if (IsTargetInsideAttackRange(target))
        {
            target.TakeDamage(baseDamage);
            return true;
        }

        // Если цель есть, но не достаётся —
        // считаем, что удар не использован
        //
        // Это важно для soft lock:
        // он не должен тратить кулдаун вхолостую,
        // пока цель не подошла в радиус.
        return false;
    }

    // =========================================================
    // СТАРЫЙ ОБЫЧНЫЙ УДАР ВПЕРЁД
    // =========================================================

    private bool DoDefaultAttack()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyMask);

        if (hits == null || hits.Length == 0)
            return false;

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
            return true;
        }

        return false;
    }

    // =========================================================
    // ПОВОРОТ К ЦЕЛИ
    // =========================================================

    private void RotateInstantlyToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (rb != null)
        {
            rb.rotation = targetRotation;
        }
        else
        {
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