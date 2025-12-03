using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

/// <summary>
/// Single-player controller that supports:
/// - Human-controlled player (Player 1)
/// - AI-controlled player (Player 2, or bots)
///
/// This does NOT touch your SplitScreenFPSController.
/// Use this script in your singleplayer scenes only.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SinglePlayerFPSController : MonoBehaviour
{
    private enum WeaponType { SwordShield, Bow, Chalk }
    private enum AIState   { Idle, Patrol, Chase, Attack, Search }

    [Header("Player Setup")]
    [SerializeField] private bool isAIControlled = false;
    [SerializeField] private bool isAttacker = true;       // keep this if your RoundManager/Bomb logic uses it
    [SerializeField] private bool isDefender = false;
    [SerializeField] private int playerNumber = 1;         // 1 = human, 2 = AI (for your own logic)
    [SerializeField] private PlayerInput playerInput;

    public int PlayerNumber => playerNumber;
    public bool IsAttacker => isAttacker;

    [Header("AI Settings")]
    [SerializeField] private bool letAIChooseTarget = true;
    [SerializeField] private Transform aiTarget;
    [SerializeField] private float aiChaseRange = 25f;
    [SerializeField] private float aiMeleeRange = 2.5f;
    [SerializeField] private float aiShootRange = 12f;
    [SerializeField] private float aiDecisionInterval = 0.25f;

    [Header("AI NavMesh")]
    [SerializeField] private bool useNavMesh = true;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float sightRange = 20f;
    [SerializeField] private float sightAngle = 120f;
    [SerializeField] private float loseTargetTime = 3f;
    [SerializeField] private LayerMask sightMask = ~0;   // everything by default
    [SerializeField] private float aiAttackCooldown = 1.0f;

    [Header("AI Combat Behaviour")]
    [Tooltip("Preferred minimum distance when using bow.")]
    [SerializeField] private float bowPreferredMinDistance = 6f;
    [Tooltip("Preferred maximum distance when using bow.")]
    [SerializeField] private float bowPreferredMaxDistance = 10f;
    [Tooltip("How far the AI tries to circle around the target in melee.")]
    [SerializeField] private float meleeStrafeRadius = 2f;
    [Tooltip("How often (seconds) the AI changes its strafe direction.")]
    [SerializeField] private float strafeChangeInterval = 2f;
    [Tooltip("Chance per second to start blocking when in melee range.")]
    [SerializeField] private float blockChancePerSecond = 0.35f;
    [Tooltip("Random block duration range in seconds.")]
    [SerializeField] private Vector2 blockDurationRange = new Vector2(0.6f, 1.5f);
    [Tooltip("Random cooldown before AI is allowed to block again.")]
    [SerializeField] private Vector2 blockCooldownRange = new Vector2(2f, 4f);

    private float aiNextDecisionTime = 0f;
    private AIState currentAIState = AIState.Idle;
    private int currentPatrolIndex = 0;
    private float timeSinceLastSeenTarget = Mathf.Infinity;
    private Vector3 lastSeenTargetPosition;
    private float nextAllowedAttackTime = 0f;

    // Extra AI runtime state (for strafing / blocking)
    private int currentStrafeSign = 0;
    private float nextStrafeChangeTime = 0f;
    private float blockEndTime = 0f;
    private float nextBlockAllowedTime = 0f;


    [Header("UI References")]
    [SerializeField] private WeaponHotbarUI hotbarUI;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Movement Smoothing")]
    [SerializeField] private float groundedSmoothTime = 0.08f;
    [SerializeField] private float airSmoothTime = 0.2f;
    [SerializeField] private float airControl = 0.3f;

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
    [SerializeField] private float aimMoveSpeedMultiplier = 0.6f;

    [Header("Sword References")]
    [SerializeField] private GameObject swordInHandRoot;
    [SerializeField] private GameObject swordOnBackRoot;

    [Header("Shield References")]
    [SerializeField] private GameObject shieldInHandRoot;
    [SerializeField] private GameObject shieldOnBackRoot;

    [Header("Bow References")]
    [SerializeField] private GameObject bowInHandRoot;
    [SerializeField] private GameObject bowOnBackRoot;
    [SerializeField] private BowController bowController;         // hook up your existing bow script here

    [Header("Chalk / Bomb References")]
    [SerializeField] private GameObject chalkInHandRoot;
    [SerializeField] private GameObject chalkOnBeltRoot;
    // If you have a ChalkController, you can add it here

    [Header("Step / Landing")]
    [SerializeField] private float groundedThreshold = 0.15f;

    [Header("Jump Timing")]
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float coyoteTime = 0.12f;

    // Core components
    private CharacterController controller;

    // Movement state
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool wasGrounded;

    private float cameraPitch;
    private float targetCameraPitch;

    private float lastJumpTime = -999f;
    private float lastGroundedTime = 0f;

    // Animation smoothing
    [SerializeField] private float animSpeedSmoothTime = 0.15f;
    private float currentAnimSpeed = 0f;

    // Input state
    private bool isUsingGamepad = false;
    private bool isThirdPerson;
    private bool inputEnabled = true;

    private bool jumpRequested;
    private bool sprintPressed;
    private bool isCrouching;

    // Combat state
    private bool isAttacking = false;
    private bool isBlocking = false;
    private WeaponType currentWeapon = WeaponType.SwordShield;


    // ------------ UNITY LIFECYCLE ------------

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        isThirdPerson = startInThirdPerson;
    }

    private void Start()
    {
        // Input setup
        if (!isAIControlled)
        {
            if (playerInput != null)
            {
                if (!playerInput.enabled)
                    playerInput.enabled = true;

                // In singleplayer, we just use the Player1 action map by convention
                try
                {
                    playerInput.SwitchCurrentActionMap("Player1");
                    playerInput.currentActionMap?.Enable();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SinglePlayerFPSController] P{playerNumber} failed to switch action map 'Player1': {e.Message}");
                }

                // Simple heuristic: if the device is a gamepad, we treat it as such
                isUsingGamepad = Gamepad.current != null &&
                                 Keyboard.current == null &&
                                 Mouse.current == null;
            }
        }
        else
        {
            // AI does not use PlayerInput
            if (playerInput != null)
                playerInput.enabled = false;

            isUsingGamepad = false;

            if (useNavMesh)
            {
                if (agent == null)
                    agent = GetComponent<NavMeshAgent>();

                if (agent != null)
                {
                    // We move with CharacterController, so agent only gives us a path.
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                    agent.speed = sprintSpeed;
                    agent.angularSpeed = 720f;
                    agent.acceleration = 20f;
                }
            }
        }

        // Lock cursor for the human (if desired)
        if (!isAIControlled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Initial weapon setup
        EquipWeapon(WeaponType.SwordShield);
        UpdateWeaponHotbar();
    }

    private void Update()
    {
        wasGrounded = isGrounded;

        GroundCheck();

        if (isAIControlled)
        {
            UpdateAI();
        }

        HandleMovement();
        HandleLook();
        HandleJump();
        ApplyGravity();
        UpdateCameraPosition();
        UpdateAnimator();

        Vector3 finalMove = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);

        if (isAIControlled && useNavMesh && agent != null)
        {
            agent.nextPosition = transform.position;
        }

        jumpRequested = false;
    }

    // ------------ INPUT CALLBACKS (HUMAN) ------------

    public void OnMove(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (value.isPressed)
            jumpRequested = true;
    }

    public void OnSprint(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        sprintPressed = value.isPressed;
    }

    public void OnCrouch(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;

        isCrouching = !isCrouching;

        if (animator != null)
            animator.SetBool("IsCrouching", isCrouching);
    }

    public void OnAttack(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;

        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;
        HandleAttack(pressed);
    }

    public void OnBlock(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;

        bool pressed = isUsingGamepad ? value.Get<float>() > 0.5f : value.isPressed;

        if (currentWeapon != WeaponType.SwordShield) return;
        if (!canBlockWhileAttacking && isAttacking) return;

        isBlocking = pressed;

        if (animator != null)
            animator.SetBool("IsBlocking", isBlocking);
    }

    public void OnInteract(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;

        // Plug your existing bomb / objective / chalk logic in here
        // (copied from your SplitScreenFPSController as needed)
    }

    public void OnSelectSwordShield(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;
        EquipWeapon(WeaponType.SwordShield);
    }

    public void OnSelectBow(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;
        EquipWeapon(WeaponType.Bow);
    }

    public void OnSelectChalk(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;
        EquipWeapon(WeaponType.Chalk);
    }

    public void OnToggleView(InputValue value)
    {
        if (!inputEnabled || isAIControlled) return;
        if (!value.isPressed) return;

        isThirdPerson = !isThirdPerson;
    }

    // ------------ CORE MOVEMENT & LOOK ------------

    private void GroundCheck()
    {
        if (groundCheck == null)
        {
            isGrounded = controller.isGrounded;
            return;
        }

        bool wasGroundedLocal = isGrounded;

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (isGrounded && !wasGroundedLocal)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        // Restrict movement if attacking or blocking
        if (isAttacking && !canMoveWhileAttacking)
        {
            targetSpeed = 0f;
        }
        else if (isBlocking)
        {
            targetSpeed *= blockMoveSpeedMultiplier;
        }

        // TODO: if you use aiming for bow, you can reduce speed:
        // if (currentWeapon == WeaponType.Bow && bowController != null && bowController.IsAiming())
        //     targetSpeed *= aimMoveSpeedMultiplier;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Vector3 worldMove = transform.TransformDirection(inputDir) * targetSpeed;

        float smoothTime = isGrounded ? groundedSmoothTime : airSmoothTime;
        float control = isGrounded ? 1f : airControl;

        currentMoveVelocity = Vector3.Lerp(
            currentMoveVelocity,
            worldMove * control,
            Time.deltaTime / smoothTime);
    }

    private void HandleLook()
    {
        if (cameraTransform == null) return;

        float sensitivity = isUsingGamepad ? gamepadSensitivity : mouseSensitivity;

        // Horizontal look
        float yawDelta = lookInput.x * sensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * yawDelta);

        // Vertical look
        float pitchDelta = -lookInput.y * sensitivity * Time.deltaTime;
        targetCameraPitch = Mathf.Clamp(
            targetCameraPitch + pitchDelta,
            -lookXLimit,
            lookXLimit);

        cameraPitch = Mathf.Lerp(
            cameraPitch,
            targetCameraPitch,
            Time.deltaTime * lookSmoothing);

        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    private void HandleJump()
    {
        if (!jumpRequested) return;

        bool canJump =
            (Time.time >= lastJumpTime + jumpCooldown) &&
            (isGrounded || Time.time <= lastGroundedTime + coyoteTime);

        if (!canJump) return;

        lastJumpTime = Time.time;

        float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velocity.y = jumpVelocity;

        if (animator != null)
            animator.SetTrigger("Jump");
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private void UpdateCameraPosition()
    {
        if (cameraTransform == null) return;

        if (!isThirdPerson)
        {
            cameraTransform.localPosition = firstPersonOffset;
            return;
        }

        // Third person follow with collision
        Vector3 targetPivot = transform.position +
                              Vector3.up * thirdPersonHeight +
                              transform.right * thirdPersonSideOffset;

        Vector3 desiredPos = targetPivot - transform.forward * thirdPersonDistance;

        if (Physics.SphereCast(
                targetPivot,
                cameraCollisionRadius,
                (desiredPos - targetPivot).normalized,
                out RaycastHit hit,
                thirdPersonDistance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore))
        {
            cameraTransform.position = hit.point;
        }
        else
        {
            cameraTransform.position = desiredPos;
        }

        cameraTransform.LookAt(targetPivot);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 horizontalVelocity = new Vector3(currentMoveVelocity.x, 0f, currentMoveVelocity.z);
        float speed = horizontalVelocity.magnitude;

        currentAnimSpeed = Mathf.Lerp(
            currentAnimSpeed,
            speed,
            Time.deltaTime / animSpeedSmoothTime);

        animator.SetFloat("Speed", currentAnimSpeed);
        animator.SetBool("IsGrounded", isGrounded);
    }

    // ------------ COMBAT / WEAPONS ------------

    private void HandleAttack(bool pressed)
    {
        if (!inputEnabled) return;

        if (currentWeapon == WeaponType.SwordShield)
        {
            if (pressed && !isAttacking)
            {
                // If AI or player was blocking, drop the block to swing
                isBlocking = false;
                if (animator != null)
                    animator.SetBool("IsBlocking", false);

                isAttacking = true;

                if (animator != null)
                    animator.SetTrigger("Attack");

                // You can tune this to your actual animation length or use an Animation Event instead.
                float animLength = (animator != null)
                    ? animator.GetCurrentAnimatorStateInfo(0).length
                    : 0.6f;

                Invoke(nameof(EndAttack), animLength);
            }
        }
        else if (currentWeapon == WeaponType.Bow)
        {
            if (!pressed) return;
            if (bowController == null) return;

            // Assuming your bow controller has a similar method
            bowController.QuickShot();
        }
        // WeaponType.Chalk - usually uses Interact, not Attack
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    private void EquipWeapon(WeaponType weapon)
    {
        currentWeapon = weapon;

        if (swordInHandRoot != null) swordInHandRoot.SetActive(weapon == WeaponType.SwordShield);
        if (swordOnBackRoot != null) swordOnBackRoot.SetActive(weapon != WeaponType.SwordShield);

        if (shieldInHandRoot != null) shieldInHandRoot.SetActive(weapon == WeaponType.SwordShield);
        if (shieldOnBackRoot != null) shieldOnBackRoot.SetActive(weapon != WeaponType.SwordShield);

        if (bowInHandRoot != null) bowInHandRoot.SetActive(weapon == WeaponType.Bow);
        if (bowOnBackRoot != null) bowOnBackRoot.SetActive(weapon != WeaponType.Bow);

        if (chalkInHandRoot != null) chalkInHandRoot.SetActive(weapon == WeaponType.Chalk);
        if (chalkOnBeltRoot != null) chalkOnBeltRoot.SetActive(weapon != WeaponType.Chalk);

        if (bowController != null)
        {
        bowController.EquipBow(weapon == WeaponType.Bow);
        }

        UpdateWeaponHotbar();
    }

    private void UpdateWeaponHotbar()
    {
        if (hotbarUI == null) return;

        int index = 0;
        switch (currentWeapon)
        {
            case WeaponType.SwordShield: index = 0; break;
            case WeaponType.Bow: index = 1; break;
            case WeaponType.Chalk: index = 2; break;
        }

        // Uncomment if your hotbar UI expects this
        // hotbarUI.SetSelectedIndex(index);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            sprintPressed = false;
            jumpRequested = false;
        }
    }

    // ------------ AI LOGIC (NAVMESH + STATE MACHINE) ------------

    private void UpdateAI()
{
    if (!inputEnabled)
        return;

    // Ensure the agent is usable if we want NavMesh
    if (useNavMesh && agent != null && !agent.enabled)
    {
        agent.enabled = true;
    }

    // Make sure we have a target
    AcquireTargetIfNeeded();

    bool hasTarget = aiTarget != null;

    // Perception
    bool canSeeTarget = hasTarget && CanSeeTarget(aiTarget);
    float distanceToTarget = hasTarget
        ? Vector3.Distance(transform.position, aiTarget.position)
        : Mathf.Infinity;

    if (canSeeTarget)
    {
        timeSinceLastSeenTarget = 0f;
        lastSeenTargetPosition = aiTarget.position;
    }
    else
    {
        timeSinceLastSeenTarget += Time.deltaTime;
    }

    // ----------------- BOMB OBJECTIVE LOGIC -----------------
    // By default, AI runs toward the player.
    Vector3 navTargetPos = aiTarget != null ? aiTarget.position : transform.position;

    BombController activeBomb = null;
    bool bombIsActive = false;

    if (RoundManager.Instance != null)
    {
        activeBomb = RoundManager.Instance.ActiveBomb;
        bombIsActive = (activeBomb != null);
    }

    // If this AI is a defender and a bomb is active, override nav target to bomb site
    if (isAIControlled && isDefender && bombIsActive)
    {
        navTargetPos = activeBomb.transform.position;
    }
    // --------------------------------------------------------

    // Decide state at intervals
    if (Time.time >= aiNextDecisionTime)
    {
        aiNextDecisionTime = Time.time + aiDecisionInterval;
        UpdateAIState(canSeeTarget, distanceToTarget);
    }

    // Run state behaviour
    switch (currentAIState)
    {
        case AIState.Idle:
            HandleIdleState();
            break;
        case AIState.Patrol:
            HandlePatrolState();
            break;
        case AIState.Chase:
            HandleChaseState(distanceToTarget, navTargetPos, bombIsActive);
            break;
        case AIState.Attack:
            HandleAttackState(distanceToTarget, canSeeTarget);
            break;
        case AIState.Search:
            HandleSearchState(navTargetPos, bombIsActive);
            break;
    }


        // Use NavMeshAgent desired velocity to drive movement input
        if (useNavMesh && agent != null && agent.enabled)
        {
            Vector3 desired = agent.hasPath ? agent.desiredVelocity : Vector3.zero;

            if (desired.sqrMagnitude > 0.05f &&
                currentAIState != AIState.Attack) // in attack state we move manually
            {
                Vector3 local = transform.InverseTransformDirection(desired.normalized);
                moveInput = new Vector2(local.x, local.z);
                moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            }
            else if (currentAIState != AIState.Attack)
            {
                moveInput = Vector2.zero;
            }
        }
        else
{
    // Simple non-NavMesh fallback: move directly toward bomb or player
    if (currentAIState == AIState.Chase)
    {
        Vector3 toTarget = (navTargetPos - transform.position);
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.1f)
        {
            Vector3 local = transform.InverseTransformDirection(toTarget.normalized);
            moveInput = new Vector2(local.x, local.z);
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            sprintPressed = true;
        }
    }
    else if (currentAIState == AIState.Search)
    {
        Vector3 toLast = (navTargetPos - transform.position);
        toLast.y = 0f;
        if (toLast.sqrMagnitude > 0.1f)
        {
            Vector3 local = transform.InverseTransformDirection(toLast.normalized);
            moveInput = new Vector2(local.x, local.z);
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            sprintPressed = false;
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }
    else if (currentAIState != AIState.Attack)
    {
        moveInput = Vector2.zero;
    }
}


        // Aim at the target if we have one
        // For attack we want precise aiming, otherwise just face where we move.
        if (aiTarget != null && currentAIState == AIState.Attack)
        {
            // Use camera-based aiming only while actually attacking
            AimAtTarget();
        }
        else
        {
            // Don't drive rotation with lookInput outside of attack
            lookInput = Vector2.zero;

            // Face movement direction to avoid sideways sliding
            Vector3 horiz = new Vector3(currentMoveVelocity.x, 0f, currentMoveVelocity.z);
            if (horiz.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horiz.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }
        }


        // Handle shield block timing for AI (end block when time is up)
        if (isBlocking && Time.time >= blockEndTime)
        {
            StopBlockAI();
        }
    }

    /// <summary>
    /// If aiTarget is null and we are allowed, find the first non-AI player.
    /// </summary>
    private void AcquireTargetIfNeeded()
    {
        if (aiTarget != null || !letAIChooseTarget)
            return;

        SinglePlayerFPSController[] controllers = FindObjectsOfType<SinglePlayerFPSController>();
        foreach (var c in controllers)
        {
            if (!c.isAIControlled)
            {
                aiTarget = c.transform;
                break;
            }
        }
    }

    private bool CanSeeTarget(Transform target)
    {
        if (target == null)
            return false;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > sightRange)
            return false;

        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        Vector3 forwardXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        float angle = Vector3.Angle(forwardXZ, toTargetXZ);
        if (angle > sightAngle * 0.5f)
            return false;

        // Raycast for line-of-sight
        if (Physics.Raycast(transform.position + Vector3.up * 1.6f,
                            toTarget.normalized,
                            out RaycastHit hit,
                            sightRange,
                            sightMask,
                            QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return false;
    }

    private void UpdateAIState(bool canSeeTarget, float distanceToTarget)
    {
        switch (currentAIState)
        {
            case AIState.Idle:
            case AIState.Patrol:
                if (canSeeTarget && distanceToTarget <= aiChaseRange)
                {
                    // If in bow or melee range, go straight to attack
                    if (distanceToTarget <= aiShootRange)
                        currentAIState = AIState.Attack;
                    else
                        currentAIState = AIState.Chase;
                }
                else if (currentAIState == AIState.Idle && patrolPoints != null && patrolPoints.Length > 0)
                {
                    currentAIState = AIState.Patrol;
                }
                break;

            case AIState.Chase:
                if (!canSeeTarget && timeSinceLastSeenTarget > loseTargetTime)
                {
                    currentAIState = AIState.Search;
                }
                else if (canSeeTarget && distanceToTarget <= aiShootRange)
                {
                    // Enter attack when in ranged/melee zone, not just melee
                    currentAIState = AIState.Attack;
                }
                break;

            case AIState.Attack:
                if (!canSeeTarget && timeSinceLastSeenTarget > loseTargetTime)
                {
                    currentAIState = AIState.Search;
                }
                else if (distanceToTarget > aiChaseRange)
                {
                    // Lost them completely, go back to patrol / idle
                    if (patrolPoints != null && patrolPoints.Length > 0)
                        currentAIState = AIState.Patrol;
                    else
                        currentAIState = AIState.Idle;
                }
                else if (canSeeTarget && distanceToTarget > aiShootRange * 1.1f)
                {
                    // Out of bow range, chase again
                    currentAIState = AIState.Chase;
                }
                break;

            case AIState.Search:
                if (canSeeTarget)
                {
                    if (distanceToTarget <= aiShootRange)
                        currentAIState = AIState.Attack;
                    else
                        currentAIState = AIState.Chase;
                }
                else if (Vector3.Distance(transform.position, lastSeenTargetPosition) < 1.0f)
                {
                    if (patrolPoints != null && patrolPoints.Length > 0)
                        currentAIState = AIState.Patrol;
                    else
                        currentAIState = AIState.Idle;
                }
                break;
        }
    }

    private void HandleIdleState()
    {
        if (useNavMesh && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        moveInput = Vector2.zero;
        sprintPressed = false;
    }

    private void HandlePatrolState()
    {
        if (!useNavMesh || agent == null || patrolPoints == null || patrolPoints.Length == 0)
        {
            currentAIState = AIState.Idle;
            return;
        }

        agent.isStopped = false;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        agent.SetDestination(targetPoint.position);
        sprintPressed = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void HandleChaseState(float distanceToTarget, Vector3 navTargetPos, bool bombIsActive)
{
    if (!useNavMesh || agent == null || !agent.enabled)
    {
        currentAIState = AIState.Idle;
        return;
    }

    agent.isStopped = false;
    agent.SetDestination(navTargetPos);   // <-- bomb site OR player position
    sprintPressed = distanceToTarget > aiMeleeRange * 1.5f;

    // Pick weapon based on distance to the PLAYER (not the bomb)
    if (distanceToTarget <= aiMeleeRange * 1.2f)
    {
        EquipWeapon(WeaponType.SwordShield);
    }
    else if (distanceToTarget <= aiShootRange)
    {
        EquipWeapon(WeaponType.Bow);
    }
    else
    {
        EquipWeapon(WeaponType.SwordShield);
    }
}


    private void HandleAttackState(float distanceToTarget, bool canSeeTarget)
    {
        if (aiTarget == null)
        {
            currentAIState = AIState.Idle;
            return;
        }

        // Stop NavMesh motion while attacking and let animations / small adjustments handle the rest
        if (useNavMesh && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // We'll manually control movement during attack
        sprintPressed = false;

        // Decide melee vs bow in attack state
        bool withinMelee = distanceToTarget <= aiMeleeRange * 1.1f;
        bool withinBow = distanceToTarget <= aiShootRange && canSeeTarget;

        bool useMelee = withinMelee;
        bool useBow = !useMelee && withinBow;

        if (useMelee)
        {
            EquipWeapon(WeaponType.SwordShield);
        }
        else if (useBow)
        {
            EquipWeapon(WeaponType.Bow);
        }
        else
        {
            // Out of good attack range: let state machine push us back to chase
            moveInput = Vector2.zero;
            StopBlockAI();
            return;
        }

        // Rotate towards the target so we don't swing/shoot in random directions
        Vector3 toTarget = aiTarget.position - transform.position;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        if (toTargetXZ.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toTargetXZ.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        Vector3 desiredWorldMove = Vector3.zero;

// Just move toward/away from target, no strafing
Vector3 forwardToTarget = toTargetXZ.normalized;

if (useMelee)
{
    float desiredRadius = meleeStrafeRadius;
    float currentRadius = toTargetXZ.magnitude;

    // Move in or out to hover around desired melee radius
    if (currentRadius > desiredRadius + 0.3f)
        desiredWorldMove += forwardToTarget;      // step in
    else if (currentRadius < desiredRadius - 0.3f)
        desiredWorldMove -= forwardToTarget;      // step out

    TryStartBlockInMelee();
}
else if (useBow)
{
    // For now, play aggressive with bow:
    // always move toward the player so the AI actually chases them.
    desiredWorldMove += forwardToTarget;

    // (Optional) if you want them to stop once they're quite close, you can do:
    // if (distanceToTarget > aiMeleeRange * 1.2f)
    //     desiredWorldMove += forwardToTarget;
}



        // Convert desired world move to local moveInput
        if (desiredWorldMove.sqrMagnitude > 0.001f)
        {
            desiredWorldMove = desiredWorldMove.normalized;
            Vector3 localMove = transform.InverseTransformDirection(desiredWorldMove);
            moveInput = new Vector2(localMove.x, localMove.z);
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }
        else
        {
            moveInput = Vector2.zero;
        }

        // --- Attack timing ---
        float angleToTarget = Vector3.Angle(transform.forward, forwardToTarget);
        bool facingTarget = angleToTarget <= 25f;

        // Only actually perform an attack if:
        // - cooldown is ready
        // - we are facing the target
        // - for bow, we also require line of sight
        bool canAttackNow = Time.time >= nextAllowedAttackTime && facingTarget;

        if (canAttackNow)
        {
            if (currentWeapon == WeaponType.Bow && !canSeeTarget)
            {
                // Don't fire into walls
                return;
            }

            HandleAttack(true);
            nextAllowedAttackTime = Time.time + aiAttackCooldown;
        }
    }

    private void HandleSearchState(Vector3 navTargetPos, bool bombIsActive)
{
    if (!useNavMesh || agent == null || !agent.enabled)
    {
        // Fallback search is handled in UpdateAI when !useNavMesh
        sprintPressed = false;
        return;
    }

    agent.isStopped = false;

    // If we’re a defender and the bomb is active, always go to the bomb site.
    if (isDefender && bombIsActive && RoundManager.Instance != null && RoundManager.Instance.ActiveBomb != null)
    {
        agent.SetDestination(RoundManager.Instance.ActiveBomb.transform.position);
    }
    else
    {
        // Otherwise search around the last known / nav target position
        agent.SetDestination(navTargetPos);
    }

    sprintPressed = false;
}


    private void AimAtTarget()
    {
        if (cameraTransform == null || aiTarget == null)
            return;

        // Horizontal
        Vector3 toTarget = aiTarget.position - transform.position;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        if (toTargetXZ.sqrMagnitude < 0.001f)
            return;

        float targetYaw = Mathf.Atan2(toTargetXZ.x, toTargetXZ.z) * Mathf.Rad2Deg;
        float currentYaw = transform.eulerAngles.y;
        float yawError = Mathf.DeltaAngle(currentYaw, targetYaw);
        float yawInput = Mathf.Clamp(yawError / 45f, -1f, 1f);

        // Vertical
        Vector3 camToTarget = aiTarget.position + Vector3.up * 1.5f - cameraTransform.position;
        float targetPitch = -Mathf.Atan2(
            camToTarget.y,
            new Vector2(camToTarget.x, camToTarget.z).magnitude) * Mathf.Rad2Deg;
        float pitchError = Mathf.DeltaAngle(cameraPitch, targetPitch);
        float pitchInput = Mathf.Clamp(pitchError / 45f, -1f, 1f);

        // Smooth towards the desired look direction to avoid jitter
        Vector2 desiredLook = new Vector2(yawInput, pitchInput);
        lookInput = Vector2.Lerp(lookInput, desiredLook, 5f * Time.deltaTime);
    }

    // ------------ AI HELPER: BLOCKING ------------

    private void TryStartBlockInMelee()
    {
        if (currentWeapon != WeaponType.SwordShield)
            return;

        if (!canBlockWhileAttacking && isAttacking)
            return;

        if (Time.time < nextBlockAllowedTime)
            return;

        // Random chance per second
        if (Random.value < blockChancePerSecond * Time.deltaTime)
        {
            float duration = Random.Range(blockDurationRange.x, blockDurationRange.y);
            float cooldown = Random.Range(blockCooldownRange.x, blockCooldownRange.y);
            StartBlockAI(duration);
            nextBlockAllowedTime = Time.time + cooldown;
        }
    }

    private void StartBlockAI(float duration)
    {
        if (isBlocking)
            return;

        isBlocking = true;
        blockEndTime = Time.time + duration;

        if (animator != null)
            animator.SetBool("IsBlocking", true);
    }

    private void StopBlockAI()
    {
        if (!isBlocking)
            return;

        isBlocking = false;

        if (animator != null)
            animator.SetBool("IsBlocking", false);
    }

    // ------------ DEBUG ------------

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }

        if (aiTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, aiTarget.position + Vector3.up);
        }
    }
}





