using UnityEngine;
using UnityEngine.Serialization;

// PlayerAttack
// Этот скрипт отвечает за базовую ближнюю атаку игрока.
//
// Что меняем на этапе 7C:
// - игрок больше не завязан жёстко только на EnemyHealth как на "получателя урона";
// - обычная атака вперёд теперь ищет ОБЩИЙ ObjectHealth;
// - это значит, что позже сюда спокойно встанут:
//   - враги,
//   - боссы,
//   - разрушаемые объекты с прочностью.
//
// ВАЖНО:
// - ручной target / hover пока ещё завязан на EnemyHealth,
//   потому что текущая система таргетинга у тебя работает по врагам.
// - это нормально на данном этапе.
// - позже, если понадобится, отдельно расширим таргетинг и на другие типы целей.

public class PlayerAttack : MonoBehaviour
{
    [Header("Точка удара")]
    [SerializeField] private Transform attackPoint;

    [Header("Слой целей, которые можно ударить")]
    [FormerlySerializedAs("enemyMask")]
    [SerializeField] private LayerMask damageableMask;
    // Раньше здесь был enemyMask.
    // Теперь название честнее: сюда входят ВСЕ цели,
    // которые игрок может ударить.
    //
    // Пока у тебя там может стоять только слой Enemy.
    // Позже добавим, например, Breakable.

    [Header("Базовые параметры кулачного боя")]
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private float attackRange = 1.45f;
    [SerializeField] private float attackCooldown = 0.30f;

    [Header("Связи с системой таргета")]
    [SerializeField] private PlayerTargeting playerTargeting;
    [SerializeField] private TargetDisplayResolver targetDisplayResolver;

    [Header("Поворот к цели")]
    [SerializeField] private bool rotateToHoveredTarget = true;
    [SerializeField] private bool rotateToSpecificTarget = true;

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

        // Если под курсором есть валидный враг —
        // пока сохраняем старое поведение по EnemyHealth.
        if (playerTargeting != null && playerTargeting.HasHoveredTarget())
        {
            EnemyHealth hoveredTarget = playerTargeting.HoveredTarget;
            attackUsed = TryAttackHoveredTarget(hoveredTarget);
        }
        else
        {
            // Если цели под курсором нет —
            // обычная атака вперёд теперь ищет общий ObjectHealth.
            attackUsed = DoDefaultAttack();
        }

        if (attackUsed)
            nextAttackTime = Time.time + attackCooldown;
    }

    // Публичный метод для soft lock-атаки по конкретному врагу.
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

    // Проверка: находится ли конкретный враг внутри радиуса атаки.
    //
    // Здесь target пока остаётся EnemyHealth,
    // потому что текущий таргетинг ещё вражеский.
    public bool IsTargetInsideAttackRange(EnemyHealth target)
    {
        if (target == null)
            return false;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, damageableMask);

        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider hit in hits)
        {
            ObjectHealth health = hit.GetComponentInParent<ObjectHealth>();

            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            // target — это EnemyHealth, а EnemyHealth наследуется от ObjectHealth,
            // поэтому сравнение корректное.
            if (health == target)
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

        if (reportManualFocus)
        {
            targetDisplayResolver?.ReportManualFocus(target);
        }

        if (shouldRotateToTarget)
        {
            RotateInstantlyToTarget(target.transform.position);
        }

        // Здесь враг всё ещё типизирован как EnemyHealth,
        // но сам удар уже идёт по общей базе через TakeDamage().
        if (IsTargetInsideAttackRange(target))
        {
            target.TakeDamage(baseDamage);
            return true;
        }

        return false;
    }

    // =========================================================
    // ОБЫЧНЫЙ УДАР ВПЕРЁД
    // =========================================================

    private bool DoDefaultAttack()
    {
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, damageableMask);

        if (hits == null || hits.Length == 0)
            return false;

        ObjectHealth closestTarget = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            ObjectHealth health = hit.GetComponentInParent<ObjectHealth>();

            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            float distanceSqr = (health.transform.position - attackPoint.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = health;
            }
        }

        if (closestTarget != null)
        {
            closestTarget.TakeDamage(baseDamage);
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