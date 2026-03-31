using UnityEngine;

// TargetDisplayLevel
// Это уровень информации, который мы сейчас готовы показать о цели.
//
// None      = ничего не показываем
// NameOnly  = только имя / название
// Extended  = расширенная информация
//
// На Этапе 5 реально используем только:
// - NameOnly
// - и задел под Extended
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
// Разделение ролей такое:
// PlayerTargeting      -> кого навели / выбрали
// TargetInfoProvider   -> какие данные у цели вообще есть
// TargetDisplayResolver-> что из этих данных можно показать СЕЙЧАС
// PlayerTargetUI       -> как это нарисовать на экране
//
// Почему это важно:
// сюда потом очень удобно добавлять:
// - скрытность
// - невидимость
// - маскировку
// - наблюдательность
// - мудрость
// - особые правила для боссов
// - разные уровни раскрытия информации
//
// На Этапе 5 делаем такой фундамент:
// 1. Просто hover мышкой -> показываем только имя
// 2. Если включён soft lock / hard lock
//    и цель удерживается в боевом фокусе какое-то время,
//    тогда можно разрешить расширенную информацию
//
// Сама расширенная информация пока ещё не рисуется,
// но правило уже закладываем правильно.
[DisallowMultipleComponent]
public class TargetDisplayResolver : MonoBehaviour
{
    [Header("Ссылка на таргет игрока")]
    [SerializeField] private PlayerTargeting playerTargeting;
    // Сюда нужен PlayerTargeting с объекта Player

    [Header("Приоритет целей")]
    [SerializeField] private bool preferSelectedTarget = true;
    // Если true:
    // выбранная цель важнее цели под курсором

    [SerializeField] private bool allowHoveredTargetDisplay = true;
    // Можно ли показывать данные цели под курсором

    [SerializeField] private bool allowSelectedTargetDisplay = true;
    // Можно ли показывать данные выбранной цели

    [Header("Правила раскрытия информации")]
    [SerializeField] private bool showNameOnHover = true;
    // При простом наведении мышкой показываем только имя

    [SerializeField] private bool showExtendedInfoInSoftLock = true;
    // Если включён мягкий режим,
    // можно ли потом раскрывать расширенную информацию

    [SerializeField] private bool showExtendedInfoInHardLock = true;
    // Если включён жёсткий режим,
    // можно ли потом раскрывать расширенную информацию

    [SerializeField] private float revealDelay = 0.5f;
    // Через сколько секунд боевого фокуса
    // можно показать расширенную информацию
    //
    // Идея:
    // сразу при наведении не вываливаем всё подряд.
    // Сначала только имя.
    // А уже при удержании боевого фокуса
    // через небольшую паузу можно показать больше.

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    // Состояния боевого фокуса.
    // Пока на Этапе 5 они будут просто заложены.
    // Позже PlayerSoftLockAttack / hard lock смогут их вызывать.
    private bool softLockActive = false;
    private bool hardLockActive = false;

    // Цель, которая сейчас находится в боевом фокусе.
    // Это не просто hover мышкой,
    // а именно цель, на которой держится боевой режим.
    private EnemyHealth focusTarget;

    // Время, когда этот боевой фокус начался.
    private float focusStartTime = -999f;

    private void Awake()
    {
        if (playerTargeting == null)
            playerTargeting = GetComponent<PlayerTargeting>();

        if (playerTargeting == null)
            playerTargeting = FindFirstObjectByType<PlayerTargeting>();
    }

    private void Update()
    {
        // Если цель боевого фокуса стала невалидной —
        // полностью сбрасываем фокус.
        if ((softLockActive || hardLockActive) && !IsTargetStillValid(focusTarget))
        {
            if (showDebugLogs && focusTarget != null)
                Debug.Log($"Combat focus lost: {focusTarget.name}");

            ClearAllFocusState();
        }
    }

    // =========================================================
    // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ БОЕВЫХ РЕЖИМОВ
    // =========================================================

    // Этот метод позже будет вызывать мягкий режим.
    //
    // Пример будущей логики:
    // targetDisplayResolver.SetSoftLockState(true, currentTarget);
    public void SetSoftLockState(bool isActive, EnemyHealth target = null)
    {
        softLockActive = isActive;

        if (isActive)
        {
            // Если передали цель —
            // начинаем/обновляем боевой фокус именно на ней
            if (target != null)
                SetCombatFocusTarget(target);
        }
        else
        {
            // Если мягкий режим выключили,
            // и жёсткий тоже не активен —
            // полностью убираем боевой фокус
            if (!hardLockActive)
                ClearAllFocusState();
        }

        if (showDebugLogs)
            Debug.Log($"SoftLock active = {softLockActive}");
    }

    // Этот метод позже будет вызывать жёсткий режим.
    public void SetHardLockState(bool isActive, EnemyHealth target = null)
    {
        hardLockActive = isActive;

        if (isActive)
        {
            if (target != null)
                SetCombatFocusTarget(target);
        }
        else
        {
            if (!softLockActive)
                ClearAllFocusState();
        }

        if (showDebugLogs)
            Debug.Log($"HardLock active = {hardLockActive}");
    }

    // Установить/обновить цель боевого фокуса.
    // Важно:
    // если цель поменялась,
    // таймер revealDelay должен начаться заново.
    public void SetCombatFocusTarget(EnemyHealth target)
    {
        if (!IsTargetStillValid(target))
            return;

        // Если цель та же самая —
        // не сбрасываем таймер повторно
        if (focusTarget == target)
            return;

        focusTarget = target;
        focusStartTime = Time.time;

        if (showDebugLogs)
            Debug.Log($"Combat focus target = {focusTarget.name}");
    }

    // Полностью очистить боевой фокус.
    public void ClearAllFocusState()
    {
        softLockActive = false;
        hardLockActive = false;
        focusTarget = null;
        focusStartTime = -999f;

        if (showDebugLogs)
            Debug.Log("All combat focus state cleared");
    }

    // =========================================================
    // ГЛАВНЫЙ МЕТОД ДЛЯ UI
    // =========================================================

    // UI вызывает этот метод и получает уже готовое решение:
    // - что можно показать
    // - какое имя показать
    // - можно ли показать HP
    // - какие числа HP вернуть
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

        // Если нет таргета игрока —
        // ничего не показываем
        if (playerTargeting == null)
            return false;

        // Определяем, какая цель сейчас приоритетна для показа
        EnemyHealth targetToDisplay = GetTargetToDisplay();

        if (targetToDisplay == null)
            return false;

        // Определяем уровень информации, который сейчас разрешён
        TargetDisplayLevel displayLevel = ResolveDisplayLevel(targetToDisplay);

        if (displayLevel == TargetDisplayLevel.None)
            return false;

        // Берём провайдер данных с самой цели
        TargetInfoProvider infoProvider = targetToDisplay.GetComponentInParent<TargetInfoProvider>();

        // Если провайдера нет —
        // пробуем не ломаться и хотя бы показать имя объекта
        if (infoProvider == null)
        {
            displayName = CleanObjectName(targetToDisplay.gameObject.name);

            // Без провайдера HP показать не сможем
            showHealth = false;
            currentHealth = 0;
            maxHealth = 0;

            return !string.IsNullOrWhiteSpace(displayName);
        }

        // Имя можно показать уже сейчас
        displayName = infoProvider.GetDisplayName();

        // Расширенная информация
        // Пока на Этапе 5 это только задел.
        // Но если позже UI начнёт рисовать HP,
        // resolver уже будет готов дать эти данные.
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
    // ВНУТРЕННЯЯ ЛОГИКА RESOLVER
    // =========================================================

    private EnemyHealth GetTargetToDisplay()
    {
        if (playerTargeting == null)
            return null;

        // Вариант 1:
        // если выбрана цель и ей можно отдавать приоритет —
        // показываем её
        if (preferSelectedTarget &&
            allowSelectedTargetDisplay &&
            playerTargeting.HasSelectedTarget())
        {
            return playerTargeting.SelectedTarget;
        }

        // Вариант 2:
        // если есть цель под курсором —
        // показываем её
        if (allowHoveredTargetDisplay &&
            playerTargeting.HasHoveredTarget())
        {
            return playerTargeting.HoveredTarget;
        }

        // Вариант 3:
        // если selected не был приоритетным,
        // но hovered нет,
        // тогда можно показать selected
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

        // По умолчанию при простом наведении —
        // только имя
        TargetDisplayLevel fallbackLevel = showNameOnHover
            ? TargetDisplayLevel.NameOnly
            : TargetDisplayLevel.None;

        // Если нет боевого фокуса —
        // остаёмся на fallback уровне
        if (!IsCombatFocusOnTarget(target))
            return fallbackLevel;

        // Если боевой фокус есть,
        // но нужная задержка ещё не прошла —
        // тоже пока только fallback
        if (!HasRevealDelayPassed())
            return fallbackLevel;

        // Если сейчас активен hard lock
        // и для него разрешено расширенное отображение
        if (hardLockActive && showExtendedInfoInHardLock)
            return TargetDisplayLevel.Extended;

        // Если сейчас активен soft lock
        // и для него разрешено расширенное отображение
        if (softLockActive && showExtendedInfoInSoftLock)
            return TargetDisplayLevel.Extended;

        return fallbackLevel;
    }

    private bool IsCombatFocusOnTarget(EnemyHealth target)
    {
        if (target == null)
            return false;

        if (focusTarget == null)
            return false;

        return focusTarget == target;
    }

    private bool HasRevealDelayPassed()
    {
        return Time.time >= focusStartTime + revealDelay;
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