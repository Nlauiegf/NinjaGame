using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// Base class for all player states
public abstract class PlayerBaseState
{
    protected PlayerStateMachine stateMachine;

    public PlayerBaseState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public abstract void Enter();
    public abstract void Tick(float deltaTime);
    public abstract void Exit();
}

// The main state machine component
public class PlayerStateMachine : MonoBehaviour
{
    private bool wasGroundedLastFrame = true;

    // --- Coyote time (grounded grace period) ---
    private float jumpGroundedGraceTimer = 0f;
    private const float jumpGroundedGraceDuration = 0.10f; // 0.1 seconds of grace after jumping
    [field: SerializeField] public float MoveSpeed { get; private set; } = 5f; // Example speed
    [field: SerializeField] public float WallJumpForce { get; private set; } = 7.5f;
    [field: SerializeField] public int MaxJumps { get; private set; } = 2; // 1 = no double jump, 2 = double jump
    public int JumpsRemaining { get; set; }
    public bool CanDash { get; set; } = false;

    // --- Charge Shot ---
    [Header("Charge Shot Settings")]
    public GameObject projectilePrefab;
    public float chargeTime = 2f;
    private float currentCharge = 0f;
    private bool isCharging = false;
    private Camera mainCamera;
    private GameObject chargeUI;
    private Image chargeFillImage;

    [Header("Collider Settings")]
    [SerializeField] private CapsuleCollider2D playerCollider; // Assign in Inspector
    [SerializeField] private Vector2 standingColliderSize = new Vector2(1f, 2f); // Example
    [SerializeField] private Vector2 standingColliderOffset = new Vector2(0f, 0f); // Example
    [SerializeField] private Vector2 crouchingColliderSize = new Vector2(1f, 1f); // Example
    [SerializeField] private Vector2 crouchingColliderOffset = new Vector2(0f, -0.5f); // Example
    [SerializeField] private float standUpCheckDistance = 0.1f; // Distance above collider to check
    [SerializeField] private LayerMask groundLayer; // Assign layers considered ground/obstacles

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint; // Assign an empty GameObject childed to the player at their feet
    [SerializeField] private float groundCheckRadius = 0.2f; // Adjust radius as needed

    [Header("Crouch Settings")]
    [SerializeField] public float CrouchSpeedMultiplier { get; private set; } = 0.25f; // Half of WalkState's 0.5 multiplier


    private PlayerBaseState currentState;

    // State registry for extensibility
    private Dictionary<string, PlayerBaseState> stateRegistry = new Dictionary<string, PlayerBaseState>();

    // Concrete states
    public PlayerIdleState IdleState { get; private set; } // Add IdleState property back

    public WalkState WalkState { get; private set; }
    public RunState RunState { get; private set; }
    public JumpState JumpState { get; private set; }
    public CrouchState CrouchState { get; private set; }
    public SlideState SlideState { get; private set; }
    public WallClingState WallClingState { get; private set; }
    public ShootState ShootState { get; private set; } // Add ShootState declaration
    public FallState FallState { get; private set; } // Add FallState declaration

    // Component References (Example)
    public Rigidbody2D RB { get; private set; }
    public Animator Animator { get; private set; }
    // Add InputReader reference if using one

    // State transition event
    public delegate void StateChangedEvent(PlayerBaseState fromState, PlayerBaseState toState);
    public event StateChangedEvent OnStateChanged;

    // State duration tracking
    private float stateEnterTime;
    public float GetStateDuration()
    {
        return Time.time - stateEnterTime;
    }

    // InputReader abstraction (now a separate class)
    public InputReader InputReader { get; private set; } // Public property for states to access

    private void Awake()
    {
        // Get Components
        RB = GetComponent<Rigidbody2D>();
        Animator = GetComponentInChildren<Animator>(); // Or GetComponent<Animator>()
        if (playerCollider == null)
        {
            playerCollider = GetComponent<CapsuleCollider2D>();
            if (playerCollider != null)
            {
                // Store initial size/offset if not set via Inspector
                if (standingColliderSize == Vector2.zero) standingColliderSize = playerCollider.size;
                if (standingColliderOffset == Vector2.zero && playerCollider.offset != Vector2.zero) standingColliderOffset = playerCollider.offset;
            }
            else
            {
                Debug.LogError("Player Collider not found or assigned!", this);
            }
        }

    
        // Initialize input reader
        InputReader = new InputReader(); // Instantiate the new InputReader class

        // Initialize concrete states
        IdleState = new PlayerIdleState(this); // Instantiate the new PlayerIdleState class
        // MoveState removed
        WalkState = new WalkState(this);
        RunState = new RunState(this);
    
        // Register states
        stateRegistry[nameof(PlayerIdleState)] = IdleState; // Register the new PlayerIdleState
        // MoveState registration removed
        stateRegistry[nameof(WalkState)] = WalkState;
        stateRegistry[nameof(RunState)] = RunState;
        JumpState = new JumpState(this);
        stateRegistry[nameof(JumpState)] = JumpState;
        CrouchState = new CrouchState(this);
        stateRegistry[nameof(CrouchState)] = CrouchState;
        SlideState = new SlideState(this);
        // ... register other states
        WallClingState = new WallClingState(this);
        stateRegistry[nameof(WallClingState)] = WallClingState;
        stateRegistry[nameof(SlideState)] = SlideState;
        ShootState = new ShootState(this); // Initialize ShootState
        stateRegistry[nameof(ShootState)] = ShootState; // Register ShootState
        FallState = new FallState(this); // Initialize FallState
        stateRegistry[nameof(FallState)] = FallState; // Register FallState

        // Initialize jumps
        JumpsRemaining = MaxJumps;

        mainCamera = Camera.main;
        // Create charge meter UI
        CreateChargeMeterUI();
    }

    private void Start()
    {
        // Set the initial state
        SwitchState(IdleState); // Start in Idle state
        JumpsRemaining = MaxJumps;
    }

    private void Update()
    {
        // Update coyote time timer
        if (jumpGroundedGraceTimer > 0f)
            jumpGroundedGraceTimer -= Time.deltaTime;

        // Track grounded state for jump reset logic
        bool isGroundedNow = IsGrounded();
        if (!wasGroundedLastFrame && isGroundedNow)
        {
            // Landed this frame, reset jumps
            JumpsRemaining = Mathf.Max(0, MaxJumps - 1);
        }
        wasGroundedLastFrame = isGroundedNow;

        currentState?.Tick(Time.deltaTime);

        // Charge meter logic
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            currentCharge = 0f;
        }
        if (isCharging && Input.GetMouseButton(0))
        {
            currentCharge += Time.deltaTime;
            if (currentCharge > chargeTime)
                currentCharge = chargeTime;
        }
        if (isCharging && Input.GetMouseButtonUp(0))
        {
            if (currentCharge >= chargeTime)
            {
                ShootProjectileAtCursor();
            }
            isCharging = false;
            currentCharge = 0f;
        }

        // Update charge UI
        if (chargeUI != null)
        {
            chargeUI.SetActive(isCharging);
            if (isCharging && chargeFillImage != null)
                chargeFillImage.fillAmount = Mathf.Clamp01(currentCharge / chargeTime);
        }
    }

    public void SwitchState(PlayerBaseState newState)
    {
        if (currentState == newState) return; // Re-entrancy guard
        var prevState = currentState;
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
        OnStateChanged?.Invoke(prevState, newState);
        stateEnterTime = Time.time;
    }

    public Vector2 GetMovementInput()
    {
        // Delegate to the InputReader instance
        return InputReader.GetMovementInput();
    }

    public bool IsRunPressed()
    {
        // Delegate to the InputReader instance
        return InputReader.IsRunPressed();
    }

    // For extensibility: get state by name
    public PlayerBaseState GetState(string stateName)
    {
        if (stateRegistry.TryGetValue(stateName, out var state))
            return state;
        return null;
    }
    // Robust ground check using OverlapCircle
    public bool IsGrounded()
    {
        // During coyote time after jump, always return false
        if (jumpGroundedGraceTimer > 0f)
            return false;

        if (groundCheckPoint == null)
        {
             Debug.LogError("Ground Check Point not assigned in the Inspector!", this);
             return false; // Cannot check without the point
        }
        // Check if the circle overlaps with anything on the ground layer
        bool grounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        // Warn if grounded while moving up (likely ground check is inside collider)
        if (grounded && RB != null && RB.linearVelocity.y > 0.1f)
        {
            Debug.LogWarning("[PlayerStateMachine] IsGrounded() is true while moving upward. Adjust groundCheckPoint position or groundCheckRadius in the Inspector so it is just below the feet and not inside the collider.");
        }
        return grounded;
    }

    // Simple wall check (replace with your own logic)
    public bool IsTouchingWall()
    {
        // Wall detection using 2D raycast
        float wallCheckDistance = 0.1f; // How far to check
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left; // Check based on facing direction
        RaycastHit2D hit = Physics2D.Raycast(playerCollider.bounds.center, direction, playerCollider.bounds.extents.x + wallCheckDistance, groundLayer);
        Debug.DrawRay(playerCollider.bounds.center, direction * (playerCollider.bounds.extents.x + wallCheckDistance), hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    public void SetColliderCrouching()
    {
        if (playerCollider == null) return;
        playerCollider.size = crouchingColliderSize;
        playerCollider.offset = crouchingColliderOffset;
    }

    public void SetColliderStanding()
    {
        if (playerCollider == null) return;
        playerCollider.size = standingColliderSize;
        playerCollider.offset = standingColliderOffset;
    }

    public bool CanStandUp()
    {
        if (playerCollider == null) return true; // Cannot check, assume okay

        // Calculate the top position of the standing collider
        Vector2 standingTopPoint = (Vector2)transform.position + standingColliderOffset + Vector2.up * (standingColliderSize.y / 2f);
        // Calculate the size of the check area (slightly larger than the top part of the standing collider)
        Vector2 checkSize = new Vector2(standingColliderSize.x * 0.9f, standUpCheckDistance); // Check slightly narrower
        // Calculate the center of the check area
        Vector2 checkCenter = standingTopPoint + Vector2.up * (standUpCheckDistance / 2f);

        // Perform an overlap check
        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, groundLayer);

        return hit == null; // Can stand up if nothing is hit
    }

    private void ShootProjectileAtCursor()
    {
        if (projectilePrefab == null) {
            Debug.LogWarning("projectilePrefab is not assigned on PlayerStateMachine!");
            return;
        }
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float projectileSpeed = 10f;
            rb.linearVelocity = direction * projectileSpeed;
        }
    }

    private void CreateChargeMeterUI()
    {
        // Only create if not already present
        if (GameObject.Find("ChargeMeterUI") != null) return;
        // Create Canvas
        GameObject canvasGO = new GameObject("ChargeMeterUI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        // Create background
        GameObject bgGO = new GameObject("ChargeMeterBG");
        bgGO.transform.SetParent(canvasGO.transform);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0,0,0,0.5f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0, -20);
        bgRect.sizeDelta = new Vector2(200, 20);
        // Create fill
        GameObject fillGO = new GameObject("ChargeMeterFill");
        fillGO.transform.SetParent(bgGO.transform);
        chargeFillImage = fillGO.AddComponent<Image>();
        chargeFillImage.color = Color.cyan;
        chargeFillImage.type = Image.Type.Filled;
        chargeFillImage.fillMethod = Image.FillMethod.Horizontal;
        chargeFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        chargeFillImage.fillAmount = 0f;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0,0);
        fillRect.anchorMax = new Vector2(1,1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        chargeUI = canvasGO;
        chargeUI.SetActive(false);
    }

}