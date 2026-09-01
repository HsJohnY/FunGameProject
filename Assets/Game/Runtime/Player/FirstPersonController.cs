using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Player
{
    /// <summary>
    /// 将键鼠输入适配为第一人称角色移动和镜头观察。
    /// 本组件只负责移动表现，不持有交互、工具或任务状态。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("场景引用")]
        [SerializeField] private Camera viewCamera;

        [Header("移动")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.6f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -24f;

        [Header("视角")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.08f;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _toggleCursorAction;
        private float _verticalVelocity;
        private float _pitch;
        private bool _cursorLocked;

        /// <summary>
        /// 当前镜头俯仰角，供调试界面和自动化测试只读查询。
        /// </summary>
        public float ViewPitch => _pitch;

        /// <summary>
        /// 输入动作是否已经启用。
        /// </summary>
        public bool IsInputEnabled => _moveAction != null && _moveAction.enabled;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            if (viewCamera == null)
            {
                Debug.LogError("[Player] 第一人称控制器缺少子摄像机。", this);
                enabled = false;
                return;
            }

            CreateInputActions();
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _sprintAction?.Enable();
            _toggleCursorAction?.Enable();
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
            _toggleCursorAction?.Disable();
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _lookAction?.Dispose();
            _jumpAction?.Dispose();
            _sprintAction?.Dispose();
            _toggleCursorAction?.Dispose();
        }

        private void Update()
        {
            if (_toggleCursorAction.WasPressedThisFrame())
            {
                SetCursorLocked(!_cursorLocked);
            }

            UpdateLook();
            UpdateMovement();
        }

        private void CreateInputActions()
        {
            // M1 灰盒只验证键鼠手感，因此先在适配器内建立最小绑定。
            // 正式键位重绑定进入 MVP 设置阶段后再迁移到 InputActionAsset。
            _moveAction = new InputAction("移动", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _lookAction = new InputAction("观察", InputActionType.Value, "<Mouse>/delta");
            _jumpAction = new InputAction("跳跃", InputActionType.Button, "<Keyboard>/space");
            _sprintAction = new InputAction("冲刺", InputActionType.Button, "<Keyboard>/leftShift");
            _toggleCursorAction = new InputAction("切换鼠标锁定", InputActionType.Button, "<Keyboard>/escape");
        }

        private void UpdateLook()
        {
            if (!_cursorLocked)
            {
                return;
            }

            Vector2 lookDelta = _lookAction.ReadValue<Vector2>() * mouseSensitivity;
            transform.Rotate(Vector3.up, lookDelta.x, Space.Self);

            _pitch = FirstPersonMotionMath.ClampPitch(_pitch - lookDelta.y);
            viewCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            bool grounded = _characterController.isGrounded;
            if (grounded && _verticalVelocity < 0f)
            {
                // 保留轻微向下速度，避免 CharacterController 在斜面或台阶边缘抖动。
                _verticalVelocity = -2f;
            }

            if (grounded && _jumpAction.WasPressedThisFrame())
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _verticalVelocity += gravity * Time.deltaTime;

            Vector2 input = FirstPersonMotionMath.ClampMoveInput(_moveAction.ReadValue<Vector2>());
            Vector3 planarDirection = transform.right * input.x + transform.forward * input.y;
            float speed = walkSpeed * (_sprintAction.IsPressed() ? sprintMultiplier : 1f);
            Vector3 velocity = planarDirection * speed + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
