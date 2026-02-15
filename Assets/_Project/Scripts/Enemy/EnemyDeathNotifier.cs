using UnityEngine;

// Этот компонент сообщает спавнеру, что враг "убит".
// ВАЖНО: лучше вызывать NotifyKilled() прямо в Enemy.Die(),
// а OnDestroy оставить как запасной вариант.
public class EnemyDeathNotifier : MonoBehaviour
{
    private EnemySpawner spawner;
    private bool notified = false; // чтобы не засчитать два раза

    // Вызывается спавнером после Instantiate
    public void Init(EnemySpawner s)
    {
        spawner = s;
        notified = false;
    }

    // ✅ Главный метод: вызвать в момент смерти врага
    public void NotifyKilled()
    {
        if (notified) return;                // защита от повторов
        if (!Application.isPlaying) return;  // не считать при выходе из Play Mode
        if (spawner == null) return;

        notified = true;
        spawner.OnEnemyDestroyed();
    }

    // Запасной вариант: если забыли вызвать NotifyKilled(),
    // тогда засчитаем при Destroy
    private void OnDestroy()
    {
        NotifyKilled();
    }
}
