using UnityEngine;
using UnityEngine.InputSystem;

// PlayerInputHandler
// Центральный скрипт ввода действий игрока.
//
// На текущем этапе он отвечает за:
// 1. Attack
// 2. Ability1
// 3. Interact
// 4. Временную активацию soft lock через Left Ctrl + ЛКМ
// 5. Отмену soft lock через Left Ctrl без ЛКМ
//
// Важно:
// - этот скрипт только читает input
// - он НЕ делает урон сам
// - он НЕ двигает игрока
// - он НЕ выбирает механику боя сам
//
// Он только решает,
// какой метод какой системы вызвать.
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Ссылки на системы игрока")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerAttackSpeedAbility attackSpeedAbility;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerSoftLockAttack playerSoftLockAttack;

    private PlayerInputActions inputActions;

    // Эти флаги нужны, чтобы правильно отличать:
    // 1. Ctrl + ЛКМ -> это активация soft lock
    // 2. Ctrl без ЛКМ -> это отмена soft lock
    private bool leftCtrlWasPressedLastFrame = false;
    private bool ctrlPressWasUsedWithAttack = false;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();

        if (attackSpeedAbility == null)
            attackSpeedAbility = GetComponent<PlayerAttackSpeedAbility>();

        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();

        if (playerSoftLockAttack == null)
            playerSoftLockAttack = GetComponent<PlayerSoftLockAttack>();
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

    private void Update()
    {
        HandleSoftLockCancelInput();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // Если во время ЛКМ зажат Left Ctrl,
        // это попытка включить / переключить мягкий режим
        if (IsSoftLockActivationComboPressed())
        {
            // Помечаем, что текущее нажатие Ctrl использовалось вместе с ЛКМ.
            // Это важно:
            // когда Ctrl потом отпустят, мы НЕ должны считать это "простой отменой".
            ctrlPressWasUsedWithAttack = true;

            bool handledBySoftLock = playerSoftLockAttack != null &&
                                     playerSoftLockAttack.TryActivateOrSwitchSoftLock();

            // Если soft lock сработал —
            // обычную атаку в этот кадр не запускаем
            if (handledBySoftLock)
                return;
        }

        // Если комбинации не было
        // или soft lock не смог сработать —
        // выполняем обычную атаку
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

    private bool IsSoftLockActivationComboPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.leftCtrlKey.isPressed;
    }

    // Логика отмены мягкого режима:
    // - игрок просто нажал и отпустил Left Ctrl
    // - но НЕ использовал его вместе с ЛКМ
    //
    // Тогда считаем это командой "снять текущий таргетный режим".
    private void HandleSoftLockCancelInput()
    {
        if (Keyboard.current == null)
            return;

        bool leftCtrlIsPressedNow = Keyboard.current.leftCtrlKey.isPressed;

        // Момент нажатия Ctrl
        if (!leftCtrlWasPressedLastFrame && leftCtrlIsPressedNow)
        {
            ctrlPressWasUsedWithAttack = false;
        }

        // Момент отпускания Ctrl
        if (leftCtrlWasPressedLastFrame && !leftCtrlIsPressedNow)
        {
            // Если за это нажатие Ctrl НЕ использовался вместе с ЛКМ,
            // считаем это командой отмены.
            if (!ctrlPressWasUsedWithAttack)
            {
                playerSoftLockAttack?.CancelSoftLock();
            }
        }

        leftCtrlWasPressedLastFrame = leftCtrlIsPressedNow;
    }
}