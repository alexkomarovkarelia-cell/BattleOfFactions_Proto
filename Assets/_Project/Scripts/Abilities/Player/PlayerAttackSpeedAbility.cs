using UnityEngine;
using UnityEngine.InputSystem;

// PlayerAttackSpeedAbility
// Этот скрипт отвечает ТОЛЬКО за первую активную способность игрока.
//
// Что делает способность:
// - временно ускоряет базовую атаку
// - на время уменьшает кулдаун ударов
// - потом возвращает обычное значение
// - имеет свой собственный кулдаун
//
// ВАЖНО:
// Сейчас мы НЕ меняем механику способности.
// Мы только приводим названия методов к удобному виду
// и готовим скрипт к работе через PlayerInputHandler.
public class PlayerAttackSpeedAbility : MonoBehaviour
{
    [Header("Ссылка на атаку игрока")]
    [SerializeField] private PlayerAttack playerAttack;
    // Сюда нужно назначить компонент PlayerAttack с игрока.
    // Через него способность временно меняет кулдаун ударов.

    [Header("ВРЕМЕННЫЙ ввод (потом выключим совсем)")]
    [SerializeField] private bool allowTemporaryKeyboardInput = true;
    [SerializeField] private Key abilityKey = Key.Q;
    // Пока этот временный ввод оставляем в коде,
    // но после подключения через PlayerInputHandler
    // просто выключим его в Inspector.

    [Header("Настройки способности")]
    [SerializeField] private float abilityDuration = 5f;
    // Сколько секунд действует ускорение атаки

    [SerializeField] private float abilityCooldown = 14f;
    // Через сколько секунд способность снова станет доступна

    [SerializeField] private float attackCooldownMultiplier = 0.55f;
    // Во сколько раз уменьшается кулдаун обычной атаки.
    // Пример:
    // 0.30 * 0.55 = 0.165
    // Значит игрок будет бить быстрее, пока активна способность.

    [Header("Временный экранный фидбек")]
    [SerializeField] private bool showDebugUI = true;
    // Пока оставляем OnGUI для простой проверки.
    // Потом это можно будет заменить на нормальный UI.

    // Обычное значение кулдауна атаки до бафа
    private float savedAttackCooldown;

    // До какого времени способность активна
    private float abilityEndTime = 0f;

    // До какого времени способность на перезарядке
    private float nextAbilityReadyTime = 0f;

    // Активна ли способность прямо сейчас
    private bool isAbilityActive = false;

    private void Awake()
    {
        // Если ссылку не назначили вручную,
        // пробуем найти PlayerAttack на этом же объекте
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {
        // ВРЕМЕННЫЙ ввод.
        // После подключения через PlayerInputHandler
        // просто выключим allowTemporaryKeyboardInput в Inspector.
        if (allowTemporaryKeyboardInput)
        {
            HandleTemporaryKeyboardInput();
        }

        // Если способность активна и её время закончилось —
        // выключаем эффект
        if (isAbilityActive && Time.time >= abilityEndTime)
        {
            FinishAbility();
        }
    }

    private void HandleTemporaryKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[abilityKey].wasPressedThisFrame)
        {
            TryActivate();
        }
    }

    // Главный публичный метод активации способности.
    // Его теперь будет вызывать PlayerInputHandler.
    // Позже его сможет вызывать и UI-кнопка.
    public void TryActivate()
    {
        if (playerAttack == null)
        {
            Debug.LogWarning("PlayerAttackSpeedAbility: не назначен PlayerAttack!");
            return;
        }

        // Если способность уже активна — повторно не включаем
        if (isAbilityActive)
            return;

        // Если способность ещё на кулдауне — тоже не включаем
        if (Time.time < nextAbilityReadyTime)
            return;

        StartAbility();
    }

    // Явное досрочное отключение способности.
    // Пока редко нужно, но это хорошая база на будущее:
    // например для снятия бафа, смерти игрока, диспела и т.п.
    public void Cancel()
    {
        if (!isAbilityActive)
            return;

        FinishAbility();
    }

    // Удобный метод для внешней проверки:
    // активна ли способность сейчас.
    public bool IsActive()
    {
        return isAbilityActive;
    }

    private void StartAbility()
    {
        isAbilityActive = true;

        // Сохраняем обычный кулдаун атаки,
        // чтобы потом вернуть его назад
        savedAttackCooldown = playerAttack.GetAttackCooldown();

        // Вычисляем ускоренный кулдаун
        float boostedCooldown = savedAttackCooldown * attackCooldownMultiplier;

        // Временно назначаем ускоренное значение
        playerAttack.SetAttackCooldown(boostedCooldown);

        // Запоминаем время окончания способности
        abilityEndTime = Time.time + abilityDuration;

        // И сразу ставим кулдаун способности
        nextAbilityReadyTime = Time.time + abilityCooldown;
    }

    private void FinishAbility()
    {
        isAbilityActive = false;

        // Возвращаем обычный кулдаун атаки
        playerAttack.SetAttackCooldown(savedAttackCooldown);
    }

    // Этот метод оставляем для будущей UI-кнопки.
    public void ActivateAbilityFromUI()
    {
        TryActivate();
    }

    private void OnGUI()
    {
        if (!showDebugUI)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        string text;

        if (isAbilityActive)
        {
            float remain = Mathf.Max(abilityEndTime - Time.time, 0f);
            text = $"Способность активна: Ускорение атаки ({remain:F1} c)";
        }
        else
        {
            float cooldownRemain = Mathf.Max(nextAbilityReadyTime - Time.time, 0f);

            if (cooldownRemain > 0f)
                text = $"Способность: перезарядка {cooldownRemain:F1} c";
            else
                text = $"Способность готова";
        }

        GUI.Label(new Rect(20, 50, 500, 30), text, style);
    }

    // Полезно для будущего UI
    public float GetAbilityCooldownRemaining()
    {
        return Mathf.Max(nextAbilityReadyTime - Time.time, 0f);
    }

    public float GetAbilityActiveRemaining()
    {
        return Mathf.Max(abilityEndTime - Time.time, 0f);
    }
}