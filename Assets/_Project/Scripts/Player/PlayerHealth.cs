using UnityEngine;

// PlayerHealth
// Здоровье игрока.
//
// Важно:
// Теперь PlayerHealth НЕ хранит всю общую логику здоровья.
// Общая логика переехала в ObjectHealth.
//
// Здесь остаётся только то,
// что относится ИМЕННО к игроку:
// - звук получения урона
// - показ Game Over
// - временный мост к HUD
//
// В следующем подблоке мы сможем ещё чище отвязать PlayerHealth от HUD.

public class PlayerHealth : ObjectHealth
{
    [Header("Temporary HUD Bridge")]
    [SerializeField] private HUDController hud;
    // Пока оставляем HUD здесь как ПЕРЕХОДНЫЙ МОСТ,
    // чтобы ничего не сломать на Этапе 7A.
    //
    // Но важно:
    // теперь HUD обновляется НЕ из TakeDamage(),
    // а через подписку на событие OnHealthChanged.

    private CharacterSFX3D sfx3D;

    protected override void Awake()
    {
        // 1) Берём 3D звук с игрока
        sfx3D = GetComponent<CharacterSFX3D>();

        // 2) Если HUD не назначен вручную —
        // пытаемся найти его на сцене
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        // 3) Инициализируем базовое здоровье
        base.Awake();
    }

    private void OnEnable()
    {
        // Подписываемся на изменение здоровья.
        OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        // Очень важно отписываться,
        // чтобы не было лишних подписок и ошибок.
        OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        // Так как базовый Awake мог вызваться до подписки,
        // один раз вручную синхронизируем HUD при старте.
        HandleHealthChanged(CurrentHealth, MaxHealth);
    }

    // Реакция игрока на получение урона.
    protected override void OnDamageTaken(int damageAmount, bool isLethal)
    {
        // У игрока звук попадания играем при любом уроне,
        // даже если удар оказался смертельным.
        sfx3D?.PlayHit();
    }

    // Реакция игрока на смерть.
    protected override void OnDeath()
    {
        Debug.Log("Player died!");

        // Пока оставляем как есть:
        // HUD показывает поражение.
        hud?.ShowGameOver();

        // Позже сюда можно добавить:
        // - отключение управления
        // - анимацию смерти
        // - блокировку атаки
    }

    // Временный мост между базовым событием и текущим HUD.
    private void HandleHealthChanged(int current, int max)
    {
        hud?.SetHealth(current, max);
    }
}