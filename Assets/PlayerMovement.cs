using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Detection")]
    public LayerMask groundLayerMask = 1;
    public LayerMask solidObjectLayerMask = 1; // For detecting solid objects (not one-way platforms)
    public float groundCheckDistance = 0.1f;
    public float ceilingCheckDistance = 0.2f; // Distance to check for ceiling/objects above
    
    [Header("Health System")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float minFallDamageHeight = 5f; // Minimum height to start taking damage
    [SerializeField] private float maxFallDamageHeight = 15f; // Height that causes instant death
    [SerializeField] private int maxFallDamage = 80; // Maximum damage before instant death
    
    [Header("Health Regeneration")]
    [SerializeField] private bool enableHealthRegeneration = true;
    [SerializeField] private float healthRegenDelay = 3f; // Time to wait after taking damage before regenerating
    [SerializeField] private float healthRegenRate = 2f; // Health points per second
    [SerializeField] private float healthRegenInterval = 0.5f; // How often to regenerate (in seconds)
    
    [Header("Health Bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // Offset above player
    [SerializeField] private Vector2 healthBarSize = new Vector2(2f, 0.3f); // Width and height of health bar
    
    [Header("Sprite Animation")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite walkingSprite1;
    [SerializeField] private Sprite walkingSprite2;
    [SerializeField] private Sprite fallingSprite;
    [SerializeField] private float animationSpeed = 0.2f; // Time between frame changes

    // Components
    private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D playerCollider;
    private SpriteRenderer spriteRenderer;

    // Input
    private float horizontalInput;
    private bool jumpInput;
    private bool leftInput;
    private bool rightInput;
    private bool downInput;

    // State
    private bool isGrounded;
    private bool hasCeilingClearance = true; // Check if player has space above to jump
    private float animationTimer = 0f;
    private bool useFirstWalkSprite = true;

    private GameObject currentPlatform;
    
    // Health System
    private int currentHealth;
    private Vector3 spawnPosition;
    
    // Health Regeneration System
    private float lastDamageTime;
    private float lastRegenTime;
    private bool isRegenerating = false;
    
    // Fall Damage System
    private bool isFalling = false;
    private float fallStartHeight;
    private bool wasGroundedLastFrame = false;
    
    // Water Physics System
    private bool inWater = false;
    private WaterProperties currentWaterProperties;
    private float originalMoveSpeed;
    private float originalJumpForce;
    private float originalGravityScale;
    private float timeInWater = 0f;
    private int waterObjectCount = 0; // Track how many water objects player is in
    
    // Health Bar UI
    private Canvas healthBarCanvas;
    private Image healthBarBackground;
    private Image healthBarFill;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        
        // Ensure we have a capsule collider
        if (playerCollider == null)
        {
            playerCollider = gameObject.AddComponent<CapsuleCollider2D>();
            playerCollider.direction = CapsuleDirection2D.Vertical;
        }
        
        // Try to get SpriteRenderer from child object first, then from this object
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevent player from rotating when falling off edges
        rb.freezeRotation = true;
        
        // Improve slope handling with physics material
        PhysicsMaterial2D slopeMaterial = new PhysicsMaterial2D("PlayerSlopeMaterial");
        slopeMaterial.friction = 0.4f; // Moderate friction for slopes
        slopeMaterial.bounciness = 0f; // No bouncing
        playerCollider.sharedMaterial = slopeMaterial;        // Initialize health system
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        
        // Initialize water physics system
        originalMoveSpeed = moveSpeed;
        originalJumpForce = jumpForce;
        originalGravityScale = rb.gravityScale;
        
        // Create health bar
        CreateHealthBar();
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        CheckCeilingClearance();
        HandleFallDamage();
        HandleWaterPhysics();
        HandleMovement();
        HandleHealthRegeneration();
        UpdateSprite();
        UpdateHealthBarPosition();
    }
    
    private void GetInput()
    {
        // Use new Input System
        horizontalInput = 0f;
        jumpInput = false;
        leftInput = false;
        rightInput = false;

        // Check keyboard directly
        if (Keyboard.current != null)
        {
            // Left/right - WASD and arrow keys
            leftInput = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            rightInput = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            downInput = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
            // Jump - spacebar, W key, and up arrow (use wasPressedThisFrame for better jump responsiveness)
            jumpInput = Keyboard.current.spaceKey.wasPressedThisFrame ||
                       Keyboard.current.wKey.wasPressedThisFrame ||
                       Keyboard.current.upArrowKey.wasPressedThisFrame;
            
            // Set horizontal input
            if (leftInput)
                horizontalInput = -1f;
            else if (rightInput)
                horizontalInput = 1f;
        }
    }
    
    private void HandleMovement()
    {
        // Horizontal movement (modified by water if in water)
        float currentMoveSpeed = inWater && currentWaterProperties != null ? 
            originalMoveSpeed * currentWaterProperties.speedModifier : moveSpeed;
        
        float targetVelocityX = horizontalInput * currentMoveSpeed;
        
        // Apply water current if in water
        if (inWater && currentWaterProperties != null)
        {
            targetVelocityX += currentWaterProperties.currentForceX;
        }
        
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        
        // Sprite flipping
        if (spriteRenderer != null)
        {
            if (horizontalInput < -0.1f)
            {
                spriteRenderer.flipX = true; // Face left
                //spriteRenderer.transform.localPosition = new Vector3(-0.1f, spriteRenderer.transform.localPosition.y, spriteRenderer.transform.localPosition.z);
            }
            else if (horizontalInput > 0.1f)
            {
                spriteRenderer.flipX = false; // Face right
                //spriteRenderer.transform.localPosition = new Vector3(0.1f, spriteRenderer.transform.localPosition.y, spriteRenderer.transform.localPosition.z);
            }
        }
        
        // Jumping/Swimming
        if (inWater)
        {
            // Check if W or Up is being held for continuous swimming
            bool swimUpInput = false;
            if (Keyboard.current != null)
            {
                swimUpInput = Keyboard.current.wKey.isPressed || 
                             Keyboard.current.upArrowKey.isPressed ||
                             Keyboard.current.spaceKey.isPressed;
            }
            
            if (swimUpInput)
            {
                // Continuous swimming upward when holding swim keys
                float swimForce = currentWaterProperties != null ? 
                    originalJumpForce * currentWaterProperties.jumpForceModifier : originalJumpForce * 0.8f;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimForce);
            }
        }
        else if (jumpInput && isGrounded && hasCeilingClearance)
        {
            // Normal jumping - only if we have ceiling clearance
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        
        // Drop down through one-way platform
        if (downInput && currentPlatform != null && isGrounded)
        {
            StartCoroutine(DisableCollision());
        }
    }

    private void CheckGrounded()
    {
        Vector2 colliderCenter = (Vector2)transform.position + (Vector2)(playerCollider.offset * transform.localScale.y);
        float checkDistance = groundCheckDistance;
        
        // Account for scaling when calculating the effective collider dimensions
        float scaledPlayerWidth = playerCollider.size.x * transform.localScale.x;
        float scaledPlayerHeight = playerCollider.size.y * transform.localScale.y;
        
        // For capsule collider, calculate the actual bottom position
        // The capsule extends from center by half its height, then we need to account for the rounded ends
        float halfHeight = scaledPlayerHeight * 0.5f;
        float bottomY = colliderCenter.y - halfHeight;
        
        // Create multiple ground check positions across the player's width
        Vector2 leftCheckPos = new Vector2(colliderCenter.x - scaledPlayerWidth * 0.3f, bottomY - 0.05f);
        Vector2 centerCheckPos = new Vector2(colliderCenter.x, bottomY - 0.05f);
        Vector2 rightCheckPos = new Vector2(colliderCenter.x + scaledPlayerWidth * 0.3f, bottomY - 0.05f);
        
        // Check ground at multiple points
        bool leftGrounded = CheckGroundAtPosition(leftCheckPos, checkDistance);
        bool centerGrounded = CheckGroundAtPosition(centerCheckPos, checkDistance);
        bool rightGrounded = CheckGroundAtPosition(rightCheckPos, checkDistance);
        
        // Additional check using OverlapBox for better tilemap detection
        Vector2 overlapBoxCenter = new Vector2(colliderCenter.x, bottomY - checkDistance * 0.5f);
        Vector2 overlapBoxSize = new Vector2(scaledPlayerWidth * 0.8f, checkDistance);
        Collider2D groundCollider = Physics2D.OverlapBox(overlapBoxCenter, overlapBoxSize, 0f, groundLayerMask);
        bool overlapGrounded = groundCollider != null && groundCollider != playerCollider && groundCollider.gameObject != gameObject;
        
        // Player is grounded if ANY of the check points hit ground OR the overlap box detects ground
        isGrounded = leftGrounded || centerGrounded || rightGrounded || overlapGrounded;
    }
    
    private void CheckCeilingClearance()
    {
        Vector2 colliderCenter = (Vector2)transform.position + (Vector2)(playerCollider.offset * transform.localScale.y);
        
        // Account for scaling when calculating the effective collider dimensions
        float scaledPlayerWidth = playerCollider.size.x * transform.localScale.x;
        float scaledPlayerHeight = playerCollider.size.y * transform.localScale.y;
        
        // Check if player is inside a solid object by detecting overlaps
        Bounds playerBounds = new Bounds(colliderCenter, new Vector3(scaledPlayerWidth * 0.9f, scaledPlayerHeight * 0.9f, 0));
        
        // Get all colliders that overlap with the player
        Collider2D[] overlappingColliders = Physics2D.OverlapAreaAll(
            (Vector2)playerBounds.min, 
            (Vector2)playerBounds.max, 
            solidObjectLayerMask
        );
        
        bool insideSolidObject = false;
        
        foreach (Collider2D col in overlappingColliders)
        {
            // Skip our own collider
            if (col == playerCollider || col.gameObject == gameObject)
                continue;
                
            // Skip one-way platforms - they don't count as being "inside"
            if (col.CompareTag("OneWayPlatform"))
                continue;
                
            // Skip water objects - they don't block jumping
            if (col.CompareTag("Water"))
                continue;
            
            insideSolidObject = true;
            break;
        }
        
        // Player has ceiling clearance if they're not inside a solid object
        hasCeilingClearance = !insideSolidObject;
    }
    
    private void HandleFallDamage()
    {
        // Don't track fall damage while in water
        if (inWater)
        {
            isFalling = false;
            wasGroundedLastFrame = isGrounded;
            return;
        }
        
        // Check if we just started falling
        if (wasGroundedLastFrame && !isGrounded && rb.linearVelocity.y <= 0)
        {
            isFalling = true;
            fallStartHeight = transform.position.y;
        }
        
        // Check if we just landed
        if (!wasGroundedLastFrame && isGrounded && isFalling)
        {
            float fallDistance = fallStartHeight - transform.position.y;
            
            // Only apply damage if fall was significant
            if (fallDistance > minFallDamageHeight)
            {
                ApplyFallDamage(fallDistance);
            }
            
            isFalling = false;
        }
        
        // Update the previous frame ground state
        wasGroundedLastFrame = isGrounded;
    }
    
    private void ApplyFallDamage(float fallDistance)
    {
        int damage = 0;
        
        // Calculate damage based on fall distance
        if (fallDistance >= maxFallDamageHeight)
        {
            // Instant death for extreme falls
            damage = currentHealth;
        }
        else
        {
            // Scale damage between 0 and maxFallDamage
            float damageRatio = (fallDistance - minFallDamageHeight) / (maxFallDamageHeight - minFallDamageHeight);
            damage = Mathf.RoundToInt(damageRatio * maxFallDamage);
        }
        
        TakeDamage(damage);
    }
    
    private void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Record damage time for regeneration system
        lastDamageTime = Time.time;
        isRegenerating = false;
        
        // Update health bar display
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void HandleHealthRegeneration()
    {
        // Don't regenerate if system is disabled, health is full, or player is dead
        if (!enableHealthRegeneration || currentHealth >= maxHealth || currentHealth <= 0)
        {
            return;
        }
        
        // Check if enough time has passed since last damage
        if (Time.time - lastDamageTime >= healthRegenDelay)
        {
            // Check if enough time has passed since last regeneration tick
            if (!isRegenerating || Time.time - lastRegenTime >= healthRegenInterval)
            {
                if (!isRegenerating)
                {
                    isRegenerating = true;
                    lastRegenTime = Time.time;
                }
                
                // Regenerate health
                int healthToRegenerate = Mathf.RoundToInt(healthRegenRate * healthRegenInterval);
                currentHealth += healthToRegenerate;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                
                // Update health bar
                UpdateHealthBar();
                
                // Update last regeneration time
                lastRegenTime = Time.time;
                
                // Stop regenerating if health is full
                if (currentHealth >= maxHealth)
                {
                    isRegenerating = false;
                }
            }
        }
    }
    
    // Public method for external damage sources
    public void TakeDamageFromObject(int damage)
    {
        TakeDamage(damage);
    }
    
    // Public method for water state management
    public void SetWaterState(bool enteringWater, WaterProperties properties = null)
    {
        if (enteringWater)
        {
            // Increment water object counter
            waterObjectCount++;
            
            // Only set up water physics if this is the first water object
            if (waterObjectCount == 1)
            {
                inWater = true;
                timeInWater = 0f;
                
                // Reset fall damage tracking when entering water
                isFalling = false;
                fallStartHeight = 0f;
            }
            
            // Update water properties (use the most recent properties)
            if (properties != null)
            {
                currentWaterProperties = properties;
                // Apply water physics modifications
                rb.gravityScale = originalGravityScale * properties.gravityModifier;
            }
        }
        else
        {
            // Decrement water object counter
            waterObjectCount = Mathf.Max(0, waterObjectCount - 1);
            
            // Only disable water physics when exiting ALL water objects
            if (waterObjectCount == 0)
            {
                inWater = false;
                
                // Restore normal physics
                currentWaterProperties = null;
                rb.gravityScale = originalGravityScale;
                timeInWater = 0f;
                
                // Reset fall tracking when exiting water to prevent false fall damage
                isFalling = false;
                wasGroundedLastFrame = false;
            }
        }
    }
    
    // Public method to get current water object count (for debugging)
    public int GetWaterObjectCount()
    {
        return waterObjectCount;
    }
    
    private void HandleWaterPhysics()
    {
        if (inWater && currentWaterProperties != null)
        {
            timeInWater += Time.deltaTime;
            
            // Apply gentle buoyancy only when sinking too fast
            if (rb.linearVelocity.y < -3f)
            {
                // Gentle upward force to prevent endless sinking
                rb.AddForce(Vector2.up * currentWaterProperties.buoyancyForce * 0.3f, ForceMode2D.Force);
            }
            
            // Apply gentle drag force
            Vector2 dragForce = -rb.linearVelocity * currentWaterProperties.dragForce * 0.3f;
            rb.AddForce(dragForce, ForceMode2D.Force);
            
            // Apply water current (vertical)
            if (currentWaterProperties.currentForceY != 0)
            {
                rb.AddForce(Vector2.up * currentWaterProperties.currentForceY, ForceMode2D.Force);
            }
            
            // Handle player rotation for tilting effect
            HandleWaterTilt();
            
            // Handle breathing (if water doesn't allow breathing)
            if (!currentWaterProperties.allowBreathing && timeInWater > 10f) // 10 seconds before drowning starts
            {
                // Start drowning damage
                if (timeInWater > 10f && ((int)timeInWater) % 2 == 0) // Damage every 2 seconds after 10 seconds
                {
                    TakeDamage(5); // Drowning damage
                }
            }
        }
        else if (!inWater)
        {
            // Reset rotation when not in water
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }
    
    private void HandleWaterTilt()
    {
        if (inWater && currentWaterProperties != null)
        {
            // Tilt player based on horizontal movement
            float tiltAngle = 0f;
            
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                // Tilt in direction of movement
                tiltAngle = horizontalInput * 15f; // 15 degrees max tilt
            }
            
            // Apply tilt rotation smoothly
            Quaternion targetRotation = Quaternion.Euler(0, 0, -tiltAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
        }
    }
    
    private void Die()
    {
        // Reset player to spawn position
        transform.position = spawnPosition;
        
        // Reset health
        currentHealth = maxHealth;
        
        // Update health bar display
        UpdateHealthBar();
        
        // Reset physics
        rb.linearVelocity = Vector2.zero;
        
        // Reset fall tracking
        isFalling = false;
        wasGroundedLastFrame = false;
        
        // Reposition camera directly on player after respawn
        CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.CenterOnTarget();
        }
    }
    
    private void CreateHealthBar()
    {
        // Create a world space canvas for the health bar
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 10; // Ensure it renders on top
        
        // Set canvas size and position
        RectTransform canvasRect = healthBarCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarSize.x * 100, healthBarSize.y * 100); // Scale up for world space
        canvasRect.localScale = Vector3.one * 0.01f; // Scale down for proper world size
        
        // Create background (grey bar that shows full health bar area)
        GameObject backgroundGO = new GameObject("HealthBarBackground");
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        healthBarBackground = backgroundGO.AddComponent<Image>();
        
        // Set sprite for background
        // Create a simple white texture for the background
        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, Color.white);
        backgroundTexture.Apply();
        healthBarBackground.sprite = Sprite.Create(backgroundTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        
        healthBarBackground.color = new Color(1f, 1f, 1f, 0.9f); // White background
        
        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create fill (colored bar that shows current health)
        GameObject fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(backgroundGO.transform, false); // Child of background
        healthBarFill = fillGO.AddComponent<Image>();
        
        // Create a white sprite for the fill
        Texture2D fillTexture = new Texture2D(1, 1);
        fillTexture.SetPixel(0, 0, Color.white);
        fillTexture.Apply();
        healthBarFill.sprite = Sprite.Create(fillTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        
        healthBarFill.color = Color.green;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left; // Fill from left to right, empty from right to left
        
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // Update initial health display
        UpdateHealthBar();
    }
    
    private void UpdateHealthBarPosition()
    {
        if (healthBarCanvas != null)
        {
            // Position the health bar above the player
            Vector3 healthBarPosition = transform.position + healthBarOffset;
            healthBarCanvas.transform.position = healthBarPosition;
            
            // Make health bar face the camera
            if (Camera.main != null)
            {
                healthBarCanvas.transform.LookAt(Camera.main.transform);
                healthBarCanvas.transform.Rotate(0, 180, 0); // Flip to face properly
            }
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercent;
            
            // Change color based on health percentage
            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }
    }
    
    private bool CheckGroundAtPosition(Vector2 position, float distance)
    {
        // Use multiple detection methods for better compatibility with tilemaps
        
        // Method 1: Raycast detection
        RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.down, distance, groundLayerMask);
        
        foreach (RaycastHit2D hit in hits)
        {
            // Skip our own colliders
            if (hit.collider != playerCollider && hit.collider.gameObject != gameObject)
            {
                return true;
            }
        }
        
        // Method 2: Point overlap detection (good for composite colliders)
        Vector2 pointCheckPos = position + Vector2.down * (distance * 0.5f);
        Collider2D pointHit = Physics2D.OverlapPoint(pointCheckPos, groundLayerMask);
        if (pointHit != null && pointHit != playerCollider && pointHit.gameObject != gameObject)
        {
            return true;
        }
        
        // Method 3: Small circle overlap detection - better for CapsuleCollider2D
        Collider2D circleHit = Physics2D.OverlapCircle(position + Vector2.down * (distance * 0.5f), 0.1f, groundLayerMask);
        if (circleHit != null && circleHit != playerCollider && circleHit.gameObject != gameObject)
        {
            return true;
        }
        
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentPlatform = null;
        }
    }

    private IEnumerator DisableCollision()
    {
        BoxCollider2D platformCollider = currentPlatform.GetComponent<BoxCollider2D>();
        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
        yield return new WaitForSeconds(0.5f);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        
        // Check if player is moving horizontally
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        
        // Change sprite based on movement and ground state
        if (!isGrounded)
        {
            // Use falling sprite when in the air
            if (fallingSprite != null)
                spriteRenderer.sprite = fallingSprite;
            else if (idleSprite != null)
                spriteRenderer.sprite = idleSprite; // Fallback to idle if no falling sprite
            
            // Reset animation when in air
            animationTimer = 0f;
            useFirstWalkSprite = true;
        }
        else if (isMoving && isGrounded)
        {
            // Animate between two walking sprites when moving on ground
            animationTimer += Time.deltaTime;
            
            if (animationTimer >= animationSpeed)
            {
                // Switch between walking sprites
                useFirstWalkSprite = !useFirstWalkSprite;
                animationTimer = 0f;
            }
            
            // Set the appropriate walking sprite
            if (useFirstWalkSprite && walkingSprite1 != null)
                spriteRenderer.sprite = walkingSprite1;
            else if (!useFirstWalkSprite && walkingSprite2 != null)
                spriteRenderer.sprite = walkingSprite2;
        }
        else
        {
            // Use idle sprite when not moving and grounded
            if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;
            
            // Reset animation when not moving
            animationTimer = 0f;
            useFirstWalkSprite = true;
        }
    }
}