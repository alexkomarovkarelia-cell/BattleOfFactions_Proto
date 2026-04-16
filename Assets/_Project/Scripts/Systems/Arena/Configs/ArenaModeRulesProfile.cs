using UnityEngine;

// ArenaModeRulesProfile
// Это отдельный профиль ПРАВИЛ режима.
//
// ВАЖНО:
// Это НЕ сам режим.
// Это НЕ сложность.
// Это именно правила того, КАК режим должен стартовать и вести себя.
//
// Пример:
// - классика может стартовать мягко
// - выживание может иметь более длинный разогрев
// - рейтинговый режим позже сможет отключить часть послаблений
// - "соревнование месяца" сможет жить по своим особым правилам
//
// То есть:
// GameModeConfig       = какой это режим вообще
// ArenaDifficultyProfile = насколько он сложный
// ArenaModeRulesProfile  = по каким специальным правилам этот режим работает
[CreateAssetMenu(
    fileName = "ArenaModeRulesProfile",
    menuName = "Project/Arena/Mode Rules Profile"
)]
public class ArenaModeRulesProfile : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("Уникальный строковый ID профиля правил.")]
    public string rulesId = "classic_default";

    [Tooltip("Человеческое название профиля правил.")]
    public string rulesDisplayName = "Classic Default Rules";

    [TextArea]
    [Tooltip("Короткая заметка для себя, что это за набор правил.")]
    public string developerNotes = "";

    [Header("Правила старта режима")]
    [Tooltip("Использовать ли мягкий старт / разогрев.")]
    public bool useWarmupStart = true;

    [Tooltip("Сколько первых волн считаются фазой разогрева.")]
    [Min(0)]
    public int warmupWaveCount = 3;

    [Tooltip("С каким бюджетом стартует первая волна в разогреве.")]
    [Min(1)]
    public int warmupStartBudget = 1;

    [Tooltip("На сколько увеличивать бюджет на каждой волне разогрева.")]
    [Min(0)]
    public int warmupBudgetGrowthPerWave = 1;

    [Header("Будущий учёт силы игрока")]
    [Tooltip("Разрешать ли в будущем смещать старт по силе/уровню игрока.")]
    public bool allowPlayerPowerStartShift = false;

    [Tooltip("Максимум, насколько можно будет сдвинуть стартовую волну вверх.")]
    [Min(0)]
    public int maxStartWaveOffset = 0;

    [Header("Разрешённые типы давления")]
    [Tooltip("Разрешать ли раннее усиление через элитников.")]
    public bool allowEarlyElitePressure = true;

    [Tooltip("Разрешать ли давление через окружение (туман, ветер, мороз и т.д.).")]
    public bool allowEnvironmentPressure = true;

    [Tooltip("Разрешать ли давление через лутовые ограничения.")]
    public bool allowLootPressure = true;

    [Tooltip("Разрешать ли давление через сервисы арены.")]
    public bool allowServicePressure = true;

    private void OnValidate()
    {
        if (warmupWaveCount < 0)
            warmupWaveCount = 0;

        if (warmupStartBudget < 1)
            warmupStartBudget = 1;

        if (warmupBudgetGrowthPerWave < 0)
            warmupBudgetGrowthPerWave = 0;

        if (maxStartWaveOffset < 0)
            maxStartWaveOffset = 0;
    }
}
