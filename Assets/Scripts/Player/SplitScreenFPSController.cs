using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SplitScreenFPSController : MonoBehaviour
{
    private enum WeaponType { SwordShield, Bow }

    [Header("Player Setup")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private PlayerInput playerInput;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

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
    [SerializeField] private bool canMoveWhileAttacking = false;

    [Header("Shield / Block Settings")]
    [SerializeField] private float blockMoveSpeedMultiplier = 0.4f;
    [SerializeField] private bool canBlockWhileAttacking = false;

    [Header("Aiming / Bow Settings")]
    [SerializeField] private float aimMoveSpeedMultiplier = 0.6f; // speed while drawing bow

    [Header("Sword References")]
    [SerializeField] private GameObject swordInHandRoot;   // Sword in hand (child of hand bone)
    [SerializeField] private GameObject swordOnBackRoot;   // Sword on back (child of spine bone)

    [Header("Shield References")]
    [SerializeField] private GameObject shieldInHandRoot;  // Shield in hand
    [SerializeField] private GameObject shieldOnBackRoot;  // Shield on back

    [Header("Bow References")]
    [SerializeField] private GameObject bowInHandRoot;     // Bow in hand
    [SerializeField] private GameObject bowOnBackRoot;     // Bow on back

    // Input state
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpRequested = false;
    private bool sprintPressed = false;

    // Movement / controller
    private CharacterController controller;
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

    // Animation smoothing
    [SerializeField] private float animSpeedSmoothTime = 0.15f;
    private float currentAnimSpeed = 0f;

    private bool isUsingGamepad = false;
    private bool isThirdPerson;

    // Combat state
    private bool isAttacking = false;
    private bool isBlocking = false;
    private WeaponType currentWeapon = WeaponType.SwordShield;

    // Components
    private ShieldBlock shieldBlock;
    private BowController bowController;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

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
            cameraTransform.localPosition = firstPersonOffset;

        // Get weapon-related components
        shieldBlock = GetComponentInChildren<ShieldBlock>();
        bowController = GetComponentInChildren<BowController>();

        // Ensure we start in sword+shield mode
        EquipWeapon(WeaponType.SwordShield);
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

        // reset per-frame flags
        jumpRequested = false;
    }

    // ---------------- INPUT CALLBACKS ----------------

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

    // Keyboard weapon select
    public void OnSelectSword(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(WeaponType.SwordShield);
    }

    public void OnSelectBow(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(WeaponType.Bow);
    }

    // Gamepad weapon cycle (Y / Triangle)
    public void OnCycleWeapon(InputValue value)
    {
        if (!value.isPressed) return;

        WeaponType nextWeapon =
            (currentWeapon == WeaponType.SwordShield) ? WeaponType.Bow : WeaponType.SwordShield;

        EquipWeapon(nextWeapon);
    }

    // Attack:
    // - Sword mode: press = swing
    // - Bow mode: press = start draw, release = fire
    public void OnAttack(InputValue value)
    {
        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;

        // Sword & shield mode
        if (currentWeapon == WeaponType.SwordShield)
        {
            if (pressed && !isAttacking)
            {
                // Stop blocking if we were
                isBlocking = false;
                if (shieldBlock != null) shieldBlock.SetBlocking(false);
                if (animator != null) animator.SetBool("IsBlocking", false);

                isAttacking = true;

                if (animator != null)
                    animator.SetTrigger("Attack");

                float animLength = (animator != null)
                    ? animator.GetCurrentAnimatorStateInfo(0).length
                    : 0.6f;

                Invoke(nameof(EndAttack), animLength);
            }
        }
        else // Bow mode
        {
            if (!pressed) return;           // only react on press now
            if (bowController == null) return;
            if (!bowController.IsEquipped()) return;

            // Fire a quick shot on press
            bowController.QuickShot();
        }
    }



    private void EndAttack()
    {
        isAttacking = false;
    }

    // Block (only works in sword+shield mode)
    public void OnBlock(InputValue value)
    {
        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
        Debug.Log($"[OnBlock] called, isPressed={value.isPressed}, pressed={pressed}");

        // No blocking if bow is equipped
        if (currentWeapon == WeaponType.Bow)
            pressed = false;

        // Optionally, cannot start block mid-attack
        if (isAttacking && !canBlockWhileAttacking)
            pressed = false;

        isBlocking = pressed;

        Debug.Log($"[OnBlock] pressed={pressed}, isBlocking={isBlocking}");

        if (shieldBlock != null)
            shieldBlock.SetBlocking(isBlocking);

        if (animator != null)
        {
            animator.SetBool("IsBlocking", isBlocking);
            Debug.Log($"[OnBlock] Animator IsBlocking set to {isBlocking}");
        }
    }


    public void OnToggleView(InputValue value)
    {
        if (value.isPressed)
            isThirdPerson = !isThirdPerson;
    }

    // ---------------- GROUND / JUMP / GRAVITY ----------------

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
                animator.SetBool("IsJumping", false);
        }
    }

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

    void ApplyGravity()
    {
        if (velocity.y < 0)
            velocity.y += gravity * gravityMultiplierFalling * Time.deltaTime;
        else if (velocity.y > 0 && !jumpRequested)
            velocity.y += gravity * gravityMultiplierJumpCancel * Time.deltaTime;
        else
            velocity.y += gravity * gravityMultiplierRising * Time.deltaTime;
    }

    // ---------------- MOVEMENT / LOOK ----------------

    void HandleMovement()
    {
        // Cancel movement while sword attacking (if desired)
        if (isAttacking && !canMoveWhileAttacking)
        {
            float stopSmooth = isGrounded ? groundedSmoothTime : airSmoothTime;
            float stopLerp = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, stopSmooth));
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, Vector3.zero, stopLerp);
            return;
        }

        Vector3 desiredMove = transform.forward * moveInput.y + transform.right * moveInput.x;
        float targetSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        // Block slowdown
        if (isBlocking)
            targetSpeed *= blockMoveSpeedMultiplier;

        // Bow draw slowdown
        if (currentWeapon == WeaponType.Bow && bowController != null && bowController.IsDrawing())
            targetSpeed *= aimMoveSpeedMultiplier;

        if (desiredMove.sqrMagnitude > 1f)
            desiredMove.Normalize();

        Vector3 targetVelocity = desiredMove * targetSpeed;
        if (desiredMove.sqrMagnitude < 0.0001f)
            targetVelocity = Vector3.zero;

        float smoothTime = isGrounded ? groundedSmoothTime : airSmoothTime;
        float controlFactor = isGrounded ? 1f : airControl;
        float lerpFactor = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, smoothTime)) * controlFactor;

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, lerpFactor);
    }

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

    // ---------------- CAMERA ----------------

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

    // ---------------- ANIMATOR ----------------

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
    }

    // ---------------- UI / MISC ----------------

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

    // ---------------- WEAPON EQUIP LOGIC ----------------

    private void EquipWeapon(WeaponType weapon)
    {
        currentWeapon = weapon;

        bool swordMode = (weapon == WeaponType.SwordShield);
        bool bowMode = (weapon == WeaponType.Bow);

        // Sword visibility
        if (swordInHandRoot != null) swordInHandRoot.SetActive(swordMode);
        if (swordOnBackRoot != null) swordOnBackRoot.SetActive(!swordMode);

        // Shield visibility
        if (shieldInHandRoot != null) shieldInHandRoot.SetActive(swordMode);
        if (shieldOnBackRoot != null) shieldOnBackRoot.SetActive(!swordMode);

        // Bow visibility
        if (bowInHandRoot != null) bowInHandRoot.SetActive(bowMode);
        if (bowOnBackRoot != null) bowOnBackRoot.SetActive(!bowMode);

        // Shield behaviour
        if (shieldBlock != null)
        {
            if (swordMode)
            {
                shieldBlock.SetEquipped(true);
                shieldBlock.SetBlocking(false);
            }
            else
            {
                shieldBlock.SetBlocking(false);
                shieldBlock.SetEquipped(false);
            }
        }

        // Bow behaviour
        if (bowController != null)
            bowController.EquipBow(bowMode);

        // Animator flags
        if (animator != null)
        {
            animator.SetBool("HasBow", bowMode);
            animator.SetBool("IsBlocking", false);
            animator.SetBool("IsAiming", false);
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
