using UnityEngine;

// PlayerSoftLockAttack
// Этот скрипт отвечает за текущий режим атаки по выбранной цели.
//
// ВАЖНО:
// Сейчас мы НЕ делим режим на Soft / Hard как отдельные системы.
// Сейчас у нас один рабочий режим цели,
// а его поведение меняется галкой Enable Target Pursuit.
//
// false:
// - персонаж сам не идёт к цели
// - только держит её
// - игрок двигается вручную
//
// true:
// - персонаж сам подходит к цели
// - доходит до радиуса удара / применения
// - ручное движение на время режима блокируется
//
// На этом этапе мы также закладываем правильные сбросы режима:
// - ручная отмена
// - смерть цели
// - смерть игрока
// - выход за breakDistance
// - прыжок (через внешний вызов)
// - будущий запрет атаки / hard control (через отдельный внешний вызов)
[DisallowMultipleComponent]
public class PlayerSoftLockAttack : MonoBehaviour
{
    [Header("Ссылки на системы игрока")]
    [SerializeField] private PlayerTargeting playerTargeting;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private TargetDisplayResolver targetDisplayResolver;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Основные настройки режима")]
    [SerializeField] private bool enableTargetPursuit = false;
    // Главная галочка текущего этапа.
    //
    // false:
    // - персонаж сам НЕ идёт к цели
    // - только доворачивается
    // - игрок двигается вручную
    //
    // true:
    // - персонаж сам идёт к цели
    // - доходит до радиуса удара / применения
    // - ручное движение на время режима блокируется

    [SerializeField] private float breakDistance = 10f;
    // Если цель ушла дальше этого расстояния —
    // режим снимается полностью.

    [SerializeField] private float rotateSpeed = 720f;
    // Скорость поворота к цели.

    [Header("Настройки автоподхода")]
    [SerializeField] private float pursuitMoveSpeed = 5f;
    // Скорость автоподхода к цели.

    [SerializeField] private float pursuitStopDistance = 1.15f;
    // На какой дистанции рядом с целью мы останавливаемся,
    // если используем автоподход.
    //
    // Это НЕ радиус удара.
    // Это просто комфортная дистанция остановки рядом с целью.

    [Header("Будущие ограничения")]
    [SerializeField] private bool attackForbidden = false;
    // Временный задел на будущее.
    //
    // Сейчас это можно считать "ручным флагом запрета атаки".
    // Позже сюда смогут писать:
    // - hard control
    // - disarm
    // - pacify
    // - silence для нужных режимов
    // - другие эффекты, которые должны ломать режим
    //
    // Если этот флаг true — режим цели снимается.

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    // Текущая цель режима
    private EnemyHealth currentSoftLockTarget;

    // Активен ли режим сейчас
    private bool isSoftLockActive = false;

    private Rigidbody rb;

    public bool IsSoftLockActive => isSoftLockActive;
    public EnemyHealth CurrentSoftLockTarget => currentSoftLockTarget;

    // Это нужно PlayerMovement.
    // Если галка автоподхода включена и режим активен —
    // обычное движение игрока надо временно отключить.
    public bool BlocksManualMovement => isSoftLockActive && enableTargetPursuit;

    private void Awake()
    {
        if (playerTargeting == null)
            playerTargeting = GetComponent<PlayerTargeting>();

        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();

        if (targetDisplayResolver == null)
            targetDisplayResolver = GetComponent<TargetDisplayResolver>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!isSoftLockActive)
            return;

        // 1. Если цель пропала — снимаем режим
        if (!IsTargetValid(currentSoftLockTarget))
        {
            CancelSoftLock();
            return;
        }

        // 2. Если игрок мёртв — снимаем режим
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            CancelSoftLock();
            return;
        }

        // 3. Если цель слишком далеко — снимаем режим
        if (IsTargetTooFar(currentSoftLockTarget))
        {
            CancelSoftLock();
            return;
        }

        // 4. Если атака сейчас запрещена — снимаем режим
        if (attackForbidden)
        {
            CancelSoftLock();
            return;
        }

        // 5. Автоатака:
        // если цель в радиусе удара —
        // бьём её автоматически
        if (playerAttack != null && playerAttack.IsTargetInsideAttackRange(currentSoftLockTarget))
        {
            playerAttack.TryAttackSpecificTarget(currentSoftLockTarget, false);
        }
    }

    private void FixedUpdate()
    {
        if (!isSoftLockActive)
            return;

        if (!IsTargetValid(currentSoftLockTarget))
            return;

        // Всегда поворачиваемся к цели
        RotateToCurrentTarget();

        // Только если включена галка преследования —
        // сами двигаемся к цели
        if (enableTargetPursuit)
        {
            MoveTowardsCurrentTarget();
        }
    }

    // =========================================================
    // ПУБЛИЧНАЯ ЛОГИКА
    // =========================================================

    // Ctrl + ЛКМ:
    // - если под курсором есть цель,
    //   включаем режим на ней
    // - если уже активен режим и под курсором другая цель,
    //   переключаемся на неё
    public bool TryActivateOrSwitchSoftLock()
    {
        if (playerTargeting == null || !playerTargeting.HasHoveredTarget())
            return false;

        EnemyHealth hoveredTarget = playerTargeting.HoveredTarget;

        if (!IsTargetValid(hoveredTarget))
            return false;

        ActivateOrSwitchSoftLock(hoveredTarget);
        return true;
    }

    // Ctrl без ЛКМ:
    // снимаем текущий режим
    public void CancelSoftLock()
    {
        if (!isSoftLockActive)
            return;

        if (showDebugLogs && currentSoftLockTarget != null)
            Debug.Log($"Target mode cancelled: {currentSoftLockTarget.name}");

        isSoftLockActive = false;

        // Сообщаем resolver'у, что режим выключен
        targetDisplayResolver?.SetSoftLockState(false);

        // Если выбранная цель совпадает с текущей —
        // очищаем её
        if (playerTargeting != null &&
            playerTargeting.HasSelectedTarget() &&
            playerTargeting.SelectedTarget == currentSoftLockTarget)
        {
            playerTargeting.ClearSelectedTarget();
        }

        StopPursuitMovement();
        currentSoftLockTarget = null;
    }

    // Этот метод специально добавляем как явную точку входа
    // для других систем.
    //
    // Сейчас его будет вызывать прыжок:
    // Space -> снять режим -> прыгнуть
    //
    // Позже сюда смогут обращаться:
    // - hard control
    // - запрет атаки
    // - UI-переход в другой режим
    // - торговля / контракты / взаимодействия
    public void CancelSoftLockByExternalReason()
    {
        CancelSoftLock();
    }

    // Временный публичный метод на будущее.
    // Позже какой-то другой скрипт сможет сказать:
    // "теперь базовая атака запрещена" или "разрешена снова".
    public void SetAttackForbidden(bool forbidden)
    {
        attackForbidden = forbidden;

        if (attackForbidden && isSoftLockActive)
        {
            CancelSoftLock();
        }
    }

    // =========================================================
    // ВНУТРЕННЯЯ ЛОГИКА
    // =========================================================

    private void ActivateOrSwitchSoftLock(EnemyHealth target)
    {
        if (!IsTargetValid(target))
            return;

        bool switchedToAnotherTarget = currentSoftLockTarget != target;

        isSoftLockActive = true;
        currentSoftLockTarget = target;

        // Эта цель становится выбранной
        playerTargeting?.SelectTarget(target);

        // Сообщаем resolver'у, что целевой режим активен на этой цели
        targetDisplayResolver?.SetSoftLockState(true, target);

        if (showDebugLogs)
        {
            if (switchedToAnotherTarget)
                Debug.Log($"Target mode set: {target.name}, pursuit = {enableTargetPursuit}");
            else
                Debug.Log($"Target mode refreshed: {target.name}, pursuit = {enableTargetPursuit}");
        }
    }

    private void RotateToCurrentTarget()
    {
        if (currentSoftLockTarget == null)
            return;

        Vector3 direction = currentSoftLockTarget.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (rb != null)
        {
            Quaternion smoothRotation = Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(smoothRotation);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void MoveTowardsCurrentTarget()
    {
        if (currentSoftLockTarget == null)
            return;

        if (rb == null)
            return;

        // Если цель уже в радиусе удара —
        // дальше не двигаемся
        if (playerAttack != null && playerAttack.IsTargetInsideAttackRange(currentSoftLockTarget))
        {
            StopPursuitMovement();
            return;
        }

        Vector3 from = transform.position;
        Vector3 to = currentSoftLockTarget.transform.position;

        from.y = 0f;
        to.y = 0f;

        Vector3 direction = to - from;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            StopPursuitMovement();
            return;
        }

        float distance = direction.magnitude;

        // Если уже подошли достаточно близко —
        // останавливаемся
        if (distance <= pursuitStopDistance)
        {
            StopPursuitMovement();
            return;
        }

        Vector3 moveDir = direction.normalized;

        // Двигаем только по XZ.
        // Y сохраняем, чтобы не ломать физику/падение.
        rb.linearVelocity = new Vector3(
            moveDir.x * pursuitMoveSpeed,
            rb.linearVelocity.y,
            moveDir.z * pursuitMoveSpeed
        );
    }

    private void StopPursuitMovement()
    {
        if (rb == null)
            return;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private bool IsTargetTooFar(EnemyHealth target)
    {
        if (target == null)
            return true;

        Vector3 from = transform.position;
        Vector3 to = target.transform.position;

        from.y = 0f;
        to.y = 0f;

        float distance = Vector3.Distance(from, to);
        return distance > breakDistance;
    }

    private bool IsTargetValid(EnemyHealth target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (target.IsDead)
            return false;

        return true;
    }
}