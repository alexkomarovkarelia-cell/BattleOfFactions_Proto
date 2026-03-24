using UnityEngine;
using UnityEngine.InputSystem;

// PlayerAbilityController
// Этот скрипт отвечает ТОЛЬКО за первую способность игрока.
//
// Способность:
// - временно ускоряет атаку
// - на время уменьшает кулдаун ударов
// - потом возвращает обычное значение
// - имеет собственный кулдаун
//
// Важно:
// - Сейчас для удобства есть ВРЕМЕННЫЙ ввод с клавиатуры (Q)
// - Позже ввод вынесем в отдельный InputHandler
// - Тогда InputHandler будет вызывать метод TryActivateAttackSpeedAbility()
public class PlayerAbilityController : MonoBehaviour
{
    [Header("Ссылка на атаку игрока")]
    [SerializeField] private PlayerAttack playerAttack;
    // Сюда нужно перетащить компонент PlayerAttack с игрока.
    // Через него мы будем менять скорость ударов.

    [Header("ВРЕМЕННЫЙ ввод (потом уйдёт в InputHandler)")]
    [SerializeField] private bool allowTemporaryKeyboardInput = true;
    [SerializeField] private Key abilityKey = Key.Q;
    // Пока способность активируется кнопкой Q.
    // Позже это уберём в отдельную систему ввода.

    [Header("Настройки способности")]
    [SerializeField] private float abilityDuration = 5f;
    // Сколько секунд действует ускорение атаки

    [SerializeField] private float abilityCooldown = 14f;
    // Кулдаун способности после активации

    [SerializeField] private float attackCooldownMultiplier = 0.55f;
    // Во сколько раз уменьшается кулдаун атаки.
    // Пример:
    // обычный кулдаун = 0.30
    // множитель = 0.55
    // во время способности получится 0.165

    [Header("Временный экранный фидбек")]
    [SerializeField] private bool showDebugUI = true;
    // Временно выводим состояние способности через OnGUI.
    // Потом можно заменить на нормальный HUD / TMP / иконку.

    // Сохраняем исходный кулдаун атаки,
    // чтобы после окончания способности вернуть его назад
    private float savedAttackCooldown;

    // До какого времени действует способность
    private float abilityEndTime = 0f;

    // До какого времени способность на кулдауне
    private float nextAbilityReadyTime = 0f;

    // Флаг, активна ли способность сейчас
    private bool isAbilityActive = false;

    private void Awake()
    {
        // Если ссылку забыли назначить в Inspector,
        // пробуем найти PlayerAttack на этом же объекте
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {
        // Временный ввод с клавиатуры
        if (allowTemporaryKeyboardInput)
        {
            HandleTemporaryKeyboardInput();
        }

        // Если способность активна и время закончилось — выключаем её
        if (isAbilityActive && Time.time >= abilityEndTime)
        {
            EndAttackSpeedAbility();
        }
    }

    private void HandleTemporaryKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[abilityKey].wasPressedThisFrame)
        {
            TryActivateAttackSpeedAbility();
        }
    }

    // Этот метод потом сможет вызывать:
    // - InputHandler
    // - UI-кнопка
    // - мобильная кнопка
    public void TryActivateAttackSpeedAbility()
    {
        // Если нет ссылки на PlayerAttack — выходим
        if (playerAttack == null)
        {
            Debug.LogWarning("PlayerAbilityController: не назначен PlayerAttack!");
            return;
        }

        // Если способность уже активна — второй раз не включаем
        if (isAbilityActive)
            return;

        // Если способность ещё на кулдауне — не активируем
        if (Time.time < nextAbilityReadyTime)
            return;

        StartAttackSpeedAbility();
    }

    private void StartAttackSpeedAbility()
    {
        isAbilityActive = true;

        // Сохраняем текущий обычный кулдаун,
        // чтобы потом вернуть его обратно
        savedAttackCooldown = playerAttack.GetAttackCooldown();

        // Считаем ускоренный кулдаун
        float boostedCooldown = savedAttackCooldown * attackCooldownMultiplier;

        // Назначаем его в PlayerAttack
        playerAttack.SetAttackCooldown(boostedCooldown);

        // Запоминаем время окончания эффекта
        abilityEndTime = Time.time + abilityDuration;

        // Сразу ставим время следующей готовности способности
        nextAbilityReadyTime = Time.time + abilityCooldown;
    }

    private void EndAttackSpeedAbility()
    {
        isAbilityActive = false;

        // Возвращаем обычный кулдаун атаки
        playerAttack.SetAttackCooldown(savedAttackCooldown);
    }

    // Этот метод пригодится потом для UI-кнопки.
    // Например, кнопку способности можно будет привязать именно к нему.
    public void ActivateAbilityFromUI()
    {
        TryActivateAttackSpeedAbility();
    }

    // Временный экранный фидбек
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
                text = $"Способность готова: нажми {abilityKey}";
        }

        // Чуть ниже, чем основной HUD
        GUI.Label(new Rect(20, 50, 500, 30), text, style);
    }

    // Эти методы пригодятся позже для UI/иконок/дебага
    public bool IsAbilityActive()
    {
        return isAbilityActive;
    }

    public float GetAbilityCooldownRemaining()
    {
        return Mathf.Max(nextAbilityReadyTime - Time.time, 0f);
    }

    public float GetAbilityActiveRemaining()
    {
        return Mathf.Max(abilityEndTime - Time.time, 0f);
    }
}