using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

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
    private int coinCount = 0;
    private Text coinText;
    private const string COIN_TEXT_FORMAT = "Coins: {0}";

    void OnCollisionEnter2D(Collision2D other)
    {
        // This is now handled by the Coin script's OnTriggerEnter2D
    }

    public void AddCoins(int amount)
    {
        coinCount += amount;
        UpdateCoinUI();
        Debug.Log($"Collected {amount} coin(s)! Total: {coinCount}");
    }

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
    public bool IsCharging => isCharging;
    public float CurrentCharge => currentCharge;
    public float ChargeTime => chargeTime;
    public void HandleChargeInput()
    {
        // Charge meter logic (only called from ShootState)
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
    }

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

    [Header("Shooting Settings")]
    public float projectileSpeed = 10f;
    public int projectileDamage = 1;
    public float shootCooldown = 0.5f; // Time between shots in seconds
    private float lastShootTime = 0f;

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

    private bool isDead = false;

    [Header("Dash Settings")]
    public float dashSpeed = 12f; // Match bull's dash speed
    public float dashDuration = 0.5f; // Match bull's dash duration
    public float dashCooldown = 3f; // Match bull's dash cooldown
    public float liftAmount = 0.5f; // How high to hop during dash
    private float lastDashTime = 0f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDirection;
    private float originalGravityScale;
    private Vector3 originalPosition;

    private void Awake()
    {
        // Get Components
        RB = GetComponent<Rigidbody2D>();
        if (RB != null)
        {
            originalGravityScale = RB.gravityScale;
        }
        Animator = GetComponentInChildren<Animator>();
        if (playerCollider == null)
        {
            playerCollider = GetComponent<CapsuleCollider2D>();
            if (playerCollider != null)
            {
                if (standingColliderSize == Vector2.zero) standingColliderSize = playerCollider.size;
                if (standingColliderOffset == Vector2.zero && playerCollider.offset != Vector2.zero) standingColliderOffset = playerCollider.offset;
            }
            else
            {
                Debug.LogError("Player Collider not found or assigned!", this);
            }
        }

        // Initialize input reader
        InputReader = new InputReader();

        // Initialize concrete states
        IdleState = new PlayerIdleState(this);
        WalkState = new WalkState(this);
        RunState = new RunState(this);
    
        // Register states
        stateRegistry[nameof(PlayerIdleState)] = IdleState;
        stateRegistry[nameof(WalkState)] = WalkState;
        stateRegistry[nameof(RunState)] = RunState;
        JumpState = new JumpState(this);
        stateRegistry[nameof(JumpState)] = JumpState;
        CrouchState = new CrouchState(this);
        stateRegistry[nameof(CrouchState)] = CrouchState;
        SlideState = new SlideState(this);
        WallClingState = new WallClingState(this);
        stateRegistry[nameof(WallClingState)] = WallClingState;
        stateRegistry[nameof(SlideState)] = SlideState;
        ShootState = new ShootState(this);
        stateRegistry[nameof(ShootState)] = ShootState;
        FallState = new FallState(this);
        stateRegistry[nameof(FallState)] = FallState;

        // Initialize jumps
        JumpsRemaining = MaxJumps;

        // Get main camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No camera tagged as MainCamera found in the scene! Please ensure your camera is tagged as MainCamera.");
        }

        // Create coin counter UI
        CreateCoinCounterUI();
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
            JumpsRemaining = Mathf.Max(0, MaxJumps - 1);
        }
        wasGroundedLastFrame = isGroundedNow;

        // Handle dashing
        if (CanDash && Input.GetMouseButtonDown(1)) // Right mouse button
        {
            float timeSinceLastDash = Time.time - lastDashTime;
            if (timeSinceLastDash >= dashCooldown && !isDashing)
            {
                StartDash();
            }
            else if (isDashing)
            {
                Debug.Log($"Already dashing!");
            }
            else
            {
                Debug.Log($"Cannot dash yet. Cooldown: {dashCooldown - timeSinceLastDash:F2}s remaining");
            }
        }

        // Update dash
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
            else
            {
                // Apply dash velocity
                if (RB != null)
                {
                    // During dash, maintain the exact dash velocity without gravity
                    RB.linearVelocity = dashDirection * dashSpeed;
                }
            }
        }

        currentState?.Tick(Time.deltaTime);

        // Robust charge UI: show if in ShootState, always update fill
        if (chargeUI != null)
        {
            bool inShootState = currentState == ShootState;
            chargeUI.SetActive(inShootState);
            if (chargeFillImage != null)
                chargeFillImage.fillAmount = Mathf.Clamp01(currentCharge / chargeTime);
        }

        // Handle shooting input with cooldown
        if (InputReader.IsShootPressed())
        {
            float timeSinceLastShot = Time.time - lastShootTime;
            if (timeSinceLastShot >= shootCooldown)
            {
                ShootProjectileAtCursor();
                lastShootTime = Time.time;
            }
            else
            {
                Debug.Log($"Cannot shoot yet. Cooldown: {shootCooldown - timeSinceLastShot:F2}s remaining");
            }
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
            Debug.LogError("projectilePrefab is not assigned on PlayerStateMachine! Please assign a projectile prefab in the Unity Inspector.");
            return;
        }
        if (mainCamera == null) {
            // Try to find the camera again
            mainCamera = Camera.main;
            if (mainCamera == null) {
                Debug.LogError("No camera tagged as MainCamera found in the scene! Please ensure your camera is tagged as MainCamera.");
                return;
            }
        }

        Debug.Log("Attempting to shoot projectile...");
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        
        // Spawn the projectile slightly in front of the player to avoid self-collision
        Vector3 spawnPosition = transform.position + new Vector3(direction.x, direction.y, 0) * 0.5f;
        GameObject proj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Projectile instantiated at position: {spawnPosition}");
        
        // Check if the projectile has all required components
        SpriteRenderer spriteRenderer = proj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            Debug.LogError("Projectile prefab is missing SpriteRenderer component!");
        } else if (spriteRenderer.sprite == null) {
            Debug.LogError("Projectile prefab's SpriteRenderer has no sprite assigned!");
        }

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb == null) {
            Debug.LogError("Projectile prefab is missing Rigidbody2D component!");
        } else {
            rb.linearVelocity = direction * projectileSpeed;
            Debug.Log($"Projectile velocity set to: {rb.linearVelocity}");
        }

        Collider2D collider = proj.GetComponent<Collider2D>();
        if (collider == null) {
            Debug.LogError("Projectile prefab is missing Collider2D component!");
        } else if (!collider.isTrigger) {
            Debug.LogError("Projectile prefab's Collider2D is not set to 'Is Trigger'!");
        }

        // Check if the projectile has the Projectile script
        Projectile projectileScript = proj.GetComponent<Projectile>();
        if (projectileScript == null) {
            Debug.LogError("Projectile prefab is missing Projectile script!");
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

    private void CreateCoinCounterUI()
    {
        // Try to find the existing Canvas
        Canvas canvas = null;
        GameObject canvasGO = GameObject.Find("ChargeMeterUI");
        if (canvasGO != null)
        {
            canvas = canvasGO.GetComponent<Canvas>();
        }
        if (canvas == null)
        {
            // Fallback: create a new canvas if not found
            canvasGO = new GameObject("CoinUICanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create the coin text
        GameObject coinTextGO = new GameObject("CoinText");
        coinTextGO.transform.SetParent(canvas.transform);
        coinText = coinTextGO.AddComponent<Text>();
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinText.fontSize = 24;
        coinText.color = Color.yellow;
        coinText.alignment = TextAnchor.UpperLeft;
        RectTransform rect = coinText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(160, 40);
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = string.Format(COIN_TEXT_FORMAT, coinCount);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player died!");
        // Optionally play death animation
        if (Animator != null)
        {
            Animator.Play("Death"); // Make sure you have a death animation
        }
        // Disable player controls
        this.enabled = false;
        // Reload the scene after a short delay
        Invoke("ReloadLevel", 1.5f); // Wait for animation to play
    }

    private void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void StartDash()
    {
        if (!CanDash) return;

        isDashing = true;
        dashTimer = dashDuration;
        lastDashTime = Time.time;
        originalPosition = transform.position;

        // Get dash direction based on movement input or facing direction
        Vector2 moveInput = InputReader.GetMovementInput();
        if (moveInput != Vector2.zero)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            // Use facing direction if no movement input
            dashDirection = new Vector2(transform.localScale.x > 0 ? 1 : -1, 0);
        }

        // Hop up slightly when starting dash
        transform.position += Vector3.up * liftAmount;

        // Disable gravity during dash
        if (RB != null)
        {
            RB.gravityScale = 0f;
        }

        Debug.Log($"Started dashing in direction: {dashDirection}");
    }

    private void EndDash()
    {
        isDashing = false;
        if (RB != null)
        {
            // Restore original gravity scale
            RB.gravityScale = originalGravityScale;
            // Reset velocity to prevent sliding
            RB.linearVelocity = Vector2.zero;
        }

        // Move back down to original height
        transform.position -= Vector3.up * liftAmount;

        Debug.Log("Dash ended");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If we hit something while dashing, end the dash
        if (isDashing)
        {
            EndDash();
        }
    }
}