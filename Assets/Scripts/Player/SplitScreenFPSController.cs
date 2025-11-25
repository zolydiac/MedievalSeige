using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SplitScreenFPSController : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private int playerNumber = 1; // 1 or 2
    [SerializeField] private PlayerInput playerInput; // Drag your PlayerInput component here

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -35f;

    [Header("Gravity Multipliers")]
    [SerializeField] private float gravityMultiplierFalling = 3.5f;
    [SerializeField] private float gravityMultiplierRising = 1.0f;
    [SerializeField] private float gravityMultiplierJumpCancel = 2f;

    [Header("Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float gamepadSensitivity = 100f;
    [SerializeField] private float lookXLimit = 90f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Input Values
    private Vector2 moveInput;
    private Vector2 lookInput;

    private bool jumpPressed = false;
    private bool sprintPressed = false;
    private bool attackPressed = false;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch;

    // Jump Fix
    private float lastGroundedTime = 0f;
    private float groundedGracePeriod = 0.15f;
    private float lastJumpTime = -999f;
    private float jumpCooldown = 0.25f;

    // For tracking input device
    private bool isUsingGamepad = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Auto-find Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Auto-find PlayerInput if not assigned
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        // Detect control scheme
        if (playerInput != null)
        {
            string mapName = playerNumber == 1 ? "Player1" : "Player2";
            playerInput.SwitchCurrentActionMap(mapName);

            if (playerNumber == 2)
            {
                var gamepad = UnityEngine.InputSystem.Gamepad.current;
                if (gamepad != null)
                {
                    playerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
                }
                else
                {
                    Debug.LogError("No gamepad found!");
                }
            }

            playerInput.currentActionMap?.Enable();
            isUsingGamepad = playerInput.currentControlScheme == "Gamepad";
        }

        if (playerNumber == 1)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -1, 0);
            groundCheck = gc.transform;
        }
    }

    void Update()
    {
        GroundCheck();
        HandleMovement();
        HandleLook();
        HandleJump();
        ApplyGravity();
        UpdateAnimator();

        controller.Move(velocity * Time.deltaTime);

        if (playerNumber == 1)
        {
            HandleCursor();
        }
    }

    // INPUT CALLBACKS
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        // For gamepad, only register jump if the value is strong enough
        if (isUsingGamepad)
        {
            if (value.Get<float>() > 0.8f) // Add a threshold
                jumpPressed = true;
        }
        else
        {
            // For keyboard, just check if pressed
            if (value.isPressed)
                jumpPressed = true;
        }
    }

    public void OnSprint(InputValue value)
    {
        // Check the actual button state properly
        if (isUsingGamepad)
        {
            // For gamepad trigger/button
            sprintPressed = value.Get<float>() > 0.5f;
        }
        else
        {
            // For keyboard
            sprintPressed = value.isPressed;
        }

        Debug.Log("Sprint pressed: " + sprintPressed); // Temporary debug
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            attackPressed = true;
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }

    // MOVEMENT & PHYSICS
    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f;
            lastGroundedTime = Time.time;
        }
    }

    void HandleJump()
    {
        bool coyote = (Time.time - lastGroundedTime) <= groundedGracePeriod;
        bool cooldownReady = (Time.time - lastJumpTime) >= jumpCooldown;

        if (jumpPressed && coyote && cooldownReady)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
            jumpPressed = false; // consume jump
        }
    }

    void ApplyGravity()
    {
        if (velocity.y < 0)
            velocity.y += gravity * gravityMultiplierFalling * Time.deltaTime;
        else if (velocity.y > 0 && !jumpPressed)
            velocity.y += gravity * gravityMultiplierJumpCancel * Time.deltaTime;
        else
            velocity.y += gravity * gravityMultiplierRising * Time.deltaTime;
    }

    void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        float speed = sprintPressed ? sprintSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleLook()
    {
        float sensitivity = isUsingGamepad ? gamepadSensitivity : mouseSensitivity;

        float yaw = lookInput.x * sensitivity * Time.deltaTime;
        float pitch = lookInput.y * sensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * yaw);

        cameraPitch -= pitch;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // Calculate movement speed for animator (0 = idle, 0.5 = walk, 1 = run)
        float speed = 0f;

        if (moveInput.magnitude > 0.1f)
        {
            if (sprintPressed)
                speed = 1f; // Running
            else
                speed = 0.5f; // Walking
        }

        animator.SetFloat("Speed", speed);

        // Update jump state IMMEDIATELY when velocity changes
        animator.SetBool("IsJumping", velocity.y > 0.5f || !isGrounded);
    }


    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // DEBUG
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}