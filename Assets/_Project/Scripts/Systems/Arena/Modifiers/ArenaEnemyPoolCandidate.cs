using UnityEngine;

// ArenaEnemyPoolCandidate
// Это runtime-кандидат врага для текущей волны.
//
// ВАЖНО:
// Это НЕ asset.
// Это НЕ prefab.
// Это временный объект данных, который живёт только во время расчёта пула.
//
// Зачем он нужен:
// - чтобы не менять ScriptableObject-профили напрямую;
// - чтобы можно было временно менять вес выбора;
// - чтобы можно было убирать/добавлять кандидатов через модификаторы.
[System.Serializable]
public class ArenaEnemyPoolCandidate
{
    [Tooltip("Исходный профиль врага.")]
    public ArenaWaveEnemyProfile sourceProfile;

    [Tooltip("ID врага, который потом пойдёт в спавнер.")]
    public string enemyTypeId = "melee_basic";

    [Tooltip("Эффективный вес выбора на этой волне.")]
    public int effectiveWeight = 1;

    [Tooltip("Включён ли этот кандидат после всех модификаторов.")]
    public bool isEnabled = true;

    [TextArea]
    [Tooltip("Отладочная заметка.")]
    public string debugNote = "";

    public void ValidateData()
    {
        if (string.IsNullOrWhiteSpace(enemyTypeId))
            enemyTypeId = "melee_basic";

        if (effectiveWeight < 1)
            effectiveWeight = 1;
    }
}