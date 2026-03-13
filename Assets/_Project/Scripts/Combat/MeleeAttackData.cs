

using UnityEngine;

// Этот ScriptableObject хранит БАЗОВЫЕ данные одной ближней атаки.
//
// Сейчас мы используем его для кулаков.
// Позже на этой же системе можно сделать:
// - меч
// - копьё
// - топор
// - кинжал
//
// Почему это хорошо:
// - данные атаки лежат отдельно от игрока
// - можно делать разные виды атак без переписывания PlayerAttack
// - удобно расширять проект дальше
[CreateAssetMenu(
    fileName = "AD_Melee_New",
    menuName = "Battle Of Factions/Combat/Melee Attack Data"
)]
public class MeleeAttackData : ScriptableObject
{
    [Header("Служебное имя атаки")]
    [Tooltip("Имя для удобства в проекте. Например: Punch, SwordSlash, SpearThrust")]
    public string attackId = "Punch";

    [Header("Базовые параметры атаки")]
    [Tooltip("Базовый урон атаки БЕЗ учёта бонусов игрока")]
    public int baseDamage = 10;

    [Tooltip("Базовый радиус удара")]
    public float baseRadius = 1.2f;

    [Tooltip("Базовый кулдаун между ударами")]
    public float baseCooldown = 0.35f;

    [Header("Кого можно бить")]
    [Tooltip("Слой целей для этой атаки. Обычно это слой врагов")]
    public LayerMask targetMask;
}