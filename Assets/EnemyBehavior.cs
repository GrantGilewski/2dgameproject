using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyBehavior : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float respawnDelay = 3f;
    
    [Header("Health Bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // Offset above enemy
    [SerializeField] private Vector2 healthBarSize = new Vector2(1.5f, 0.2f); // Width and height of health bar
    
    [Header("Death Settings")]
    [SerializeField] private Color deathColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray and semi-transparent
    [SerializeField] private bool showRespawnTimer = true;
    
    [Header("Aggression Settings")]
    [SerializeField] private bool isAggressive = false; // Enable enemy aggression
    [SerializeField] private float detectionRadius = 5f; // Radius to detect and follow player
    [SerializeField] private float followSpeed = 2f; // Speed when following player
    [SerializeField] private float attackRange = 2f; // Range to attack player
    [SerializeField] private float attackCooldown = 2f; // Time between attacks
    [SerializeField] private float attackDamage = 15f; // Damage dealt by enemy attacks
    [SerializeField] private float attackDuration = 0.3f; // How long attack damage object lasts
    [SerializeField] private Vector2 attackSize = new Vector2(1f, 1f); // Size of attack damage object
    
    [Header("Collision")]
    [SerializeField] private LayerMask playerLayerMask = 1; // What layers count as player
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private Rigidbody2D rb;
    private int currentHealth;
    private bool isDead = false;
    private Vector3 spawnPosition;
    private Color originalColor;
    
    // Aggression System
    private Transform playerTransform;
    private bool playerInRange = false;
    private float lastAttackTime = 0f;
    private bool isFacingRight = true;
    
    // Damage Object Integration
    private bool inDamageZone = false;
    private float lastDamageTime = 0f;
    private DamageObject currentDamageObject;
    
    // Health Bar UI
    private Canvas healthBarCanvas;
    private Image healthBarBackground;
    private Image healthBarFill;
    private Text respawnTimerText; // New: For showing respawn countdown
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        enemyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        
        // Add rigidbody if none exists (needed for movement)
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f; // Normal gravity
            rb.freezeRotation = true; // Prevent rotation
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
            
            // Create physics material for better ground interaction
            PhysicsMaterial2D enemyMaterial = new PhysicsMaterial2D("EnemyMaterial");
            enemyMaterial.friction = 0.4f; // Some friction to prevent sliding
            enemyMaterial.bounciness = 0f; // No bouncing
            
            // Apply material to collider
            if (enemyCollider != null)
            {
                enemyCollider.sharedMaterial = enemyMaterial;
            }
        }
        
        // Add collider if none exists
        if (enemyCollider == null)
        {
            enemyCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Find player for aggression system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Set up collision layer - make sure enemy doesn't collide with player
        SetupCollisionLayers();
        
        // Initialize health and position
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        
        // Store original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Create health bar
        CreateHealthBar();
        
        Debug.Log($"Enemy {gameObject.name} initialized at {spawnPosition} with {maxHealth} health. Aggressive: {isAggressive}");
    }
    
    void Update()
    {
        // Early exit if dead to prevent any processing
        if (isDead) 
        {
            // Clear any remaining damage state when dead
            if (inDamageZone)
            {
                inDamageZone = false;
                currentDamageObject = null;
            }
            return;
        }
        
        // Validate essential components before processing
        if (!ValidateComponents()) return;
        
        UpdateHealthBarPosition();
        
        // Handle continuous damage while in damage zone
        if (inDamageZone && currentDamageObject != null)
        {
            try
            {
                // Additional null check for the damage object and its gameObject
                if (currentDamageObject != null && currentDamageObject.gameObject != null && Time.time - lastDamageTime >= currentDamageObject.damageRate)
                {
                    TakeDamage(currentDamageObject.damageAmount);
                    
                    // Exit immediately if we died from this damage
                    if (isDead) return;
                    
                    lastDamageTime = Time.time;
                    // Safe logging with multiple null checks
                    string objName = "Unknown";
                    if (currentDamageObject != null && currentDamageObject.gameObject != null)
                    {
                        objName = currentDamageObject.gameObject.name;
                    }
                    Debug.Log($"Enemy {gameObject.name} took {currentDamageObject.damageAmount} damage from {objName}");
                }
                else if (currentDamageObject == null || currentDamageObject.gameObject == null)
                {
                    // Clean up null damage object reference
                    inDamageZone = false;
                    currentDamageObject = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing damage for {gameObject.name}: {e.Message}");
                // Clean up on error
                inDamageZone = false;
                currentDamageObject = null;
            }
        }
        
        // Handle aggression behavior
        if (isAggressive && playerTransform != null)
        {
            try
            {
                HandleAggression();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in HandleAggression for {gameObject.name}: {e.Message}");
            }
        }
    }
    
    private bool ValidateComponents()
    {
        // Re-get essential components if they're null
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider2D>();
        }
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        return true; // Continue processing even if some components are missing
    }
    
    // Damage Object Integration - Trigger Events
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return; // Don't process triggers when dead
        
        DamageObject damageObj = other.GetComponent<DamageObject>();
        if (damageObj != null)
        {
            inDamageZone = true;
            currentDamageObject = damageObj;
            lastDamageTime = 0f; // Reset timer to cause immediate damage
            Debug.Log($"Enemy {gameObject.name} entered damage zone: {other.gameObject.name}");
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return; // Don't process triggers when dead
        
        DamageObject damageObj = other.GetComponent<DamageObject>();
        if (damageObj != null && currentDamageObject == damageObj)
        {
            inDamageZone = false;
            currentDamageObject = null;
            Debug.Log($"Enemy {gameObject.name} exited damage zone: {other.gameObject.name}");
        }
    }
    
    // Damage Object Integration - Collision Events
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return; // Don't process collisions when dead
        
        DamageObject damageObj = collision.gameObject.GetComponent<DamageObject>();
        if (damageObj != null)
        {
            inDamageZone = true;
            currentDamageObject = damageObj;
            lastDamageTime = 0f; // Reset timer to cause immediate damage
            Debug.Log($"Enemy {gameObject.name} collided with damage object: {collision.gameObject.name}");
        }
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        if (isDead) return; // Don't process collisions when dead
        
        DamageObject damageObj = collision.gameObject.GetComponent<DamageObject>();
        if (damageObj != null && currentDamageObject == damageObj)
        {
            inDamageZone = false;
            currentDamageObject = null;
            Debug.Log($"Enemy {gameObject.name} stopped colliding with damage object: {collision.gameObject.name}");
        }
    }
    
    private void HandleAggression()
    {
        if (playerTransform == null || rb == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if player is within detection radius
        if (distanceToPlayer <= detectionRadius)
        {
            playerInRange = true;
            
            // Follow player if not in attack range
            if (distanceToPlayer > attackRange)
            {
                FollowPlayer();
            }
            else
            {
                // Stop moving when in attack range
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
                
                // Try to attack if cooldown is ready
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    AttackPlayer();
                }
            }
            
            // Face the player
            FacePlayer();
        }
        else
        {
            playerInRange = false;
            // Stop moving when player is out of range
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }
    
    private void FollowPlayer()
    {
        if (rb == null || playerTransform == null) return;
        
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        
        // Move towards player (only horizontal movement)
        float horizontalMovement = direction.x * followSpeed;
        rb.linearVelocity = new Vector2(horizontalMovement, rb.linearVelocity.y);
    }
    
    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null) return;
        
        bool shouldFaceRight = playerTransform.position.x > transform.position.x;
        
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            spriteRenderer.flipX = !isFacingRight; // Flip sprite based on direction
        }
    }
    
    private void AttackPlayer()
    {
        if (playerTransform == null) return;
        
        lastAttackTime = Time.time;
        
        // Create attack damage object in direction of player
        Vector3 attackDirection = (playerTransform.position - transform.position).normalized;
        Vector3 attackPosition = transform.position + attackDirection * (attackRange * 0.7f); // Slightly in front of enemy
        
        StartCoroutine(CreateAttackDamageObject(attackPosition));
        
        Debug.Log($"Enemy {gameObject.name} attacks player!");
    }
    
    private IEnumerator CreateAttackDamageObject(Vector3 position)
    {
        // Create temporary attack damage object
        GameObject attack = new GameObject($"{gameObject.name}_Attack");
        attack.transform.position = position;
        
        // Add collider for damage detection
        BoxCollider2D attackCollider = attack.AddComponent<BoxCollider2D>();
        attackCollider.size = attackSize;
        attackCollider.isTrigger = true;
        
        // Add damage object component
        DamageObject damageComponent = attack.AddComponent<DamageObject>();
        damageComponent.damageAmount = (int)attackDamage;
        damageComponent.damageRate = 0.1f; // Fast damage rate
        
        // Use reflection to set that this should NOT damage enemies (only player)
        var excludeEnemyField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeEnemyField != null)
        {
            excludeEnemyField.SetValue(damageComponent, false);
        }
        
        // Visual indicator (temporary - will be replaced with graphics later)
        SpriteRenderer attackRenderer = attack.AddComponent<SpriteRenderer>();
        
        // Create red attack sprite
        Texture2D attackTexture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 0.3f, 0f, 0.7f); // Orange-red color
        }
        attackTexture.SetPixels(pixels);
        attackTexture.Apply();
        
        attackRenderer.sprite = Sprite.Create(attackTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
        attackRenderer.sortingOrder = 8;
        
        // Wait for attack duration
        yield return new WaitForSeconds(attackDuration);
        
        // Destroy attack object
        Destroy(attack);
    }
    
    private void SetupCollisionLayers()
    {
        // Set this GameObject to NPC|Enemy layer
        gameObject.layer = LayerMask.NameToLayer("NPC|Enemy");
        
        // Find player and ignore collision
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null && enemyCollider != null)
            {
                Physics2D.IgnoreCollision(enemyCollider, playerCollider, true);
                Debug.Log($"Enemy {gameObject.name} - Collision with player disabled");
            }
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Enemy {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Update health bar display
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            // Immediately clear damage zone state before dying to prevent Update conflicts
            inDamageZone = false;
            currentDamageObject = null;
            Die();
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"Enemy {gameObject.name} died");
        
        // Stop all movement when dead
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // Change sprite color to death color and keep it visible
        if (spriteRenderer != null)
        {
            spriteRenderer.color = deathColor;
        }
        
        // Disable collider so it can't take more damage or be interacted with
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // Clean up all damage-related state
        inDamageZone = false;
        currentDamageObject = null;
        lastDamageTime = 0f;
        
        // Reset aggression state
        playerInRange = false;
        lastAttackTime = 0f;
        
        // Update health bar to show death state
        UpdateHealthBar();
        
        // Start respawn countdown
        StartCoroutine(RespawnCountdown());
    }
    
    private IEnumerator RespawnCountdown()
    {
        float timeRemaining = respawnDelay;
        
        while (timeRemaining > 0)
        {
            if (showRespawnTimer && respawnTimerText != null)
            {
                respawnTimerText.text = $"Respawning in: {timeRemaining:F1}s";
                respawnTimerText.gameObject.SetActive(true);
            }
            
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        
        Respawn();
    }
    
    private void Respawn()
    {
        Debug.Log($"Enemy {gameObject.name} respawning");
        
        // Reset position to spawn point
        transform.position = spawnPosition;
        
        // Reset health
        currentHealth = maxHealth;
        isDead = false;
        
        // Ensure rigidbody is still valid and reset its velocity
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // Restore original sprite color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        // Re-enable collider
        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }
        
        // Hide respawn timer
        if (respawnTimerText != null)
        {
            respawnTimerText.gameObject.SetActive(false);
        }
        
        // Reset aggression state
        playerInRange = false;
        lastAttackTime = 0f;
        
        // Update health bar
        UpdateHealthBar();
        
        // Re-setup collision layers (in case something changed)
        SetupCollisionLayers();
        
        // Re-find player if reference was lost
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    
    private void CreateHealthBar()
    {
        // Create a world space canvas for the health bar
        GameObject canvasGO = new GameObject($"{gameObject.name}_HealthBarCanvas");
        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 10; // Ensure it renders on top
        
        // Set canvas size and position
        RectTransform canvasRect = healthBarCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarSize.x * 100, healthBarSize.y * 100); // Scale up for world space
        canvasRect.localScale = Vector3.one * 0.01f; // Scale down to appropriate size
        
        // Background
        GameObject backgroundGO = new GameObject("HealthBarBackground");
        backgroundGO.transform.SetParent(canvasGO.transform, false);
        
        healthBarBackground = backgroundGO.AddComponent<Image>();
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark background
        
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        
        // Health fill
        GameObject fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.color = new Color(0f, 1f, 0f, 0.9f); // Green fill
        
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // Respawn timer text
        GameObject timerGO = new GameObject("RespawnTimer");
        timerGO.transform.SetParent(canvasGO.transform, false);
        
        respawnTimerText = timerGO.AddComponent<Text>();
        respawnTimerText.text = "";
        respawnTimerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        respawnTimerText.fontSize = 14;
        respawnTimerText.color = Color.white;
        respawnTimerText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform timerRect = timerGO.GetComponent<RectTransform>();
        timerRect.anchorMin = Vector2.zero;
        timerRect.anchorMax = Vector2.one;
        timerRect.sizeDelta = new Vector2(0, 30); // Taller for text
        timerRect.anchoredPosition = new Vector2(0, -20); // Below health bar
        
        respawnTimerText.gameObject.SetActive(false); // Hidden by default
        
        // Update initial health display
        UpdateHealthBar();
    }
    
    private void UpdateHealthBarPosition()
    {
        if (healthBarCanvas != null)
        {
            Vector3 healthBarPosition = transform.position + healthBarOffset;
            healthBarCanvas.transform.position = healthBarPosition;
            
            // Make health bar face the camera
            if (Camera.main != null)
            {
                healthBarCanvas.transform.LookAt(Camera.main.transform);
                healthBarCanvas.transform.Rotate(0, 180, 0); // Flip to face camera correctly
            }
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercentage = isDead ? 0f : (float)currentHealth / maxHealth;
            
            // Update fill amount
            healthBarFill.fillAmount = healthPercentage;
            
            // Update fill color based on health percentage
            if (isDead)
            {
                healthBarFill.color = new Color(0.3f, 0.3f, 0.3f, 0.9f); // Dark gray when dead
            }
            else if (healthPercentage > 0.6f)
            {
                healthBarFill.color = new Color(0f, 1f, 0f, 0.9f); // Green
            }
            else if (healthPercentage > 0.3f)
            {
                healthBarFill.color = new Color(1f, 1f, 0f, 0.9f); // Yellow
            }
            else
            {
                healthBarFill.color = new Color(1f, 0f, 0f, 0.9f); // Red
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up health bar when enemy is destroyed
        if (healthBarCanvas != null)
        {
            Destroy(healthBarCanvas.gameObject);
        }
    }
}