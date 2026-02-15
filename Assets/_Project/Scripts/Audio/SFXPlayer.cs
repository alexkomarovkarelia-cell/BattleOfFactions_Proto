using UnityEngine;

// SFXPlayer — центральный проигрыватель коротких звуков (SFX).
// Идея как в норм проектах: из кода вызываем события (PlayHitEnemy, PlayHurtPlayer...),
// а клипы настраиваются в инспекторе.
public class SFXPlayer : MonoBehaviour
{
    // Быстрый доступ из любого скрипта без протаскивания ссылок (для MVP это норм).
    public static SFXPlayer I { get; private set; }

    [Header("AudioSource (источник звука)")]
    [SerializeField] private AudioSource source;

    [Header("Клипы (у тебя уже есть)")]
    [SerializeField] private AudioClip coinClip;   // звук монеты
    [SerializeField] private AudioClip medkitClip; // звук лечения

    [Header("Combat SFX (добавим сейчас)")]
    [SerializeField] private AudioClip hitEnemyClip;   // игрок нанёс урон врагу (hit)
    [SerializeField] private AudioClip hurtPlayerClip; // игрок получил урон (hurt)

    [Header("Game Flow SFX")]
    [SerializeField] private AudioClip startClip; // старт игры/забега
    [SerializeField] private AudioClip winClip;   // победа
    [SerializeField] private AudioClip loseClip;  // поражение

    [Header("Поведение (чтобы не звучало одинаково)")]
    [Tooltip("Рандомный pitch для ударов/получения урона (делает звук живее)")]
    [SerializeField] private bool randomPitchForCombat = true;

    [Tooltip("Диапазон pitch для боевых звуков")]
    [SerializeField] private Vector2 combatPitchRange = new Vector2(0.95f, 1.05f);

    private void Awake()
    {
        // Singleton
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        // Если не назначили source вручную — берём с этого же объекта
        if (source == null)
            source = GetComponent<AudioSource>();

        // Если AudioSource вообще нет — предупредим
        if (source == null)
            Debug.LogWarning("SFXPlayer: нет AudioSource на объекте SFXPlayer.");
        else
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D звук (MVP)
        }
    }

    // -------------------------
    // Уже было (оставляем как есть)
    // -------------------------
    public void PlayCoin() => PlayOneShotSafe(coinClip, isCombat: false);
    public void PlayMedkit() => PlayOneShotSafe(medkitClip, isCombat: false);

    // -------------------------
    // Новое для Блока А
    // -------------------------
    public void PlayHitEnemy() => PlayOneShotSafe(hitEnemyClip, isCombat: true);
    public void PlayHurtPlayer() => PlayOneShotSafe(hurtPlayerClip, isCombat: true);

    public void PlayStart() => PlayOneShotSafe(startClip, isCombat: false);
    public void PlayWin() => PlayOneShotSafe(winClip, isCombat: false);
    public void PlayLose() => PlayOneShotSafe(loseClip, isCombat: false);

    // -------------------------
    // Внутренняя безопасная игра клипа
    // -------------------------
    private void PlayOneShotSafe(AudioClip clip, bool isCombat)
    {
        if (source == null) return;
        if (clip == null) return;

        // Для ударов/урона — делаем небольшой рандом pitch
        if (isCombat && randomPitchForCombat)
            source.pitch = Random.Range(combatPitchRange.x, combatPitchRange.y);
        else
            source.pitch = 1f;

        source.PlayOneShot(clip);
    }
}
