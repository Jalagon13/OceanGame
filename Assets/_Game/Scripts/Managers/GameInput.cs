using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OceanGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event Action<Vector2> OnMoveInputPressed;
        public event Action OnJumpPressed;
        public event Action OnPrimaryActionPressed;
        public event Action OnSecondaryActionPressed;

        public Vector2 MoveInput { get; private set; }
        public bool JumpHold { get; private set; }

        private PlayerInput _playerInput;

        private void Awake()
        {
            Instance = this;

            _playerInput = new PlayerInput();
            _playerInput.Enable();

            _playerInput.Player.Move.started += GameInput_OnMove;
            _playerInput.Player.Move.performed += GameInput_OnMove;
            _playerInput.Player.Move.canceled += GameInput_OnMove;
            
            _playerInput.Player.Jump.started += GameInput_OnJump;
            _playerInput.Player.Jump.canceled += GameInput_OnJump;
            
            _playerInput.Player.PrimaryAction.started += GameInput_OnPrimaryAction;
            _playerInput.Player.SecondaryAction.started += GameInput_OnSecondaryAction;
        }

        private void OnDestroy()
        {
            if (_playerInput == null)
            {
                return;
            }

            _playerInput.Player.Move.started -= GameInput_OnMove;
            _playerInput.Player.Move.performed -= GameInput_OnMove;
            _playerInput.Player.Move.canceled -= GameInput_OnMove;
            
            _playerInput.Player.Jump.started -= GameInput_OnJump;
            _playerInput.Player.Jump.canceled -= GameInput_OnJump;

            _playerInput.Player.PrimaryAction.started -= GameInput_OnPrimaryAction;
            _playerInput.Player.SecondaryAction.started -= GameInput_OnSecondaryAction;
            _playerInput.Disable();
        }

        private void GameInput_OnPrimaryAction(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Debug.Log($"prim");
                OnPrimaryActionPressed?.Invoke();
            }
        }

        private void GameInput_OnSecondaryAction(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                Debug.Log($"sec");
                OnSecondaryActionPressed?.Invoke();
            }
        }

        private void GameInput_OnJump(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                JumpHold = true;
            }
            else if(context.phase == InputActionPhase.Canceled)
            {
                JumpHold = false;
            }
        
            if (context.phase == InputActionPhase.Started)
            {
                OnJumpPressed?.Invoke();
            }
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();

            if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Canceled)
            {
                OnMoveInputPressed?.Invoke(MoveInput);
            }
        }
    }
}
