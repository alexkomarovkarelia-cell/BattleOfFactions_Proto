using UnityEngine;

// Этот скрипт хранит БОЕВЫЕ бонусы игрока.
//
// Сейчас здесь только простая база.
// Позже сюда можно добавить:
// - силу
// - ловкость
// - владение оружием
// - бонусы от экипировки
// - бонусы от бафов
// - штрафы, если не хватает статов
//
// ВАЖНО:
// Сейчас мы не делаем сложную RPG-систему.
// Мы просто закладываем фундамент, чтобы потом не переделывать всё заново.
public class PlayerCombatStats : MonoBehaviour
{
    [Header("Бонусы к атаке")]
    [Tooltip("Плоский бонус к урону. 0 = без бонуса, 3 = +3 урона")]
    [SerializeField] private int flatDamageBonus = 0;

    [Tooltip("Плоский бонус к радиусу удара. Например 0.2 = бьём чуть дальше")]
    [SerializeField] private float flatRadiusBonus = 0f;

    [Tooltip("Множитель скорости атаки. 1 = обычная скорость, 1.2 = быстрее, 0.8 = медленнее")]
    [SerializeField] private float attackSpeedMultiplier = 1f;

    // Возвращаем итоговый урон:
    // базовый урон атаки + бонус игрока
    public int GetFinalDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + flatDamageBonus);
    }

    // Возвращаем итоговый радиус:
    // базовый радиус атаки + бонус игрока
    public float GetFinalRadius(float baseRadius)
    {
        return Mathf.Max(0.1f, baseRadius + flatRadiusBonus);
    }

    // Возвращаем итоговый кулдаун:
    // чем выше скорость атаки, тем меньше задержка между ударами
    public float GetFinalCooldown(float baseCooldown)
    {
        float safeMultiplier = Mathf.Max(0.01f, attackSpeedMultiplier);
        return Mathf.Max(0.05f, baseCooldown / safeMultiplier);
    }
}