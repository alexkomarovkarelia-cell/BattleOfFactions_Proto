using UnityEngine;

// PlayerAttackSpeedAbility
// Этот скрипт отвечает ТОЛЬКО за первую активную способность игрока.
//
// Способность:
// - временно ускоряет атаку
// - на время уменьшает кулдаун ударов
// - потом возвращает обычное значение
// - имеет собственный кулдаун
//
// ВАЖНО:
// - Этот скрипт больше НЕ читает клавиатуру сам
// - Вызов способности идёт только через PlayerInputHandler -> TryActivate()
// - Позже этот же метод сможет вызывать UI-кнопка
public class PlayerAttackSpeedAbility : MonoBehaviour
{
    [Header("Ссылка на атаку игрока")]
    [SerializeField] private PlayerAttack playerAttack;
    // Сюда нужно перетащить компонент PlayerAttack с игрока.
    // Через него мы будем менять скорость ударов.

    [Header("Настройки способности")]
    [SerializeField] private float abilityDuration = 5f;
    // Сколько секунд действует ускорение атаки

    [SerializeField] private float abilityCooldown = 14f;
    // Кулдаун способности после активации

    [SerializeField] private float attackCooldownMultiplier = 0.55f;
    // Во сколько раз уменьшается кулдаун атаки

    [Header("Временный экранный фидбек")]
    [SerializeField] private bool showDebugUI = true;
    // Временно выводим состояние способности через OnGUI

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
        // Если способность активна и время закончилось — выключаем её
        if (isAbilityActive && Time.time >= abilityEndTime)
        {
            FinishAbility();
        }
    }

    // Главный метод активации.
    // Его вызывает PlayerInputHandler.
    public void TryActivate()
    {
        // Если нет ссылки на PlayerAttack — выходим
        if (playerAttack == null)
        {
            Debug.LogWarning("PlayerAttackSpeedAbility: не назначен PlayerAttack!");
            return;
        }

        // Если способность уже активна — второй раз не включаем
        if (isAbilityActive)
            return;

        // Если способность ещё на кулдауне — не активируем
        if (Time.time < nextAbilityReadyTime)
            return;

        StartAbility();
    }

    // Принудительное отключение способности.
    // Пока редко нужно, но это правильная база на будущее.
    public void Cancel()
    {
        if (!isAbilityActive)
            return;

        FinishAbility();
    }

    // Удобный метод для других систем и будущего UI
    public bool IsActive()
    {
        return isAbilityActive;
    }

    private void StartAbility()
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

    private void FinishAbility()
    {
        isAbilityActive = false;

        // Возвращаем обычный кулдаун атаки
        playerAttack.SetAttackCooldown(savedAttackCooldown);
    }

    // Этот метод пригодится потом для UI-кнопки
    public void ActivateAbilityFromUI()
    {
        TryActivate();
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
                text = "Способность готова";
        }

        GUI.Label(new Rect(20, 50, 500, 30), text, style);
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