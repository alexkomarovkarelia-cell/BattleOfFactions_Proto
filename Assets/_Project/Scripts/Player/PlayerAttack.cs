using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Этот скрипт отвечает за АТАКУ игрока.
//
// ВАЖНО:
// Сейчас это основа именно для БЛИЖНЕГО БОЯ (кулаки).
// Мы не делаем здесь "всё оружие мира сразу".
// Мы делаем ПРАВИЛЬНЫЙ ФУНДАМЕНТ:
//
// 1) Игрок нажимает кнопку атаки через Input Actions
// 2) Скрипт проверяет кулдаун
// 3) Берёт базовые данные атаки из MeleeAttackData
// 4) Берёт бонусы игрока из PlayerCombatStats
// 5) Ищет цели в радиусе удара
// 6) Наносит урон каждой цели только 1 раз за удар
//
// Почему это хороший путь:
// - сейчас работают кулаки
// - потом можно сделать новый Data Asset для меча / копья / топора
// - позже можно добавить бонусы, штрафы, криты, требования по статам
// - не придётся переписывать систему с нуля
public class PlayerAttack : MonoBehaviour
{
    [Header("Данные текущей атаки")]
    [SerializeField] private MeleeAttackData currentAttackData;
    // Это ссылка на ScriptableObject с БАЗОВЫМИ параметрами атаки.
    // Сейчас сюда назначим data asset кулаков.
    // Позже сюда можно будет назначить data asset меча, копья и т.д.

    [Header("Точка удара")]
    [SerializeField] private Transform attackPoint;
    // Это точка перед игроком, вокруг которой ищем цели.
    // Обычно это пустой объект перед персонажем.

    [Header("Дополнительные ссылки")]
    [SerializeField] private CharacterSFX3D playerSfx;
    // Сюда можно вручную назначить компонент со звуками игрока.
    // Если не назначишь - попробуем найти автоматически в Awake().

    [SerializeField] private PlayerCombatStats combatStats;
    // Здесь лежат бонусы игрока:
    // + урон
    // + радиус
    // + скорость атаки
    // Позже сюда же можно расширить:
    // шанс крита, множитель крита, штрафы, бафы, дебафы и т.д.

    // Экземпляр Input Actions.
    // Мы уже используем такой же подход в PlayerMovement.
    private PlayerInputActions inputActions;

    // Время, когда снова разрешено атаковать.
    // Пока текущее время меньше nextAttackTime - новая атака запрещена.
    private float nextAttackTime = 0f;

    // Буфер коллайдеров для OverlapSphereNonAlloc.
    // Почему так:
    // обычный OverlapSphere создаёт новый массив и может плодить мусор в памяти.
    // Здесь мы создаём массив ОДИН раз и переиспользуем его.
    //
    // 16 сейчас вполне хватит для MVP арены.
    // Если потом в радиусе может быть больше целей - увеличим размер.
    private readonly Collider[] hitBuffer = new Collider[16];

    // Набор уникальных целей.
    // Нужен, чтобы один и тот же враг не получил урон дважды за один удар,
    // если у него несколько коллайдеров.
    private readonly HashSet<EnemyHealth> uniqueEnemies = new HashSet<EnemyHealth>();

    // Awake вызывается самым первым при запуске объекта.
    private void Awake()
    {
        // Создаём объект новой системы ввода.
        inputActions = new PlayerInputActions();

        // Если ссылки не назначены руками - пытаемся найти их автоматически.
        if (playerSfx == null)
            playerSfx = GetComponent<CharacterSFX3D>();

        if (combatStats == null)
            combatStats = GetComponent<PlayerCombatStats>();
    }

    // OnEnable вызывается, когда объект/скрипт включается.
    private void OnEnable()
    {
        // Включаем карту действий Player.
        inputActions.Player.Enable();

        // Подписываемся на кнопку Attack.
        // Когда действие Attack сработает - вызовется OnAttackPerformed.
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    // OnDisable вызывается, когда объект/скрипт выключается.
    private void OnDisable()
    {
        // Очень важно отписываться от событий,
        // чтобы не получить двойные вызовы и странные ошибки.
        inputActions.Player.Attack.performed -= OnAttackPerformed;

        // Выключаем карту действий Player.
        inputActions.Player.Disable();
    }

    // Этот метод вызывается системой ввода, когда игрок нажал кнопку Attack.
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        TryAttack();
    }

    // Основной метод попытки атаки.
    // Здесь мы:
    // - проверяем настройки
    // - проверяем кулдаун
    // - считаем итоговые параметры удара
    // - запускаем сам удар
    private void TryAttack()
    {
        // Проверяем, назначены ли данные атаки.
        if (currentAttackData == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен Current Attack Data!");
            return;
        }

        // Проверяем, назначена ли точка удара.
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerAttack: не назначен AttackPoint!");
            return;
        }

        // Проверка кулдауна:
        // если текущее время ещё меньше времени следующей атаки - выходим.
        if (Time.time < nextAttackTime)
            return;

        // Берём БАЗОВЫЕ параметры атаки из ScriptableObject.
        int finalDamage = currentAttackData.baseDamage;
        float finalRadius = currentAttackData.baseRadius;
        float finalCooldown = currentAttackData.baseCooldown;

        // Если на игроке есть PlayerCombatStats -
        // считаем ИТОГОВЫЕ параметры уже с бонусами.
        if (combatStats != null)
        {
            finalDamage = combatStats.GetFinalDamage(currentAttackData.baseDamage);
            finalRadius = combatStats.GetFinalRadius(currentAttackData.baseRadius);
            finalCooldown = combatStats.GetFinalCooldown(currentAttackData.baseCooldown);
        }

        // Сразу ставим время следующей разрешённой атаки.
        nextAttackTime = Time.time + finalCooldown;

        // Проигрываем звук атаки игрока.
        playerSfx?.PlayAttack();

        // Делаем сам удар.
        DoAttack(finalDamage, finalRadius);
    }

    // Метод, который реально ищет цели и наносит урон.
    private void DoAttack(int finalDamage, float finalRadius)
    {
        // На всякий случай очищаем набор уникальных врагов перед новым ударом.
        uniqueEnemies.Clear();

        // Ищем все коллайдеры в радиусе удара.
        //
        // Мы используем OverlapSphereNonAlloc:
        // - результат пишется в уже готовый массив hitBuffer
        // - меньше мусора в памяти
        //
        // currentAttackData.targetMask определяет, по каким слоям мы вообще ищем цели.
        int hitCount = Physics.OverlapSphereNonAlloc(
            attackPoint.position,
            finalRadius,
            hitBuffer,
            currentAttackData.targetMask,
            QueryTriggerInteraction.Ignore
        );

        // Перебираем все найденные коллайдеры.
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];

            // Защита на случай пустого элемента.
            if (hit == null)
                continue;

            // Пока для MVP ищем EnemyHealth.
            // ВАЖНО:
            // Позже мы сможем расширить это до общей системы повреждаемых объектов
            // (например враг, ящик, руда, опора, стена и т.д.).
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
                continue;

            // Если этот враг уже был добавлен в набор -
            // значит мы уже наносили ему урон в ЭТОМ ударе.
            // Такое бывает, если у врага несколько коллайдеров.
            if (!uniqueEnemies.Add(enemyHealth))
                continue;

            // Наносим урон врагу.
            enemyHealth.TakeDamage(finalDamage);
        }
    }

    // Рисуем радиус удара в Scene View, когда объект выделен.
    // Это очень удобно для настройки.
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        // Радиус для предпросмотра.
        // Если данные атаки назначены - показываем их базовый радиус.
        // Если нет - рисуем запасной радиус 1.
        float previewRadius = 1f;

        if (currentAttackData != null)
            previewRadius = currentAttackData.baseRadius;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, previewRadius);
    }
}