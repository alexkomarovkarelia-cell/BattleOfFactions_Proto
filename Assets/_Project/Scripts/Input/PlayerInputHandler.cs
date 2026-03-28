using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInputHandler
// Центральный скрипт ввода действий игрока.
//
// На этом этапе он отвечает за:
// 1. Attack
// 2. Ability1
// 3. Interact
//
// Важно:
// - этот скрипт только читает input
// - он НЕ делает урон
// - он НЕ меняет кулдауны
// - он НЕ ищет объекты сам
//
// Он только вызывает нужные методы у других систем игрока.
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Ссылки на системы игрока")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerAttackSpeedAbility attackSpeedAbility;
    [SerializeField] private PlayerInteraction playerInteraction;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();

        if (attackSpeedAbility == null)
            attackSpeedAbility = GetComponent<PlayerAttackSpeedAbility>();

        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Attack.performed += OnAttackPerformed;
        inputActions.Player.Ability1.performed += OnAbility1Performed;
        inputActions.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Ability1.performed -= OnAbility1Performed;
        inputActions.Player.Interact.performed -= OnInteractPerformed;

        inputActions.Player.Disable();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        playerAttack?.TryAttack();
    }

    private void OnAbility1Performed(InputAction.CallbackContext context)
    {
        attackSpeedAbility?.TryActivate();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        playerInteraction?.TryInteract();
    }
}