using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OceanGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event Action<Vector2> MoveInputPressed;
        public event Action JumpPressed;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        private PlayerInput _playerInput;

        private void Awake()
        {
            Instance = this;

            _playerInput = new PlayerInput();
            _playerInput.Enable();

            _playerInput.Player.Move.started += GameInput_OnMove;
            _playerInput.Player.Move.performed += GameInput_OnMove;
            _playerInput.Player.Move.canceled += GameInput_OnMove;
            _playerInput.Player.Look.started += GameInput_OnLook;
            _playerInput.Player.Look.performed += GameInput_OnLook;
            _playerInput.Player.Look.canceled += GameInput_OnLook;
            _playerInput.Player.Jump.started += GameInput_OnJump;
            _playerInput.Player.Jump.performed += GameInput_OnJump;
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
            _playerInput.Player.Look.started -= GameInput_OnLook;
            _playerInput.Player.Look.performed -= GameInput_OnLook;
            _playerInput.Player.Look.canceled -= GameInput_OnLook;
            _playerInput.Player.Jump.started -= GameInput_OnJump;
            _playerInput.Player.Jump.performed -= GameInput_OnJump;
            _playerInput.Disable();
        }

        private void GameInput_OnJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
            {
                JumpPressed?.Invoke();
            }
        }

        private void GameInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();

            if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Canceled)
            {
                MoveInputPressed?.Invoke(MoveInput);
            }
        }

        private void GameInput_OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
        }
    }
}
