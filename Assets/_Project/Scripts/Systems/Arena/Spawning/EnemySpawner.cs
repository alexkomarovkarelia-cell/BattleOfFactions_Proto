using System.Collections;
using UnityEngine;

// EnemySpawner — управляет волнами врагов:
// 1) Запускает волны 1..totalWaves
// 2) В каждой волне нужно убить enemiesThisWave врагов
// 3) Спавнит врагов с интервалом, но не больше maxAlive одновременно
// 4) Между волнами делает паузу с отсчётом на HUD
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab врага")]
    [SerializeField] private GameObject enemyPrefab; // Префаб врага, которого будем создавать (Instantiate)

    [Header("Точки спавна")]
    [SerializeField] private Transform[] spawnPoints; // Массив точек (твои EnemySpawnPoint_1..4)

    [Header("Сколько волн всего")]
    [SerializeField] private int totalWaves = 50; // Всего волн (позже можно сделать 10 для MVP)

    [Header("Формула количества врагов")]
    [SerializeField] private int startEnemies = 1;       // Сколько врагов в 1-й волне
    [SerializeField] private int addEnemiesPerWave = 2;  // Сколько добавляем на каждую следующую волну
                                                         // Пример: 1,3,5,7,9...

    [Header("Лимит врагов на сцене одновременно")]
    [SerializeField] private int maxAlive = 4; // Чтобы не появлялось слишком много врагов сразу

    [Header("Интервал между спавнами (сек)")]
    [SerializeField] private float spawnInterval = 0.5f; // Пауза между созданием врагов внутри волны

    [Header("Пауза между волнами (сек)")]
    [SerializeField] private int breakSeconds = 4; // Сколько секунд отдых между волнами

    [Header("HUD")]
    [SerializeField] private HUDController hud; // UI: волна, прогресс, сообщения, победа

    // Публичные данные (можно показывать в экране результата)
    public int WavesCompleted => wavesCompleted;
    public int TotalWaves => totalWaves;

    // Состояния
    private bool started = false;  // чтобы не запускать спавн повторно
    private bool stopped = false;  // чтобы остановить спавн (например при смерти игрока/победе)

    // Текущие данные по волнам
    private int currentWave = 0;
    private int wavesCompleted = 0;

    // Данные текущей волны
    private int enemiesThisWave = 0;   // сколько врагов нужно убить в этой волне
    private int spawnedThisWave = 0;   // сколько врагов уже заспавнили
    private int killedThisWave = 0;    // сколько уже убили

    // Сколько врагов сейчас живых на сцене
    private int alive = 0;

    // Чтобы не спавнить 2 раза подряд в одной и той же точке
    private int lastSpawnIndex = -1;
    [Header("Difficulty (Arena)")]
    [SerializeField] private ArenaDifficultyId startDifficulty = ArenaDifficultyId.Normal;

    // Профили (ScriptableObject)
    [SerializeField] private ArenaDifficultyProfile easyProfile;
    [SerializeField] private ArenaDifficultyProfile normalProfile;
    [SerializeField] private ArenaDifficultyProfile hardProfile;

    // Текущий активный профиль (выбран игроком)
    private ArenaDifficultyProfile activeProfile;
    private ArenaDifficultyProfile GetProfile(ArenaDifficultyId id)
    {
        switch (id)
        {
            case ArenaDifficultyId.Easy: return easyProfile;
            case ArenaDifficultyId.Hard: return hardProfile;
            default: return normalProfile;
        }
    }

    // Вызываем из UI перед стартом
    public void SetDifficulty(ArenaDifficultyId id)
    {
        activeProfile = GetProfile(id);

        // Если профили не назначены — просто работаем как раньше
        if (activeProfile == null) return;

        // Опционально меняем totalWaves (если override включен)
        if (activeProfile.overrideTotalWaves)
        {
            totalWaves = Mathf.Max(1, activeProfile.totalWaves);
            hud?.SetWave(0, totalWaves); // обновим UI ещё до старта
        }
    }

    private void Start()
    {
        // Если HUD не назначен в инспекторе — пробуем найти автоматически в сцене
        if (hud == null) hud = FindFirstObjectByType<HUDController>();
        // Выбираем сложность по умолчанию (Normal)
        SetDifficulty(startDifficulty);

        // До старта показываем 0/totalWaves
        hud?.SetWave(0, totalWaves);
        hud?.SetWaveProgress(0, 0);
        hud?.HideCenterMessage();
       
    }

    // Вызывается, когда нажали "Start" (или по твоей логике)
    public void BeginSpawning()
    {
        if (started) return; // защита: если уже стартовали — второй раз не запускаем

        started = true;
        stopped = false;

        // Запускаем корутину — это "параллельный" процесс по волнам
        StartCoroutine(WavesLoop());
    }

    // Остановка спавна (например при поражении или победе)
    public void StopSpawning()
    {
        stopped = true;
    }

    // Формула количества врагов на волну
    private int GetEnemiesForWave(int waveNumber)
    {
        // Простая формула: 1,3,5,7,9...
        // startEnemies = 1
        // addEnemiesPerWave = 2
        return startEnemies + (waveNumber - 1) * addEnemiesPerWave;
    }

    // Главный цикл волн
    private IEnumerator WavesLoop()
    {
        // Проверки, чтобы не словить ошибки
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab не назначен!");
            yield break;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: spawnPoints пустой!");
            yield break;
        }

        // Волны 1..totalWaves
        for (currentWave = 1; currentWave <= totalWaves; currentWave++)
        {
            if (stopped) yield break; // если нас остановили — выходим из корутины

            // Настройка волны
            enemiesThisWave = GetEnemiesForWave(currentWave);
            spawnedThisWave = 0;
            killedThisWave = 0;

            // Обновляем HUD
            hud?.SetWave(currentWave, totalWaves);
            hud?.SetWaveProgress(0, enemiesThisWave);

            // Показать короткое сообщение о старте волны
            hud?.ShowCenterMessage($"Волна {currentWave}/{totalWaves} начинается!");
            yield return new WaitForSeconds(1f);
            hud?.HideCenterMessage();

            // Пока не убили всех врагов волны — продолжаем
            while (killedThisWave < enemiesThisWave)
            {
                if (stopped) yield break;

                // Условия спавна:
                // 1) сейчас живых меньше лимита maxAlive
                // 2) мы ещё не заспавнили всех врагов этой волны
                if (alive < maxAlive && spawnedThisWave < enemiesThisWave)
                {
                    SpawnOneEnemy(); // создаём одного врага
                    yield return new WaitForSeconds(spawnInterval); // пауза между спавнами
                }
                else
                {
                    // Если лимит живых достигнут или уже всех заспавнили — ждём кадр
                    yield return null;
                }
            }

            // Волна пройдена
            wavesCompleted = currentWave;

            // Если это последняя волна — победа
            if (currentWave >= totalWaves)
            {
                hud?.ShowWin();
                StopSpawning();
                yield break;
            }

            // Сообщение + отсчёт до следующей волны
            hud?.ShowCenterMessage($"Волна {currentWave} пройдена!");

            for (int t = breakSeconds; t >= 1; t--)
            {
                if (stopped) yield break;
                hud?.ShowCenterMessage($"Следующая волна через {t}...");
                yield return new WaitForSeconds(1f);
            }

            hud?.HideCenterMessage();
        }
    }

    // Создание одного врага (Instantiate)
    private void SpawnOneEnemy()
    {
        // 1) Выбираем случайную точку спавна (рандом)
        int index = Random.Range(0, spawnPoints.Length);

        // 2) Чтобы не спавнить два раза подряд в одной и той же точке
        //    (если точек больше одной)
        if (spawnPoints.Length > 1)
        {
            while (index == lastSpawnIndex)
                index = Random.Range(0, spawnPoints.Length);
        }

        lastSpawnIndex = index; // запоминаем последнюю точку

        // 3) Берём Transform точки
        Transform point = spawnPoints[index];

        // 4) Создаём врага в позиции точки
        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        ApplyDifficultyToEnemy(enemy);

        // 5) Обновляем счётчики этой волны
        spawnedThisWave++;
        alive++;

        // 6) Добавляем нотификатор смерти:
        //    Он нужен, чтобы спавнер узнал, что враг УМЕР и уничтожился (Destroy)
        //    Тогда мы уменьшаем alive и увеличиваем killedThisWave
        EnemyDeathNotifier notifier = enemy.GetComponent<EnemyDeathNotifier>();
        if (notifier == null) notifier = enemy.AddComponent<EnemyDeathNotifier>();
        notifier.Init(this);
    }

    // Этот метод вызывается нотификатором, когда враг уничтожен (Destroy)
    public void OnEnemyDestroyed()
    {
        if (stopped) return;

        // Уменьшаем число живых врагов (но не ниже 0)
        alive = Mathf.Max(alive - 1, 0);

        // Увеличиваем число убитых в этой волне
        killedThisWave++;

        // Обновляем прогресс на HUD: убили / нужно убить
        hud?.SetWaveProgress(killedThisWave, enemiesThisWave);
    }
    private void ApplyDifficultyToEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        if (activeProfile == null) return; // если профиль не назначен — ничего не делаем

        // HP
        enemy.GetComponent<EnemyHealth>()?.ApplyDifficulty(activeProfile.enemyHpMultiplier);

        // Скорость
        enemy.GetComponent<EnemyChase>()?.ApplyDifficulty(activeProfile.enemySpeedMultiplier);

        // Урон/кулдаун атаки
        enemy.GetComponent<EnemyMeleeAttack>()
            ?.ApplyDifficulty(activeProfile.enemyDamageMultiplier, activeProfile.enemyAttackCooldownMultiplier);
    }
}
