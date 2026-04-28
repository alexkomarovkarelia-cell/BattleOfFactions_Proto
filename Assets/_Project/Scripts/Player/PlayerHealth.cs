using UnityEngine;

// PlayerHealth
// Теперь здоровье игрока уже НЕ обновляет HUD полоску напрямую.
//
// Что здесь остаётся:
// - реакция игрока на получение урона
// - реакция игрока на смерть
// - временный мост к HUD только для ShowGameOver
//
// Что уже вынесено наружу:
// - обновление HP в HUD
// Этим теперь занимается отдельный PlayerHealthHudBridge.

public class PlayerHealth : ObjectHealth
{
    [Header("Temporary Death UI Bridge")]
    [SerializeField] private HUDController hud;
    // Пока оставляем HUD здесь только для показа поражения.
    //
    // Это ВРЕМЕННЫЙ переходный шаг.
    // HP уже не обновляется отсюда.
    // Позже поражение тоже можно будет вынести в отдельный bridge/presenter.

    private CharacterSFX3D sfx3D;

    protected override void Awake()
    {
        // Звук игрока
        sfx3D = GetComponent<CharacterSFX3D>();

        // Пока оставляем поиск HUD для Game Over
        if (hud == null)
            hud = FindFirstObjectByType<HUDController>();

        // Инициализируем базовое здоровье
        base.Awake();
    }

    protected override void OnDamageTaken(int damageAmount, bool isLethal)
    {
        sfx3D?.PlayHit();
    }

    protected override void OnDeath()
    {
        Debug.Log("Player died!");

        // Пока ShowGameOver оставляем здесь,
        // чтобы не раздувать шаг 7B.
        hud?.ShowGameOver();

        // Позже сюда можно добавить:
        // - отключение управления
        // - анимацию смерти
        // - блокировку атаки
    }
}