using UnityEngine;
using UnityEngine.InputSystem;

// Простая ближняя атака: ищем врагов в радиусе вокруг точки AttackPoint
public class PlayerAttack : MonoBehaviour
{
    [Header("Кнопка атаки (клавиатура)")]
    [SerializeField] private Key attackKey = Key.E; // по умолчанию E (можешь поменять в инспекторе)

    [Header("Настройки атаки")]
    [SerializeField] private Transform attackPoint;     // точка перед игроком
    [SerializeField] private float attackRadius = 1.2f; // радиус удара
    [SerializeField] private int damage = 10;           // урон (int)

    [Header("Кого бьём")]
    [SerializeField] private LayerMask enemyMask;       // слой врагов
    [SerializeField] private CharacterSFX3D playerSfx; // можно не ставить вручную, найдём в Awake Это звуки 3Д
    [SerializeField] private float attackCooldown = 0.35f; // Кулдаун атаки
    private float nextAttackTime;

    private void Update()
    {
        // Если нет клавиатуры и мыши (например мобилка) — просто выходим
        if (Keyboard.current == null && Mouse.current == null) return;

        // ✅ Кулдаун: если ещё рано — ничего не делаем
        if (Time.time < nextAttackTime) return;

        // 1) Нажали выбранную клавишу (например E)
        bool pressedKey = false;
        if (Keyboard.current != null)
            pressedKey = Keyboard.current[attackKey].wasPressedThisFrame;

        // 2) Нажали левую кнопку мыши
        bool pressedMouse = false;
        if (Mouse.current != null)
            pressedMouse = Mouse.current.leftButton.wasPressedThisFrame;

        // 3) Если нажали хотя бы одно — атакуем и ставим следующий момент атаки
        if (pressedKey || pressedMouse)
        {
            DoAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }
    private void Awake()
    {
        if (playerSfx == null)
            playerSfx = GetComponent<CharacterSFX3D>();
    }

    private void DoAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("AttackPoint не назначен в PlayerAttack!");
            return;
        }

        // ✅ звук атаки игрока (взмах) — один раз на нажатие
        playerSfx?.PlayAttack();

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius, enemyMask);

        foreach (var hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
    }


// Рисуем радиус удара в Scene для наглядности
private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
