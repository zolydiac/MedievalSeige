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
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    private float originalHeight;
    private float originalCenterY;

    [Header("Movement Smoothing")]
    [SerializeField] private float groundedSmoothTime = 0.08f;
    [SerializeField] private float airSmoothTime = 0.2f;
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
    [SerializeField] private bool startInThirdPerson = true;
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float thirdPersonDistance = 4f;
    [SerializeField] private float thirdPersonHeight = 1.6f;
    [SerializeField] private float thirdPersonSideOffset = 0.6f;
    [SerializeField] private LayerMask cameraCollisionMask;
    [SerializeField] private float cameraCollisionRadius = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration = 0.6f;
    [SerializeField] private bool canMoveWhileAttacking = false;

    [Header("Shield / Block Settings")]
    [SerializeField] private Transform leftHand;      // assign LeftHand_Socket in inspector
    [SerializeField] private GameObject shieldPrefab;       // assign Shield prefab
    [SerializeField] private float blockMoveSpeedMultiplier = 0.4f;
    [SerializeField] private bool canBlockWhileAttacking = false;

    private bool isBlocking = false;


    // Input
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpRequested = false;
    private bool sprintPressed = false;
    private bool crouchPressed = false;
    private bool attackPressed = false;

    // Attack state
    private bool isAttacking = false;

    // Components
    private CharacterController controller;

    // Movement state
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool wasGrounded;

    // Look state
    private float cameraPitch;
    private float targetCameraPitch;

    [Header("Jump Timing")]
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float coyoteTime = 0.12f;

    private float lastJumpTime = -999f;
    private float lastGroundedTime = 0f;

    private float currentAnimSpeed = 0f;
    [Header("Animation Smoothing")]
    [SerializeField] private float animSpeedSmoothTime = 0.15f;

    private bool isUsingGamepad = false;
    private bool isThirdPerson;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenterY = controller.center.y;

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
        wasGrounded = isGrounded;

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

        jumpRequested = false;
        attackPressed = false;
    }

    // INPUT
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
        if (pressed)
            jumpRequested = true;
    }

    public void OnSprint(InputValue value)
    {
        sprintPressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
    }

    public void OnCrouch(InputValue value)
    {
        crouchPressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
    }

    // ATTACK
        public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking)
        {
            // Optional: drop block when attacking
            isBlocking = false;
            if (animator != null)
                animator.SetBool("IsBlocking", false);

            isAttacking = true;
            attackPressed = true;

            if (animator != null)
                animator.SetTrigger("Attack");

            float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
            Invoke(nameof(EndAttack), animLength);
        }
    }


    private void EndAttack()
    {
        isAttacking = false;
    }

        public void OnBlock(InputValue value)
    {
        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;

        // Optional: prevent blocking while attacking unless allowed
        if (isAttacking && !canBlockWhileAttacking)
            pressed = false;

        isBlocking = pressed;

        ShieldController shield = GetComponentInChildren<ShieldController>();
        if (shield != null)
            shield.SetBlocking(isBlocking);


        if (animator != null)
            animator.SetBool("IsBlocking", isBlocking);
    }


    public void OnToggleView(InputValue value)
    {
        if (value.isPressed)
            isThirdPerson = !isThirdPerson;
    }

    // GROUND CHECK
    void GroundCheck()
    {
        bool controllerGrounded = controller.isGrounded;
        bool sphereGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        isGrounded = controllerGrounded || sphereGrounded;

        if (isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            lastGroundedTime = Time.time;

            if (!wasGrounded && animator != null)
            {
                animator.SetBool("IsJumping", false);
            }
        }
    }

    // JUMP
    void HandleJump()
    {
        bool cooldownReady = Time.time - lastJumpTime >= jumpCooldown;
        bool groundedOrCoyote = isGrounded || (Time.time - lastGroundedTime <= coyoteTime);

        if (jumpRequested && groundedOrCoyote && cooldownReady)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;

            if (animator != null)
                animator.SetBool("IsJumping", true);
        }
    }

    // GRAVITY
    void ApplyGravity()
    {
        if (velocity.y < 0)
            velocity.y += gravity * gravityMultiplierFalling * Time.deltaTime;
        else if (velocity.y > 0 && !jumpRequested)
            velocity.y += gravity * gravityMultiplierJumpCancel * Time.deltaTime;
        else
            velocity.y += gravity * gravityMultiplierRising * Time.deltaTime;
    }

    // MOVEMENT
    void HandleMovement()
    {
        // BLOCK MOVEMENT DURING ATTACK
        if (isAttacking && !canMoveWhileAttacking)
        {
            float stopSmooth = isGrounded ? groundedSmoothTime : airSmoothTime;
            float stopLerp = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, stopSmooth));
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, stopLerp);
            return;
        }

        Vector3 desiredMove =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        float targetSpeed;

        if (crouchPressed)
            targetSpeed = crouchSpeed;
        else
            targetSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        // Reduce movement speed while blocking
        if (isBlocking)
            targetSpeed *= blockMoveSpeedMultiplier;


        if (desiredMove.sqrMagnitude > 1f)
            desiredMove.Normalize();

        Vector3 targetVelocity = desiredMove * targetSpeed;

        if (desiredMove.sqrMagnitude < 0.0001f)
            targetVelocity = Vector3.zero;

        float smoothTime = isGrounded ? groundedSmoothTime : airSmoothTime;
        float controlFactor = isGrounded ? 1f : airControl;

        float lerpFactor = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, smoothTime)) * controlFactor;

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, lerpFactor);

        // Adjust height for crouching
        float targetHeight = crouchPressed ? crouchHeight : originalHeight;
        float heightDifference = originalHeight - targetHeight;
        float targetCenterY = originalCenterY - (heightDifference * 0.5f);

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);

        Vector3 newCenter = controller.center;
        newCenter.y = Mathf.Lerp(controller.center.y, targetCenterY, Time.deltaTime * 10f);
        controller.center = newCenter;
    }

    // LOOK
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

    // CAMERA POSITION
    void UpdateCameraPosition()
    {
        if (cameraTransform == null) return;

        if (!isThirdPerson)
        {
            cameraTransform.localPosition = firstPersonOffset;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
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
                cameraCollisionMask))
            {
                desiredPos = pivot + dir.normalized * (hit.distance - 0.05f);
            }
        }

        cameraTransform.position = desiredPos;
        cameraTransform.rotation = camRot;
    }

    // ANIMATOR
    void UpdateAnimator()
    {
        if (animator == null) return;

        float horizontalSpeed = new Vector2(currentMoveVelocity.x, currentMoveVelocity.z).magnitude;

        Vector3 localVelocity = transform.InverseTransformDirection(currentMoveVelocity);
        bool movingBackward = localVelocity.z < -0.1f;

        float targetAnimSpeed;

        if (!isGrounded)
        {
            targetAnimSpeed = currentAnimSpeed;
        }
        else
        {
            if (horizontalSpeed > 0.1f)
            {
                float speedValue = sprintPressed ? 1f : 0.5f;
                targetAnimSpeed = movingBackward ? -speedValue : speedValue;
            }
            else
            {
                targetAnimSpeed = 0f;
            }
        }

        currentAnimSpeed = Mathf.Lerp(
            currentAnimSpeed,
            targetAnimSpeed,
            Time.deltaTime / Mathf.Max(animSpeedSmoothTime, 0.0001f));

        animator.SetFloat("Speed", currentAnimSpeed);
        animator.SetBool("IsCrouching", crouchPressed);
    }

    // CURSOR
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }
}
