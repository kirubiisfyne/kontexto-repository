using UnityEngine;

namespace Master.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        private CharacterController controller;
        private float x, y;
    
        [SerializeField] private Transform cameraAnchor;
    
        [Header("Movement")]
        public Vector3 inputDirection;
        [SerializeField] [Range(0, 1)] private float walkSpeed = 0.2f;
        [SerializeField] [Range(0, 1)] private float runSpeed = 0.4f;
        private const float BaseSpeed = 10f;

        [SerializeField] private float jumpHeight = 1f;
    
        private Vector3 velocity;
        private const float Gravity = -9.81f;
    
        [Header("Aim")]
        [SerializeField] [Range(0, 1)] private float sensitivity = 0.2f;
        private const float AimSensitivityMultiplier = 10f;
        private const float AimAngleIncrements = 180f;
    
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] [Range(0, 1)] private float animationLerpTime = 0.1f;
        
        private static readonly int XVelocityHash = Animator.StringToHash("X Velocity");
        private static readonly int YVelocityHash = Animator.StringToHash("Y Velocity");

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            // Initialize rotation variables to match the current scene placement
            // to prevent the camera from "snapping" on the first mouse movement.
            x = transform.localEulerAngles.y;
            y = cameraAnchor.localEulerAngles.x;
        }

        public void SetInputActive(bool active)
        {
            this.enabled = active;

            if (active)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Update()
        {
            HandleMovement();
            HandleAim();
        }
    
        // Methods
        private void HandleMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveY = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f;
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
            inputDirection = new Vector3(moveX, 0f, moveY).normalized;

            float xTargetAnimValue = 0f;
            float currentSpeed = walkSpeed * BaseSpeed;
            if (isMoving)
            {
                if (isRunning && moveY > 0f) // If moveY is negative (moving backwards), return false
                {
                    xTargetAnimValue = moveY;
                    currentSpeed = runSpeed * BaseSpeed;
                }
                else
                {
                    xTargetAnimValue = moveY * 0.5f;
                    currentSpeed = walkSpeed * BaseSpeed;
                }
            }
        
            Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        
            // Gravity Logic
            if (controller.isGrounded)
            {
                velocity.y = -2f;
            }
        
            // Jump Logic
            if (Input.GetButtonDown("Jump") && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * Gravity);
            }
        
            // Apply Gravity
            velocity.y += Gravity * Time.deltaTime;
        
            // Apply Animations
            if (animator != null)
            {
                animator.SetFloat(XVelocityHash, xTargetAnimValue, animationLerpTime, Time.deltaTime);
            }
        
            // Apply Movement
            controller.Move(((move * currentSpeed) + velocity) * Time.deltaTime);
        }

        private void HandleAim()
        {
            // Only handle horizontal rotation for the player body.
            // Vertical rotation (pitch) should be handled by Cinemachine's Input Axis Controller.
            x += Input.GetAxis("Mouse X") * (sensitivity * AimSensitivityMultiplier * AimAngleIncrements * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, x , 0);
        }
    }
}
