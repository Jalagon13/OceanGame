using UnityEngine;

namespace LittleGuyGamePrototype
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform _cameraYawPivot;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpHeight = 1.4f;
        [SerializeField] private float _jumpCooldown = 0.15f;
        [SerializeField] private float _airMoveMultiplier = 0.6f;
        [SerializeField] private float _gravity = 25f;

        [Header("Camera")]
        [SerializeField] private float _lookSensitivityX = 2f;
        [SerializeField] private float _lookSensitivityY = 2f;
        [SerializeField] private float _maxLookPitch = 80f;

        private CharacterController _cc;
        private Vector2 _moveInput;
        private float _pitch;
        private float _yaw;
        private float _verticalVelocity;
        private float _jumpCooldownTimer;

        private void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.MoveInputPressed += HandleMoveInput;
                GameInput.Instance.JumpPressed += HandleJumpPressed;
            }
        }

        private void OnDisable()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.MoveInputPressed -= HandleMoveInput;
                GameInput.Instance.JumpPressed -= HandleJumpPressed;
            }
        }

        private void Update()
        {
            if (_cc == null)
            {
                return;
            }

            UpdateLook();
            UpdateJumpCooldown();
            UpdateMovement();
        }

        private void HandleMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
        }

        private void HandleJumpPressed()
        {
            TryJump();
        }

        private void UpdateLook()
        {
            Vector2 lookInput = GameInput.Instance.LookInput;
            if (lookInput == Vector2.zero)
            {
                return;
            }

            _yaw += lookInput.x * _lookSensitivityX;
            _pitch -= lookInput.y * _lookSensitivityY;
            _pitch = Mathf.Clamp(_pitch, -_maxLookPitch, _maxLookPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (_cameraYawPivot != null)
            {
                _cameraYawPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void UpdateJumpCooldown()
        {
            if (_jumpCooldownTimer > 0f)
            {
                _jumpCooldownTimer -= Time.deltaTime;
            }
        }

        private void UpdateMovement()
        {
            bool isGrounded = _cc.isGrounded;

            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            float moveMultiplier = isGrounded ? 1f : _airMoveMultiplier;
            Vector3 inputDirection = Vector3.zero;

            if (_moveInput != Vector2.zero)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                forward.y = 0f;
                right.y = 0f;

                if (forward.sqrMagnitude > 0f)
                {
                    forward.Normalize();
                }

                if (right.sqrMagnitude > 0f)
                {
                    right.Normalize();
                }

                inputDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;
            }

            Vector3 moveVelocity = inputDirection * (_moveSpeed * moveMultiplier);
            _verticalVelocity -= _gravity * Time.deltaTime;

            if (_cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            Vector3 finalVelocity = new Vector3(moveVelocity.x, _verticalVelocity, moveVelocity.z);
            _cc.Move(finalVelocity * Time.deltaTime);
        }

        private void TryJump()
        {
            if (_cc == null || _jumpCooldownTimer > 0f || !_cc.isGrounded)
            {
                return;
            }

            float jumpVelocity = Mathf.Sqrt(_jumpHeight * 2f * _gravity);
            _verticalVelocity = jumpVelocity;
            _jumpCooldownTimer = _jumpCooldown;
        }
    }
}
