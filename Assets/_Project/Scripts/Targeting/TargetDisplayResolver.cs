using UnityEngine;

// TargetDisplayLevel
// Это уровень информации, который мы готовы показать о цели.
//
// None      = ничего не показываем
// NameOnly  = только имя / название
// Extended  = расширенная информация
//
// На Этапе 5 реально используем:
// - NameOnly
// - фундамент под Extended
public enum TargetDisplayLevel
{
    None = 0,
    NameOnly = 1,
    Extended = 2
}

// TargetDisplayResolver
// Этот скрипт живёт НА ИГРОКЕ.
//
// Его задача:
// решить, ЧТО ИМЕННО МОЖНО ПОКАЗАТЬ о текущей цели.
//
// ВАЖНО:
// - он НЕ выбирает цель
// - он НЕ рисует UI
// - он НЕ хранит "сырые" данные цели
//
// Разделение ролей:
// PlayerTargeting      -> кого навели / выбрали
// TargetInfoProvider   -> какие данные у цели вообще есть
// TargetDisplayResolver-> что из этих данных можно показать СЕЙЧАС
// PlayerTargetUI       -> как это нарисовать
//
// На этом этапе важно:
// расширенная информация должна открываться НЕ ТОЛЬКО от soft/hard режима,
// но и от обычного ручного действия ЛКМ по цели.
//
// Поэтому у нас будет несколько источников фокуса:
// - Hover            -> просто навели курсор
// - ManualFocus      -> обычный ЛКМ по цели
// - SoftLockFocus    -> мягкий режим
// - HardLockFocus    -> жёсткий режим
[DisallowMultipleComponent]
public class TargetDisplayResolver : MonoBehaviour
{
    [Header("Ссылка на таргет игрока")]
    [SerializeField] private PlayerTargeting playerTargeting;

    [Header("Приоритет целей")]
    [SerializeField] private bool preferSelectedTarget = true;
    [SerializeField] private bool allowHoveredTargetDisplay = true;
    [SerializeField] private bool allowSelectedTargetDisplay = true;

    [Header("Правила показа")]
    [SerializeField] private bool showNameOnHover = true;
    [SerializeField] private bool showExtendedInfoInManualFocus = true;
    [SerializeField] private bool showExtendedInfoInSoftLock = true;
    [SerializeField] private bool showExtendedInfoInHardLock = true;

    [SerializeField] private float revealDelay = 0.5f;
    // Через сколько секунд фокуса разрешаем расширенную информацию.
    // То есть:
    // - сразу при наведении только имя
    // - если цель удерживается в фокусе, можно открыть больше

    [SerializeField] private float manualFocusHoldDuration = 1.25f;
    // Сколько секунд после обычного ручного действия
    // сохраняется manual focus.
    //
    // Это важно, потому что ЛКМ-удар — событие короткое.
    // Нам нужно, чтобы фокус не пропадал мгновенно.
    //
    // Если игрок бьёт ту же цель ещё раз —
    // мы просто продлеваем это окно.

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    // ================================
    // SOFT / HARD LOCK СОСТОЯНИЯ
    // ================================

    private bool softLockActive = false;
    private bool hardLockActive = false;

    private EnemyHealth lockFocusTarget;
    private float lockFocusStartTime = -999f;

    // ================================
    // MANUAL FOCUS СОСТОЯНИЯ
    // ================================

    private bool manualFocusActive = false;
    private EnemyHealth manualFocusTarget;
    private float manualFocusStartTime = -999f;
    private float manualFocusExpireTime = -999f;

    private void Awake()
    {
        if (playerTargeting == null)
            playerTargeting = GetComponent<PlayerTargeting>();

        if (playerTargeting == null)
            playerTargeting = FindFirstObjectByType<PlayerTargeting>();
    }

    private void Update()
    {
        UpdateManualFocus();
        UpdateLockFocus();
    }

    // =========================================================
    // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ MANUAL / SOFT / HARD FOCUS
    // =========================================================

    // Этот метод вызываем, когда игрок вручную делает действие по цели.
    // На Этапе 5 — это обычный ЛКМ-удар по врагу под курсором.
    //
    // Позже этот же метод смогут использовать:
    // - выстрел
    // - баф на цель
    // - дебаф на цель
    // - другие направленные действия
    public void ReportManualFocus(EnemyHealth target)
    {
        if (!IsTargetStillValid(target))
            return;

        // Если manual focus уже активен на этой же цели,
        // НЕ сбрасываем старт revealDelay заново.
        // Мы только продлеваем время удержания.
        if (manualFocusActive && manualFocusTarget == target)
        {
            manualFocusExpireTime = Time.time + manualFocusHoldDuration;

            if (showDebugLogs)
                Debug.Log($"Manual focus extended: {target.name}");

            return;
        }

        // Если цели раньше не было или цель сменилась —
        // начинаем новый manual focus.
        manualFocusActive = true;
        manualFocusTarget = target;
        manualFocusStartTime = Time.time;
        manualFocusExpireTime = Time.time + manualFocusHoldDuration;

        if (showDebugLogs)
            Debug.Log($"Manual focus started: {target.name}");
    }

    public void ClearManualFocus()
    {
        if (showDebugLogs && manualFocusTarget != null)
            Debug.Log($"Manual focus cleared: {manualFocusTarget.name}");

        manualFocusActive = false;
        manualFocusTarget = null;
        manualFocusStartTime = -999f;
        manualFocusExpireTime = -999f;
    }

    // Этот метод позже будет вызывать мягкий режим
    public void SetSoftLockState(bool isActive, EnemyHealth target = null)
    {
        softLockActive = isActive;

        if (isActive)
        {
            if (target != null)
                SetLockFocusTarget(target);
        }
        else
        {
            if (!hardLockActive)
                ClearLockFocus();
        }

        if (showDebugLogs)
            Debug.Log($"SoftLock active = {softLockActive}");
    }

    // Этот метод позже будет вызывать жёсткий режим
    public void SetHardLockState(bool isActive, EnemyHealth target = null)
    {
        hardLockActive = isActive;

        if (isActive)
        {
            if (target != null)
                SetLockFocusTarget(target);
        }
        else
        {
            if (!softLockActive)
                ClearLockFocus();
        }

        if (showDebugLogs)
            Debug.Log($"HardLock active = {hardLockActive}");
    }

    public void ClearAllFocusState()
    {
        ClearManualFocus();
        ClearLockFocus();
    }

    // =========================================================
    // ГЛАВНЫЙ МЕТОД ДЛЯ UI
    // =========================================================

    public bool TryGetDisplayData(
        out string displayName,
        out bool showHealth,
        out int currentHealth,
        out int maxHealth)
    {
        displayName = "";
        showHealth = false;
        currentHealth = 0;
        maxHealth = 0;

        if (playerTargeting == null)
            return false;

        EnemyHealth targetToDisplay = GetTargetToDisplay();

        if (targetToDisplay == null)
            return false;

        TargetDisplayLevel displayLevel = ResolveDisplayLevel(targetToDisplay);

        if (displayLevel == TargetDisplayLevel.None)
            return false;

        TargetInfoProvider infoProvider = targetToDisplay.GetComponentInParent<TargetInfoProvider>();

        if (infoProvider == null)
        {
            displayName = CleanObjectName(targetToDisplay.gameObject.name);
            showHealth = false;
            currentHealth = 0;
            maxHealth = 0;

            return !string.IsNullOrWhiteSpace(displayName);
        }

        displayName = infoProvider.GetDisplayName();

        if (displayLevel == TargetDisplayLevel.Extended && infoProvider.HasHealthData)
        {
            showHealth = true;
            currentHealth = infoProvider.GetCurrentHealth();
            maxHealth = infoProvider.GetMaxHealth();
        }
        else
        {
            showHealth = false;
            currentHealth = 0;
            maxHealth = 0;
        }

        return !string.IsNullOrWhiteSpace(displayName);
    }

    // =========================================================
    // ВНУТРЕННЯЯ ЛОГИКА
    // =========================================================

    private void UpdateManualFocus()
    {
        if (!manualFocusActive)
            return;

        // Если цель manual focus умерла/пропала — очищаем
        if (!IsTargetStillValid(manualFocusTarget))
        {
            ClearManualFocus();
            return;
        }

        // Если время вышло — очищаем
        if (Time.time > manualFocusExpireTime)
        {
            ClearManualFocus();
        }
    }

    private void UpdateLockFocus()
    {
        if (!softLockActive && !hardLockActive)
            return;

        if (!IsTargetStillValid(lockFocusTarget))
        {
            ClearLockFocus();
        }
    }

    private void SetLockFocusTarget(EnemyHealth target)
    {
        if (!IsTargetStillValid(target))
            return;

        // Если цель та же самая — не перезапускаем revealDelay
        if (lockFocusTarget == target)
            return;

        lockFocusTarget = target;
        lockFocusStartTime = Time.time;

        if (showDebugLogs)
            Debug.Log($"Lock focus target = {target.name}");
    }

    private void ClearLockFocus()
    {
        if (showDebugLogs && lockFocusTarget != null)
            Debug.Log($"Lock focus cleared: {lockFocusTarget.name}");

        softLockActive = false;
        hardLockActive = false;
        lockFocusTarget = null;
        lockFocusStartTime = -999f;
    }

    private EnemyHealth GetTargetToDisplay()
    {
        if (playerTargeting == null)
            return null;

        if (preferSelectedTarget &&
            allowSelectedTargetDisplay &&
            playerTargeting.HasSelectedTarget())
        {
            return playerTargeting.SelectedTarget;
        }

        if (allowHoveredTargetDisplay &&
            playerTargeting.HasHoveredTarget())
        {
            return playerTargeting.HoveredTarget;
        }

        if (!preferSelectedTarget &&
            allowSelectedTargetDisplay &&
            playerTargeting.HasSelectedTarget())
        {
            return playerTargeting.SelectedTarget;
        }

        return null;
    }

    private TargetDisplayLevel ResolveDisplayLevel(EnemyHealth target)
    {
        if (!IsTargetStillValid(target))
            return TargetDisplayLevel.None;

        // Базовый уровень:
        // просто hover мышкой = только имя
        TargetDisplayLevel fallbackLevel = showNameOnHover
            ? TargetDisplayLevel.NameOnly
            : TargetDisplayLevel.None;

        // 1. Проверяем hard lock
        if (hardLockActive &&
            showExtendedInfoInHardLock &&
            lockFocusTarget == target &&
            HasLockRevealDelayPassed())
        {
            return TargetDisplayLevel.Extended;
        }

        // 2. Проверяем soft lock
        if (softLockActive &&
            showExtendedInfoInSoftLock &&
            lockFocusTarget == target &&
            HasLockRevealDelayPassed())
        {
            return TargetDisplayLevel.Extended;
        }

        // 3. Проверяем manual focus
        if (manualFocusActive &&
            showExtendedInfoInManualFocus &&
            manualFocusTarget == target &&
            HasManualRevealDelayPassed())
        {
            return TargetDisplayLevel.Extended;
        }

        return fallbackLevel;
    }

    private bool HasLockRevealDelayPassed()
    {
        return Time.time >= lockFocusStartTime + revealDelay;
    }

    private bool HasManualRevealDelayPassed()
    {
        return Time.time >= manualFocusStartTime + revealDelay;
    }

    private bool IsTargetStillValid(EnemyHealth target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        if (target.IsDead)
            return false;

        return true;
    }

    private string CleanObjectName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "";

        return rawName.Replace("(Clone)", "").Trim();
    }
}