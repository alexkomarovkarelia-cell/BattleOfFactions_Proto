using UnityEngine;

// ArenaDifficultyProfile — набор настроек сложности для арены.
// Мы делаем это ScriptableObject, чтобы:
// - легко менять цифры без перекомпиляции
// - добавлять новые сложности без переделок кода
// - позже использовать профили и для подземелий/событий
[CreateAssetMenu(menuName = "Project/Difficulty/Arena Difficulty Profile", fileName = "DIF_Arena_")]
public class ArenaDifficultyProfile : ScriptableObject
{
    [Header("ID сложности")]
    public ArenaDifficultyId id = ArenaDifficultyId.Normal;

    [Header("Опционально: переопределить количество волн")]
    [Tooltip("Если включено — EnemySpawner возьмёт totalWaves из этого профиля. Если выключено — волны не зависят от сложности.")]
    public bool overrideTotalWaves = true;

    [Tooltip("Сколько волн в этой сложности (если overrideTotalWaves включён).")]
    public int totalWaves = 30;

    [Header("Множители врагов")]
    [Tooltip("HP врага = базовое HP * этот множитель")]
    public float enemyHpMultiplier = 1f;

    [Tooltip("Урон врага = базовый урон * этот множитель")]
    public float enemyDamageMultiplier = 1f;

    [Tooltip("Скорость врага = базовая скорость * этот множитель")]
    public float enemySpeedMultiplier = 1f;

    [Tooltip("Кулдаун атаки врага = базовый кулдаун * этот множитель. Меньше = чаще бьёт.")]
    public float enemyAttackCooldownMultiplier = 1f;
}