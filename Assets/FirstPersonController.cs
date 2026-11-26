using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person controller using the new Input System, with physics-material-based surface friction (ice!).
/// WASD / left stick = move, mouse / right stick = look,
/// Left Shift / left stick press = sprint, Space / gamepad south = jump, Esc = release cursor.
/// Attach to the player object (with a CharacterController) and make the camera a child of it.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera (child of this object). Falls back to Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float acceleration = 12f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;    // degrees per pixel of mouse movement
    [SerializeField] private float gamepadSensitivity = 150f;  // degrees per second at full stick
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Surface Friction")]
    [Tooltip("Friction value treated as 'normal grip'. Ground with a physics material at or above this gives full control.")]
    [SerializeField] private float normalFriction = 0.6f;
    [Tooltip("Fraction of control kept while airborne (1 = same as ground).")]
    [Range(0f, 1f)]
    [SerializeField] private float airControl = 0.5f;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;
    private float surfaceFriction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        surfaceFriction = normalFriction;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Move: WASD + left stick
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Look: mouse delta + right stick
        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        // Sprint (hold)
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        // Jump
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
        SetCursorLocked(false);
    }

    private void Update()
    {
        // Esc frees the cursor, clicking the game view locks it again
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLocked(true);

        if (Cursor.lockState == CursorLockMode.Locked)
            HandleLook();

        HandleMovement();
    }

    private void HandleLook()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();

        // Mouse delta is already per-frame, so it isn't scaled by deltaTime.
        // Gamepad stick is a rate, so it is.
        bool usingMouse = lookAction.activeControl != null && lookAction.activeControl.device is Mouse;
        Vector2 degrees = usingMouse
            ? look * mouseSensitivity
            : look * gamepadSensitivity * Time.deltaTime;

        // Yaw rotates the whole body, pitch only tilts the camera
        transform.Rotate(Vector3.up * degrees.x);

        pitch = Mathf.Clamp(pitch - degrees.y, -maxLookAngle, maxLookAngle);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        bool wantsSprint = sprintAction.IsPressed() && input.y > 0.1f; // sprint only when moving forward
        float targetSpeed = wantsSprint ? sprintSpeed : walkSpeed;

        // Relative to where the player is facing
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        Vector3 targetVelocity = moveDir * targetSpeed;

        // Grip: 1 on normal ground, close to 0 on ice. Applies to both speeding up and slowing down.
        float grip = Mathf.Clamp01(surfaceFriction / normalFriction);
        if (!controller.isGrounded)
            grip = Mathf.Max(grip, 0f) * airControl;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * grip * Time.deltaTime * Mathf.Max(walkSpeed, sprintSpeed));

        // Gravity & jump
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small downward force keeps the controller grounded

        if (jumpAction.WasPressedThisFrame() && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    // CharacterController ignores physics materials, so we read the floor's material ourselves.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.5f) return; // only floors, not walls or ceilings

        var mat = hit.collider.sharedMaterial;
        surfaceFriction = mat != null ? mat.dynamicFriction : normalFriction;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}