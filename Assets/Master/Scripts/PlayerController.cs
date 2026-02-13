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
        [SerializeField] private Vector2 sensitivity = new Vector2(0.2f, 0.2f);
        [SerializeField] [Tooltip("To avoid camera flipping. Default: 45")] private float yAimClamp = 45f;
        private const float AimSensitivityMultiplier = 10f;
        private const float AimAngleIncrements = 180f;
    
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] [Range(0, 1)] private float animationLerpTime = 0.1f;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
        
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
                // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
                animator.SetTrigger("Jump");
            }
        
            // Apply Gravity
            velocity.y += Gravity * Time.deltaTime;
        
            // Apply Animations
            if (animator != null)
            {
                // ReSharper disable once Unity.PreferAddressByIdToGraphicsParams
                animator.SetFloat("X Velocity", xTargetAnimValue, animationLerpTime, Time.deltaTime);
            }
        
            // Apply Movement
            controller.Move(((move * currentSpeed) + velocity) * Time.deltaTime);
        }

        private void HandleAim()
        {
            x += Input.GetAxis("Mouse X") * (sensitivity.x * AimSensitivityMultiplier * AimAngleIncrements * Time.deltaTime);
            y -= Input.GetAxis("Mouse Y") * (sensitivity.y * AimSensitivityMultiplier * AimAngleIncrements * Time.deltaTime);
            y = Mathf.Clamp(y, (yAimClamp * -1), yAimClamp); // Clamp Y axis rotation to avoid flipping
        
            transform.localRotation = Quaternion.Euler(0, x , 0);
            cameraAnchor.localRotation = Quaternion.Euler(y, 0 , 0);
        }
    }
}
