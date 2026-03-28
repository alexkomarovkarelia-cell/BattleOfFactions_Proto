using UnityEngine;
using UnityEngine.InputSystem;

// Этот скрипт - центральная точка ввода действий игрока.
//

// Позже сюда добавим:
// - Ability1
// - Interact
//
// Главная идея:
// сам этот скрипт читает input,
// а другие игровые скрипты только выполняют действия.
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Ссылки на системы игрока")]
    [SerializeField] private PlayerAttack playerAttack;

    // Сгенерированный Unity класс из PlayerInputActions.inputactions
    private PlayerInputActions inputActions;

    private void Awake()
    {
        // Создаём экземпляр input actions
        inputActions = new PlayerInputActions();

        // Если забыли назначить ссылку вручную,
        // пробуем найти PlayerAttack на этом же объекте
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();
    }

    private void OnEnable()
    {
        // Включаем карту действий Player
        inputActions.Player.Enable();

        // Подписываемся на событие нажатия Attack
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        // Отписываемся, чтобы не было дублей подписок
        inputActions.Player.Attack.performed -= OnAttackPerformed;

        // Выключаем карту действий
        inputActions.Player.Disable();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // Просим систему атаки попробовать атаковать
        playerAttack?.TryAttack();
    }
}