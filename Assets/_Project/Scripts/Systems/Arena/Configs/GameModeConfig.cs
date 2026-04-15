using UnityEngine;

// GameModeConfig
// Это ScriptableObject-конфиг режима арены.
//
// ВАЖНО:
// - обычный игрок не должен тонуть в куче параметров
// - режимы должны быть понятными:
//   Classic, Survival, позже Constructor, PvP
//
// Этот конфиг хранит правила режима,
// а директор режима потом будет работать уже в рамках этих правил.
[CreateAssetMenu(
    fileName = "GameModeConfig",
    menuName = "Project/Arena/Game Mode Config"
)]
public class GameModeConfig : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("Уникальный строковый ID режима.")]
    public string modeId = "classic";

    [Tooltip("Название режима, которое потом можно показать в UI.")]
    public string modeDisplayName = "Classic";

    [TextArea]
    [Tooltip("Короткое описание режима. Потом можно использовать в меню выбора.")]
    public string modeDescription = "";

    [Header("Основные правила режима")]
    [Tooltip("Если включено — режим бесконечный.")]
    public bool isEndless = false;

    [Tooltip("Максимальное число волн. Для бесконечного режима можно оставить 0.")]
    [Min(0)]
    public int maxWaves = 10;

    [Tooltip("Использовать ли рост бюджета от волны к волне.")]
    public bool useBudgetGrowth = true;

    [Tooltip("Разрешать ли смену арен в этом режиме.")]
    public bool allowArenaRotation = false;

    [Header("Разрешённые вмешательства директора")]
    [Tooltip("Может ли директор режима менять темп спавна.")]
    public bool allowSpawnInterventions = true;

    [Tooltip("Может ли директор режима включать окруженческие эффекты (туман, мороз, ветер и т.д.).")]
    public bool allowEnvironmentInterventions = true;

    [Tooltip("Может ли директор режима влиять на правила лута.")]
    public bool allowLootInterventions = true;

    [Tooltip("Может ли директор режима влиять на сервисы арены (торговец, ремонт и т.д.).")]
    public bool allowServiceInterventions = true;

    [Header("Правила бюджета")]
    [Tooltip("Разрешено ли в этом режиме использовать заранее подготовленный бюджетный план.")]
    public bool useExternalBudgetPlan = false;

    [Tooltip("Разрешено ли в этом режиме распределение бюджета игроком. Для будущего режима Конструктора.")]
    public bool allowPlayerBudgetDistribution = false;

    [Header("Темп режима")]
    [Tooltip("Дополнительная базовая задержка перед стартом первой волны.")]
    [Min(0f)]
    public float preRunDelay = 1f;

    [Tooltip("Минимальная задержка между волнами в этом режиме.")]
    [Min(0f)]
    public float minTimeBetweenWaves = 1f;

    private void OnValidate()
    {
        if (maxWaves < 0)
            maxWaves = 0;

        if (preRunDelay < 0f)
            preRunDelay = 0f;

        if (minTimeBetweenWaves < 0f)
            minTimeBetweenWaves = 0f;

        // Если режим бесконечный, то логично не ограничивать его maxWaves.
        // 0 здесь значит "без жёсткого лимита".
        if (isEndless && maxWaves != 0)
            maxWaves = 0;
    }
}