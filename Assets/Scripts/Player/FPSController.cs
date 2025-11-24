using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -35f;

    [Header("Gravity Multipliers")]
    [SerializeField] private float gravityMultiplierFalling = 3.5f;
    [SerializeField] private float gravityMultiplierRising = 1.0f;
    [SerializeField] private float gravityMultiplierJumpCancel = 2f;

    [Header("Look Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookXLimit = 90f;

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
    private bool isGrounded;
    private float cameraPitch = 0f;

    // Jump Fix Variables
    private float lastGroundedTime = 0f;
    private float groundedGracePeriod = 0.15f;
    private float lastJumpTime = -999f;
    private float jumpCooldown = 0.25f;

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

        controller.Move(velocity * Time.deltaTime);

        HandleCursor();
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -5f; // Hard stick to the ground
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

        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        float speed = walkSpeed;
        if (Input.GetKey(keySprint)) speed = sprintSpeed;
        if (Input.GetKey(keyCrouch)) speed = crouchSpeed;

        controller.Move(move * speed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);

        cameraPitch -= my;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);
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
