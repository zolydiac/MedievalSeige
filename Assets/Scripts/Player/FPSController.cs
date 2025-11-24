using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
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
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookXLimit = 90f;
    [SerializeField] private float lookSmoothing = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Player 1 Keys (WASD)")]
    [SerializeField] private KeyCode keyForward = KeyCode.W;
    [SerializeField] private KeyCode keyBack = KeyCode.S;
    [SerializeField] private KeyCode keyLeft = KeyCode.A;
    [SerializeField] private KeyCode keyRight = KeyCode.D;
    [SerializeField] private KeyCode keyJump = KeyCode.Space;
    [SerializeField] private KeyCode keySprint = KeyCode.LeftShift;
    [SerializeField] private KeyCode keyCrouch = KeyCode.LeftControl;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private float cameraPitch = 0f;
    private float targetCameraPitch = 0f;

    // Jump Fix Variables
    private float lastGroundedTime = 0f;
    private float groundedGracePeriod = 0.15f;
    private float lastJumpTime = -999f;
    private float jumpCooldown = 0.2f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
        HandleMouseLook();
        HandleJump();
        ApplyGravity();

        Vector3 finalMove = currentMoveVelocity + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);

        HandleCursor();
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Lighter ground stick for smoother feel
            lastGroundedTime = Time.time;
        }
    }

    void HandleJump()
    {
        bool coyote = (Time.time - lastGroundedTime) <= groundedGracePeriod;
        bool cooldownReady = (Time.time - lastJumpTime) >= jumpCooldown;

        if (Input.GetKeyDown(keyJump) && coyote && cooldownReady)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }
    }

    void ApplyGravity()
    {
        if (velocity.y < 0)
            velocity.y += gravity * gravityMultiplierFalling * Time.deltaTime;
        else if (velocity.y > 0 && !Input.GetKey(keyJump))
            velocity.y += gravity * gravityMultiplierJumpCancel * Time.deltaTime;
        else
            velocity.y += gravity * gravityMultiplierRising * Time.deltaTime;
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(keyLeft)) horizontal = -1f;
        if (Input.GetKey(keyRight)) horizontal = 1f;
        if (Input.GetKey(keyForward)) vertical = 1f;
        if (Input.GetKey(keyBack)) vertical = -1f;

        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 targetMove = transform.right * inputDir.x + transform.forward * inputDir.z;

        float targetSpeed = walkSpeed;
        if (Input.GetKey(keySprint)) targetSpeed = sprintSpeed;
        if (Input.GetKey(keyCrouch)) targetSpeed = crouchSpeed;

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

    void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Smooth horizontal rotation
        transform.Rotate(Vector3.up * mx);

        // Smooth vertical look
        targetCameraPitch -= my;
        targetCameraPitch = Mathf.Clamp(targetCameraPitch, -lookXLimit, lookXLimit);
        cameraPitch = Mathf.Lerp(cameraPitch, targetCameraPitch, lookSmoothing * Time.deltaTime);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
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
}
