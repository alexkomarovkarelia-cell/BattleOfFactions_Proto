using UnityEngine;

// TargetInfoProvider
// Этот скрипт висит НА САМОЙ ЦЕЛИ.
//
// Примеры целей:
// - враг
// - сундук
// - босс
// - NPC
// - позже другой игрок
//
// Главная задача этого скрипта:
// отдать "сырые" данные о цели.
//
// ВАЖНО:
// - он НЕ выбирает цель
// - он НЕ показывает UI
// - он НЕ решает, что игрок МОЖЕТ видеть
//
// Он только отвечает на вопросы:
// - как зовут цель?
// - есть ли у неё HP?
// - сколько у неё текущего HP?
// - сколько у неё максимального HP?
//
// Почему это правильно:
// тогда UI не будет знать,
// как устроен враг, сундук или босс.
//
// UI просто попросит у цели данные,
// а TargetInfoProvider их отдаст.
[DisallowMultipleComponent]
public class TargetInfoProvider : MonoBehaviour
{
    [Header("Отображаемое имя цели")]
    [SerializeField] private string displayName = "";
    // Это имя, которое можно показывать в UI.
    //
    // Примеры:
    // "Enemy"
    // "Волк"
    // "Бандит"
    // "Квестовый сундук"
    //
    // Если оставить пустым,
    // можно брать имя объекта из сцены.

    [SerializeField] private bool useGameObjectNameIfEmpty = true;
    // Если true и displayName пустой,
    // тогда используем имя GameObject.

    [Header("Данные здоровья")]
    [SerializeField] private bool hasHealthData = false;
    // true  = цель умеет отдавать HP
    // false = HP нет или пока не хотим его показывать/использовать
    //
    // Для врага позже это обычно будет true.
    // Для обычного сундука чаще false.

    // Кэш ссылки на здоровье цели.
    // Пока на Этапе 5 у нас это EnemyHealth,
    // потому что валидная цель — обычный враг.
    // Позже сюда можно будет аккуратно расширять поддержку.
    private EnemyHealth enemyHealth;

    // Публичное свойство:
    // может ли цель отдавать данные HP.
    public bool HasHealthData => hasHealthData;

    private void Awake()
    {
        // Если цель должна уметь отдавать HP,
        // пробуем найти EnemyHealth на объекте или у родителя.
        if (hasHealthData)
        {
            enemyHealth = GetComponent<EnemyHealth>();

            if (enemyHealth == null)
                enemyHealth = GetComponentInParent<EnemyHealth>();
        }
    }

    // Вернуть отображаемое имя цели
    public string GetDisplayName()
    {
        // Если красивое имя задано вручную — используем его
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        // Если имя не задано, но разрешено брать имя объекта —
        // берём имя GameObject
        if (useGameObjectNameIfEmpty)
        {
            string rawName = gameObject.name;
            string cleanName = rawName.Replace("(Clone)", "").Trim();
            return cleanName;
        }

        return "";
    }

    // Вернуть текущее HP цели
    public int GetCurrentHealth()
    {
        // Если цель не поддерживает HP —
        // возвращаем 0
        if (!hasHealthData)
            return 0;

        // Если EnemyHealth не найден —
        // тоже возвращаем 0
        if (enemyHealth == null)
            return 0;

        return enemyHealth.CurrentHealth;
    }

    // Вернуть максимальное HP цели
    public int GetMaxHealth()
    {
        if (!hasHealthData)
            return 0;

        if (enemyHealth == null)
            return 0;

        return enemyHealth.MaxHealth;
    }
}