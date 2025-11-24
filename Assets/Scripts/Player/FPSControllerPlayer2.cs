using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSControllerPlayer2 : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.3f;

    [Header("Gravity Multipliers")]
    [SerializeField] private float gravityMultiplierFalling = 2f;
    [SerializeField] private float gravityMultiplierRising = 1.0f;
    [SerializeField] private float gravityMultiplierJumpCancel = 2.5f;

    [Header("Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float lookXLimit = 90f;
    [SerializeField] private float lookSmoothing = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    // Input Values
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintPressed;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private float cameraPitch;
    private float targetCameraPitch;

    // Jump Fix
    private float lastGroundedTime = 0f;
    private float groundedGracePeriod = 0.15f;
    private float lastJumpTime = -999f;
    private float jumpCooldown = 0.2f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

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

        Vector3 finalMove = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }

    // ---------- Input System Callbacks ----------
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value) => jumpPressed = value.isPressed;
    public void OnSprint(InputValue value) => sprintPressed = value.isPressed;

    // ---------- Logic ----------
    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
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
        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 targetMove = transform.right * inputDir.x + transform.forward * inputDir.z;

        float targetSpeed = walkSpeed;
        if (sprintPressed) targetSpeed = sprintSpeed;

        Vector3 targetVelocity = targetMove * targetSpeed;

        // Smooth acceleration/deceleration
        float accelRate = (inputDir.magnitude > 0) ? acceleration : deceleration;
        
        // Reduce control in air
        if (!isGrounded)
        {
            accelRate *= airControl;
        }

        currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, accelRate * Time.deltaTime);
    }

    void HandleLook()
    {
        float yaw = lookInput.x * lookSensitivity;
        float pitch = lookInput.y * lookSensitivity;

        // Smooth horizontal rotation
        transform.Rotate(Vector3.up * yaw);

        // Smooth vertical look
        targetCameraPitch -= pitch;
        targetCameraPitch = Mathf.Clamp(targetCameraPitch, -lookXLimit, lookXLimit);
        cameraPitch = Mathf.Lerp(cameraPitch, targetCameraPitch, lookSmoothing * Time.deltaTime);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}