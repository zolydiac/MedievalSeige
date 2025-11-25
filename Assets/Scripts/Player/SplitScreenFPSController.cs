using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SplitScreenFPSController : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private PlayerInput playerInput;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Movement Smoothing")]
    [Tooltip("How quickly horizontal movement changes while grounded (lower = snappier).")]
    [SerializeField] private float groundedSmoothTime = 0.08f;
    [Tooltip("How quickly horizontal movement changes in the air.")]
    [SerializeField] private float airSmoothTime = 0.2f;
    [Tooltip("Air control factor (0-1). Lower = less control in air.")]
    [SerializeField] private float airControl = 0.3f;

    [Header("Gravity Multipliers")]
    [SerializeField] private float gravityMultiplierFalling = 2f;
    [SerializeField] private float gravityMultiplierRising = 1f;
    [SerializeField] private float gravityMultiplierJumpCancel = 2.5f;

    [Header("Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float gamepadSensitivity = 100f;
    [SerializeField] private float lookXLimit = 90f;
    [SerializeField] private float lookSmoothing = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("View Mode")]
    [Tooltip("Start in third person instead of first person.")]
    [SerializeField] private bool startInThirdPerson = true;
    [Tooltip("Local camera position relative to player in first-person mode.")]
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.6f, 0f);
    [Tooltip("Distance of third-person camera behind the player.")]
    [SerializeField] private float thirdPersonDistance = 4f;
    [Tooltip("Height of third-person camera pivot above player origin.")]
    [SerializeField] private float thirdPersonHeight = 1.6f;
    [Tooltip("Side offset (shoulder view). Positive = right shoulder.")]
    [SerializeField] private float thirdPersonSideOffset = 0.6f;
    [Tooltip("Layers the camera collides with.")]
    [SerializeField] private LayerMask cameraCollisionMask;
    [Tooltip("Radius of the camera collision sphere.")]
    [SerializeField] private float cameraCollisionRadius = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Trigger name used by the jump animation (e.g. 'Jump').")]
    [SerializeField] private string jumpTriggerName = "Jump";

    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpRequested = false;
    private bool sprintPressed = false;
    private bool attackPressed = false;

    // Components
    private CharacterController controller;

    // Movement state
    private Vector3 velocity;               // vertical velocity stored in .y
    private Vector3 currentMoveVelocity;    // horizontal movement
    private bool isGrounded;

    // Look state
    private float cameraPitch;
    private float targetCameraPitch;

    // Jump timings
    [Header("Jump Timing")]
    [Tooltip("Minimum time between jumps (seconds). Tiny delay between jumps.")]
    [SerializeField] private float jumpCooldown = 0.2f;
    [Tooltip("Allow a short time to still jump after leaving ground.")]
    [SerializeField] private float coyoteTime = 0.12f;

    private float lastJumpTime = -999f;
    private float lastGroundedTime = 0f;

    // Input device
    private bool isUsingGamepad = false;

    // View mode
    private bool isThirdPerson;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            string mapName = playerNumber == 1 ? "Player1" : "Player2";
            playerInput.SwitchCurrentActionMap(mapName);

            if (playerNumber == 2 && Gamepad.current != null)
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);

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

        isThirdPerson = startInThirdPerson;

        if (cameraTransform != null && !isThirdPerson)
        {
            cameraTransform.localPosition = firstPersonOffset;
        }
    }

    void Update()
    {
        GroundCheck();
        HandleMovement();
        HandleLook();
        HandleJump();
        ApplyGravity();
        UpdateCameraPosition();
        UpdateAnimator();

        Vector3 finalMove = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);

        if (playerNumber == 1)
            HandleCursor();

        // one-frame inputs (jump is a "pressed this frame" style flag)
        jumpRequested = false;
        attackPressed = false;
    }

    // ─────────── INPUT ───────────

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
        if (pressed)
        {
            // latch the request – HandleJump() will consume it this frame
            jumpRequested = true;
        }
    }

    public void OnSprint(InputValue value)
    {
        sprintPressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
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

    public void OnToggleView(InputValue value)
    {
        if (value.isPressed)
        {
            isThirdPerson = !isThirdPerson;
        }
    }

    // ─────────── GROUND CHECK ───────────

    void GroundCheck()
    {
        if (controller == null) return;

        bool wasGrounded = isGrounded;

        // Use controller.isGrounded (reliable) + optional sphere check
        bool controllerGrounded = controller.isGrounded;
        bool sphereGrounded = false;

        if (groundCheck != null)
        {
            sphereGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }

        isGrounded = controllerGrounded || sphereGrounded;

        if (isGrounded)
        {
            // small downward force to keep controller stuck to ground
            if (velocity.y < 0f)
                velocity.y = -2f;

            lastGroundedTime = Time.time;

            // just landed → turn off jump bool
            if (!wasGrounded && animator != null)
            {
                animator.SetBool("IsJumping", false);
            }
        }
    }

    // ─────────── JUMP SYSTEM ───────────

    void HandleJump()
    {
        bool cooldownReady = Time.time - lastJumpTime >= jumpCooldown;
        bool groundedOrCoyote = isGrounded || (Time.time - lastGroundedTime <= coyoteTime);

        if (jumpRequested && groundedOrCoyote && cooldownReady)
        {
            // physics jump
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;

            // animation jump
            if (animator != null)
            {
                animator.SetBool("IsJumping", true);

                if (!string.IsNullOrEmpty(jumpTriggerName))
                {
                    animator.ResetTrigger(jumpTriggerName);  // avoid double-fire edge cases
                    animator.SetTrigger(jumpTriggerName);
                }
            }
        }
    }

    // ─────────── GRAVITY ───────────

    void ApplyGravity()
    {
        // Apply extra gravity multipliers depending on jump phase
        if (velocity.y < 0f)
        {
            // falling
            velocity.y += gravity * gravityMultiplierFalling * Time.deltaTime;
        }
        else if (velocity.y > 0f && !jumpRequested)
        {
            // rising but jump button is not held → short hop
            velocity.y += gravity * gravityMultiplierJumpCancel * Time.deltaTime;
        }
        else
        {
            // normal rising
            velocity.y += gravity * gravityMultiplierRising * Time.deltaTime;
        }
    }

    // ─────────── MOVEMENT ───────────

    void HandleMovement()
    {
        Vector3 desiredMove =
            transform.forward * moveInput.y +
            transform.right  * moveInput.x;

        float targetSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        if (desiredMove.sqrMagnitude > 1f)
            desiredMove.Normalize();

        Vector3 targetVelocity = desiredMove * targetSpeed;

        if (desiredMove.sqrMagnitude < 0.0001f)
            targetVelocity = Vector3.zero;

        float smoothTime = isGrounded ? groundedSmoothTime : airSmoothTime;
        float controlFactor = isGrounded ? 1f : airControl;
        float lerpFactor = Mathf.Clamp01(Time.deltaTime / Mathf.Max(smoothTime, 0.0001f)) * controlFactor;

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, lerpFactor);
    }

    // ─────────── LOOK ───────────

    void HandleLook()
    {
        if (cameraTransform == null) return;

        float sensitivity = isUsingGamepad ? gamepadSensitivity : mouseSensitivity;

        float yaw = lookInput.x * sensitivity * Time.deltaTime;
        float pitch = lookInput.y * sensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * yaw);

        targetCameraPitch -= pitch;
        targetCameraPitch = Mathf.Clamp(targetCameraPitch, -lookXLimit, lookXLimit);
        cameraPitch = Mathf.Lerp(cameraPitch, targetCameraPitch, lookSmoothing * Time.deltaTime);
    }

    // ─────────── CAMERA POSITION ───────────

    void UpdateCameraPosition()
    {
        if (cameraTransform == null) return;

        if (!isThirdPerson)
        {
            cameraTransform.localPosition = firstPersonOffset;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            return;
        }

        Vector3 pivot = transform.position + Vector3.up * thirdPersonHeight;
        Quaternion camRot = Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0f);

        Vector3 offset = camRot * new Vector3(thirdPersonSideOffset, 0f, -thirdPersonDistance);
        Vector3 desiredPos = pivot + offset;

        Vector3 dir = desiredPos - pivot;
        float dist = dir.magnitude;

        if (dist > 0.01f)
        {
            if (Physics.SphereCast(
                pivot,
                cameraCollisionRadius,
                dir.normalized,
                out RaycastHit hit,
                dist,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivot + dir.normalized * (hit.distance - 0.05f);
            }
        }

        cameraTransform.position = desiredPos;
        cameraTransform.rotation = camRot;
    }

    // ─────────── ANIMATION ───────────

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speedValue =
            moveInput.magnitude > 0.1f
                ? (sprintPressed ? 1f : 0.5f)
                : 0f;

        animator.SetFloat("Speed", speedValue);

        // Optional extra param if you want it in your Animator:
        // animator.SetBool("Grounded", isGrounded);
    }

    // ─────────── CURSOR ───────────

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) &&
            Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}
