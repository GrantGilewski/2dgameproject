using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class WeaponClassController : MonoBehaviour
{
    [Header("GUI Settings")]
    [SerializeField] private Vector2 guiPosition = new Vector2(-150, 100); // Bottom right corner
    [SerializeField] private Vector2 slotSize = new Vector2(80, 80);
    [SerializeField] private float slotSpacing = 10f;
    
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 1.2f, 0); // Lower above shard
    
    [Header("ValorShard Attack Settings")]
    [SerializeField] private float swordRange = 0.8f; // Closer to player
    [SerializeField] private float swordWidth = 1.2f; // Wider to match player
    [SerializeField] private float swordHeight = 2.0f; // Taller to ensure coverage
    [SerializeField] private float swordDuration = 0.25f; // Halved from 0.5f
    [SerializeField] private int swordDamage = 25;
    [SerializeField] private float swordCooldown = 0.5f; // Cooldown between sword attacks
    
    [Header("ValorShard Wave Attack Settings")]
    [SerializeField] private float waveBlockSize = 1f; // Size of each wave block
    [SerializeField] private float waveBlockSpacing = 1.2f; // Spacing between blocks (no overlap)
    [SerializeField] private float waveBounceHeight = 2f; // How high blocks bounce
    [SerializeField] private int waveDamage = 30; // Damage per wave block
    
    [Header("WhisperShard Attack Settings")]
    [SerializeField] private float daggerRange = 0.6f; // Closer than sword
    [SerializeField] private float daggerWidth = 0.8f; // Smaller than sword
    [SerializeField] private float daggerHeight = 1.5f; // Smaller than sword
    [SerializeField] private float daggerDuration = 0.2f; // Quick attack
    [SerializeField] private int daggerDamage = 20;
    [SerializeField] private float daggerCooldown = 0.3f; // Cooldown between dagger melee attacks
    [SerializeField] private float projectileSpeed = 15f; // Increased speed for longer horizontal flight
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private float projectileLifetime = 3f; // How long projectile exists
    [SerializeField] private float projectileCooldown = 0.8f; // Cooldown between dagger throws
    
    [Header("StormShard Attack Settings")]
    [SerializeField] private float staffRange = 1f; // Particle emission point distance
    [SerializeField] private float lightningRange = 10f; // Max range for targeted lightning
    [SerializeField] private int lightningDamage = 30;
    [SerializeField] private float lightningCooldown = 0.6f; // Cooldown between lightning arcs
    [SerializeField] private int boltDamage = 40; // Sky bolt damage
    [SerializeField] private float lightningDuration = 0.3f; // How long lightning arc lasts
    [SerializeField] private float boltDuration = 0.5f; // How long lightning bolt impact lasts
    [SerializeField] private float boltCooldown = 1.2f; // Cooldown between lightning bolts
    [SerializeField] private float boltHeight = 100f; // Height above player for sky bolt
    [SerializeField] private float boltRange = 15f; // Range to find nearest enemy for sky bolt
    
    // Weapon System
    private enum ShardType { None, ValorShard, WhisperShard, StormShard }
    private ShardType[] equippedShards = new ShardType[2]; // Two slots
    private int activeSlotIndex = 0;
    private bool isWeaponMenuOpen = false;
    
    // ValorShard Charging System
    private bool isChargingValorAttack = false;
    private float chargeStartTime = 0f;
    private float currentChargeTime = 0f;
    
    // Cooldown System
    private float lastSwordAttackTime = 0f;
    private float lastDaggerAttackTime = 0f;
    private float lastProjectileAttackTime = 0f;
    private float lastLightningArcTime = 0f;
    private float lastLightningBoltTime = 0f;
    
    // GUI Components
    private Canvas weaponCanvas; // Automatically created
    private Image[] slotImages = new Image[2];
    private Image[] slotBackgrounds = new Image[2];
    private Image activeSlotIndicator;
    
    // Interaction System
    private GameObject nearbyShardObject;
    private ShardType nearbyShardType;
    private Canvas promptCanvas;
    private Text promptText;
    
    // Player References (this script should be attached to the player)
    private PlayerMovement playerMovement;
    private Transform playerTransform;
    
    // Storm Shard Components
    private GameObject stormParticlePoint; // Invisible emission point for storm attacks
    
    // Shard Sprites (will be loaded from the GameObjects)
    private Dictionary<ShardType, Sprite> shardSprites = new Dictionary<ShardType, Sprite>();
    
    // Public Properties
    public bool IsChargingValorAttack => isChargingValorAttack;
    
    void Start()
    {
        // Get player components (this script is attached to the player)
        playerMovement = GetComponent<PlayerMovement>();
        playerTransform = transform;
        
        if (playerMovement == null)
        {
            Debug.LogError("WeaponClassController must be attached to the same GameObject as PlayerMovement!");
            enabled = false;
            return;
        }
        
        // Initialize weapon system
        InitializeGUI();
        LoadShardSprites();
        FindStormParticlePoint();
        
        Debug.Log("WeaponClassController initialized on player");
    }
    
    void Update()
    {
        CheckForNearbyShards();
        HandleInput();
        UpdatePromptPosition();
    }
    
    private void InitializeGUI()
    {
        // Create weapon GUI canvas
        GameObject canvasGO = new GameObject("WeaponGUI");
        weaponCanvas = canvasGO.AddComponent<Canvas>();
        weaponCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        weaponCanvas.sortingOrder = 100;
        
        // Add CanvasScaler for responsive design
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add GraphicRaycaster for UI interactions
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create slot container
        GameObject slotContainer = new GameObject("SlotContainer");
        slotContainer.transform.SetParent(weaponCanvas.transform, false);
        
        RectTransform containerRect = slotContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1, 0); // Bottom right
        containerRect.anchorMax = new Vector2(1, 0);
        containerRect.pivot = new Vector2(1, 0);
        containerRect.anchoredPosition = guiPosition;
        containerRect.sizeDelta = new Vector2(slotSize.x * 2 + slotSpacing, slotSize.y);
        
        // Create slots
        for (int i = 0; i < 2; i++)
        {
            CreateWeaponSlot(slotContainer, i);
        }
        
        // Create active slot indicator
        CreateActiveSlotIndicator(slotContainer);
        
        Debug.Log("Weapon GUI initialized");
    }
    
    private void CreateWeaponSlot(GameObject parent, int slotIndex)
    {
        // Slot background
        GameObject slotBG = new GameObject($"Slot{slotIndex}_Background");
        slotBG.transform.SetParent(parent.transform, false);
        
        RectTransform bgRect = slotBG.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(-(slotIndex * (slotSize.x + slotSpacing)), 0);
        bgRect.sizeDelta = slotSize;
        
        Image bgImage = slotBG.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark background
        slotBackgrounds[slotIndex] = bgImage;
        
        // Slot content (shard image)
        GameObject slotContent = new GameObject($"Slot{slotIndex}_Content");
        slotContent.transform.SetParent(slotBG.transform, false);
        
        RectTransform contentRect = slotContent.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchoredPosition = Vector2.zero;
        
        Image contentImage = slotContent.AddComponent<Image>();
        contentImage.color = Color.clear; // Transparent by default
        slotImages[slotIndex] = contentImage;
    }
    
    private void CreateActiveSlotIndicator(GameObject parent)
    {
        GameObject indicator = new GameObject("ActiveSlotIndicator");
        indicator.transform.SetParent(parent.transform, false);
        
        RectTransform indicatorRect = indicator.AddComponent<RectTransform>();
        indicatorRect.sizeDelta = slotSize + Vector2.one * 5f; // Slightly larger than slot
        
        Image indicatorImage = indicator.AddComponent<Image>();
        indicatorImage.color = new Color(1f, 1f, 0f, 0.8f); // Yellow outline
        indicatorImage.type = Image.Type.Sliced;
        
        // Create simple border sprite
        Texture2D borderTexture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % 32;
            int y = i / 32;
            if (x < 3 || x > 28 || y < 3 || y > 28)
                pixels[i] = Color.white;
            else
                pixels[i] = Color.clear;
        }
        borderTexture.SetPixels(pixels);
        borderTexture.Apply();
        
        indicatorImage.sprite = Sprite.Create(borderTexture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
        activeSlotIndicator = indicatorImage;
        
        UpdateActiveSlotIndicator();
    }
    
    private void LoadShardSprites()
    {
        // Find shard GameObjects and extract their sprites
        string[] shardTags = { "ValorShard", "WhisperShard", "StormShard" };
        ShardType[] shardTypes = { ShardType.ValorShard, ShardType.WhisperShard, ShardType.StormShard };
        
        for (int i = 0; i < shardTags.Length; i++)
        {
            GameObject[] shardObjects = GameObject.FindGameObjectsWithTag(shardTags[i]);
            if (shardObjects.Length > 0)
            {
                SpriteRenderer spriteRenderer = shardObjects[0].GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    shardSprites[shardTypes[i]] = spriteRenderer.sprite;
                    Debug.Log($"Loaded sprite for {shardTypes[i]}");
                }
            }
        }
    }
    
    private void CheckForNearbyShards()
    {
        if (playerTransform == null) return;
        
        // Check for each shard type
        string[] shardTags = { "ValorShard", "WhisperShard", "StormShard" };
        ShardType[] shardTypes = { ShardType.ValorShard, ShardType.WhisperShard, ShardType.StormShard };
        
        GameObject closestShard = null;
        ShardType closestShardType = ShardType.None;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < shardTags.Length; i++)
        {
            GameObject[] shards = GameObject.FindGameObjectsWithTag(shardTags[i]);
            foreach (GameObject shard in shards)
            {
                float distance = Vector3.Distance(playerTransform.position, shard.transform.position);
                if (distance <= interactionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestShard = shard;
                    closestShardType = shardTypes[i];
                }
            }
        }
        
        // Update nearby shard
        if (closestShard != nearbyShardObject)
        {
            nearbyShardObject = closestShard;
            nearbyShardType = closestShardType;
            UpdateInteractionPrompt();
        }
    }
    
    private void UpdateInteractionPrompt()
    {
        if (nearbyShardObject != null && !HasShardEquipped(nearbyShardType) && GetEmptySlotIndex() != -1)
        {
            ShowInteractionPrompt($"Press E to equip\n{nearbyShardType}");
        }
        else
        {
            HideInteractionPrompt();
        }
    }
    
    private void ShowInteractionPrompt(string message)
    {
        if (promptCanvas == null)
        {
            CreateInteractionPrompt();
        }
        
        promptText.text = message;
        promptCanvas.gameObject.SetActive(true);
    }
    
    private void HideInteractionPrompt()
    {
        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
        }
    }
    
    private void CreateInteractionPrompt()
    {
        GameObject promptGO = new GameObject("InteractionPrompt");
        promptCanvas = promptGO.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 15; // Above health bar
        
        RectTransform canvasRect = promptCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(350, 80); // Even larger size
        canvasRect.localScale = Vector3.one * 0.008f; // Slightly smaller scale to fit better
        
        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(promptGO.transform, false);
        
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.85f); // Even darker background
        
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(promptGO.transform, false);
        
        promptText = textGO.AddComponent<Text>();
        promptText.text = "Press E to equip";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 24; // Even larger font
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        
        // Enable text wrapping for multi-line text
        promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5); // Smaller padding so text fills more
        textRect.offsetMax = new Vector2(-5, -5); // Smaller padding
        
        promptCanvas.gameObject.SetActive(false);
    }
    
    private void UpdatePromptPosition()
    {
        if (promptCanvas != null && promptCanvas.gameObject.activeSelf && nearbyShardObject != null)
        {
            // Position the prompt above the shard, not the player
            Vector3 promptPosition = nearbyShardObject.transform.position + promptOffset;
            promptCanvas.transform.position = promptPosition;
            
            // Face camera
            if (Camera.main != null)
            {
                promptCanvas.transform.LookAt(Camera.main.transform);
                promptCanvas.transform.Rotate(0, 180, 0);
            }
        }
    }
    
    private void HandleInput()
    {
        if (playerMovement == null) return;
        
        // Check for shard pickup using new Input System
        bool eKeyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (eKeyPressed && nearbyShardObject != null && !HasShardEquipped(nearbyShardType))
        {
            EquipShard(nearbyShardType);
        }
        
        // Check for weapon switching using new Input System
        bool qKeyHeld = Keyboard.current != null && Keyboard.current.qKey.isPressed;
        if (qKeyHeld)
        {
            // Disable player movement while in weapon menu
            isWeaponMenuOpen = true;
            
            bool leftPressed = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            bool rightPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
            
            if (leftPressed)
            {
                SwitchToSlot(1); // Left arrow switches to left slot (slot 1)
            }
            else if (rightPressed)
            {
                SwitchToSlot(0); // Right arrow switches to right slot (slot 0)
            }
        }
        else
        {
            isWeaponMenuOpen = false;
        }
        
        // Check for attack using new Input System
        bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool rightClickPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool rightClickHeld = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool rightClickReleased = Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
        
        if (!isWeaponMenuOpen)
        {
            // Handle ValorShard charging attack
            ShardType activeWeapon = equippedShards[activeSlotIndex];
            if (activeWeapon == ShardType.ValorShard && rightClickPressed)
            {
                // Start charging
                isChargingValorAttack = true;
                chargeStartTime = Time.time;
            }
            
            if (isChargingValorAttack)
            {
                currentChargeTime = Time.time - chargeStartTime;
                
                if (rightClickReleased)
                {
                    // Release charge attack
                    UseActiveWeapon(true); // Right-click attack
                    isChargingValorAttack = false;
                    currentChargeTime = 0f;
                }
            }
            else if (leftClickPressed)
            {
                UseActiveWeapon(false); // Left-click attack
            }
            else if (rightClickPressed && activeWeapon != ShardType.ValorShard)
            {
                UseActiveWeapon(true); // Right-click attack for other weapons
            }
        }
    }
    
    private void EquipShard(ShardType shardType)
    {
        int emptySlot = GetEmptySlotIndex();
        if (emptySlot == -1) return;
        
        equippedShards[emptySlot] = shardType;
        UpdateSlotDisplay(emptySlot);
        
        // Remove shard from world
        if (nearbyShardObject != null)
        {
            Destroy(nearbyShardObject);
            nearbyShardObject = null;
        }
        
        HideInteractionPrompt();
        
        Debug.Log($"Equipped {shardType} in slot {emptySlot}");
    }
    
    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 2 && equippedShards[slotIndex] != ShardType.None)
        {
            activeSlotIndex = slotIndex;
            UpdateActiveSlotIndicator();
            Debug.Log($"Switched to slot {slotIndex}: {equippedShards[slotIndex]}");
        }
        else if (equippedShards[slotIndex] == ShardType.None)
        {
            Debug.Log($"Slot {slotIndex} is empty, cannot switch to it");
        }
    }
    
    private void UseActiveWeapon(bool isRightClick = false)
    {
        ShardType activeShard = equippedShards[activeSlotIndex];
        
        switch (activeShard)
        {
            case ShardType.ValorShard:
                UseValorShard(isRightClick);
                break;
            case ShardType.WhisperShard:
                UseWhisperShard(isRightClick);
                break;
            case ShardType.StormShard:
                UseStormShard(isRightClick);
                break;
            default:
                Debug.Log("No weapon equipped in active slot");
                break;
        }
    }
    
    private void UseValorShard(bool isRightClick)
    {
        if (playerTransform == null) return;
        
        if (isRightClick)
        {
            // Right-click: Wave attack based on charge time (no cooldown for charging)
            CreateWaveAttack();
        }
        else
        {
            // Left-click: Regular sword attack with cooldown
            if (Time.time - lastSwordAttackTime < swordCooldown) return;
            
            CreateSwordAttack();
            lastSwordAttackTime = Time.time;
        }
    }
    
    private void CreateSwordAttack()
    {
        // Get player's sprite renderer to check facing direction
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        
        // Create sword attack damage object in front of player
        bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
        Vector3 attackPosition = playerTransform.position + (Vector3.right * (facingLeft ? -swordRange : swordRange));
        
        StartCoroutine(CreateSwordAttack(attackPosition));
        
        Debug.Log("ValorShard sword attack!");
    }
    
    private void UseWhisperShard(bool isRightClick)
    {
        if (playerTransform == null) return;
        
        if (isRightClick)
        {
            // Right-click: Throw projectile dagger with cooldown
            if (Time.time - lastProjectileAttackTime < projectileCooldown) return;
            
            ThrowDaggerProjectile();
            lastProjectileAttackTime = Time.time;
            Debug.Log("WhisperShard dagger throw!");
        }
        else
        {
            // Left-click: Quick dagger strike with cooldown
            if (Time.time - lastDaggerAttackTime < daggerCooldown) return;
            
            CreateDaggerStrike();
            lastDaggerAttackTime = Time.time;
            Debug.Log("WhisperShard dagger strike!");
        }
    }
    
    private void UseStormShard(bool isRightClick)
    {
        if (playerTransform == null) return;
        
        if (isRightClick)
        {
            // Right-click: Lightning bolt from sky with cooldown
            if (Time.time - lastLightningBoltTime < boltCooldown) return;
            
            CreateLightningBolt();
            lastLightningBoltTime = Time.time;
            Debug.Log("StormShard lightning bolt!");
        }
        else
        {
            // Left-click: Electric arc with cooldown
            if (Time.time - lastLightningArcTime < lightningCooldown) return;
            
            CreateElectricArc();
            lastLightningArcTime = Time.time;
            Debug.Log("StormShard electric arc!");
        }
    }
    
    private IEnumerator CreateSwordAttack(Vector3 startPosition)
    {
        // Create temporary damage object
        GameObject swordAttack = new GameObject("SwordAttack");
        swordAttack.transform.position = startPosition;
        
        // Add collider for damage detection - larger size spanning player height
        BoxCollider2D attackCollider = swordAttack.AddComponent<BoxCollider2D>();
        attackCollider.size = new Vector2(swordWidth, swordHeight);
        attackCollider.isTrigger = true;
        
        // Add damage object component with player layer exclusion
        DamageObject damageComponent = swordAttack.AddComponent<DamageObject>();
        damageComponent.damageAmount = swordDamage;
        damageComponent.damageRate = 0.1f; // Fast damage rate for sword
        
        // Use reflection to set the private excludePlayerLayer field
        var excludeField = typeof(DamageObject).GetField("excludePlayerLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeField != null)
        {
            excludeField.SetValue(damageComponent, true);
        }
        
        // Use reflection to set the private canDamageEnemies field
        var enemyDamageField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyDamageField != null)
        {
            enemyDamageField.SetValue(damageComponent, true);
        }
        
        // Visual indicator (temporary - will be replaced with graphics later)
        SpriteRenderer swordRenderer = swordAttack.AddComponent<SpriteRenderer>();
        
        // Create larger red rectangle sprite for sword attack (scaled to match collider)
        int textureWidth = Mathf.RoundToInt(swordWidth * 64); // Scale texture based on collider width
        int textureHeight = Mathf.RoundToInt(swordHeight * 32); // Scale texture based on collider height
        Texture2D swordTexture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 0f, 0f, 0.7f); // Semi-transparent red, more visible
        }
        swordTexture.SetPixels(pixels);
        swordTexture.Apply();
        
        swordRenderer.sprite = Sprite.Create(swordTexture, new Rect(0, 0, textureWidth, textureHeight), Vector2.one * 0.5f);
        swordRenderer.sortingOrder = 10;
        
        // Store initial player position and facing direction
        bool facingLeft = false;
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        if (playerSprite != null)
            facingLeft = playerSprite.flipX;
        
        // Track sword attack duration
        float elapsed = 0f;
        
        while (elapsed < swordDuration)
        {
            // Make sword follow player
            if (playerTransform != null)
            {
                Vector3 newPosition = playerTransform.position + (Vector3.right * (facingLeft ? -swordRange : swordRange));
                swordAttack.transform.position = newPosition;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Destroy attack object
        Destroy(swordAttack);
    }
    
    private void CreateWaveAttack()
    {
        // Determine number of blocks based on charge time
        int waveBlocks = GetWaveBlockCount(currentChargeTime);
        
        // Get player's facing direction
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        
        bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
        Vector3 waveDirection = facingLeft ? Vector3.left : Vector3.right;
        
        StartCoroutine(CreateWaveBlocks(waveBlocks, waveDirection));
    }
    
    private int GetWaveBlockCount(float chargeTime)
    {
        if (chargeTime < 0.5f) return 1;        // Less than 0.5 second = 1 block
        else if (chargeTime < 0.8f) return 2;   // 0.5-0.8 seconds = 2 blocks
        else if (chargeTime < 1.2f) return 3;   // 0.8-1.2 seconds = 3 blocks
        else return 5;                          // 1.2+ seconds = 5 blocks
    }
    
    private IEnumerator CreateWaveBlocks(int blockCount, Vector3 direction)
    {
        List<GameObject> waveBlocks = new List<GameObject>();
        
        // Create all wave blocks
        for (int i = 0; i < blockCount; i++)
        {
            // Calculate position for this block (no overlap)
            float distance = (i + 1) * waveBlockSpacing;
            Vector3 blockPosition = playerTransform.position + (direction * distance);
            
            // Start blocks underground
            blockPosition.y -= 1f;
            
            GameObject waveBlock = CreateWaveBlock(blockPosition, i);
            waveBlocks.Add(waveBlock);
        }
        
        // Animate blocks rising sequentially with wave delay
        for (int i = 0; i < waveBlocks.Count; i++)
        {
            if (waveBlocks[i] != null)
            {
                StartCoroutine(AnimateWaveBlock(waveBlocks[i], i * 0.1f)); // 0.1s delay between blocks
            }
        }
        
        yield return null;
    }
    
    private GameObject CreateWaveBlock(Vector3 position, int blockIndex)
    {
        GameObject waveBlock = new GameObject($"WaveBlock_{blockIndex}");
        waveBlock.transform.position = position;
        
        // Add collider for damage detection
        BoxCollider2D blockCollider = waveBlock.AddComponent<BoxCollider2D>();
        blockCollider.size = new Vector2(waveBlockSize, waveBlockSize);
        blockCollider.isTrigger = true;
        
        // Add damage object component
        DamageObject damageComponent = waveBlock.AddComponent<DamageObject>();
        damageComponent.damageAmount = waveDamage;
        damageComponent.damageRate = 0.1f;
        
        // Configure damage object
        var excludeField = typeof(DamageObject).GetField("excludePlayerLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeField != null)
        {
            excludeField.SetValue(damageComponent, true);
        }
        
        var enemyDamageField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyDamageField != null)
        {
            enemyDamageField.SetValue(damageComponent, true);
        }
        
        // Visual indicator (golden/yellow color for wave)
        SpriteRenderer blockRenderer = waveBlock.AddComponent<SpriteRenderer>();
        
        // Create texture scaled to match collider size
        int textureSize = Mathf.RoundToInt(waveBlockSize * 80); // Scale based on waveBlockSize
        Texture2D blockTexture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 0.8f, 0f, 0.9f); // Golden yellow, more visible
        }
        blockTexture.SetPixels(pixels);
        blockTexture.Apply();
        
        blockRenderer.sprite = Sprite.Create(blockTexture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f);
        blockRenderer.sortingOrder = 10;
        
        return waveBlock;
    }
    
    private IEnumerator AnimateWaveBlock(GameObject waveBlock, float delay)
    {
        // Wait for wave delay
        yield return new WaitForSeconds(delay);
        
        if (waveBlock == null) yield break;
        
        Vector3 startPos = waveBlock.transform.position;
        Vector3 groundPos = new Vector3(startPos.x, startPos.y + 1f, startPos.z); // Rise to ground level
        Vector3 peakPos = new Vector3(startPos.x, startPos.y + 1f + waveBounceHeight, startPos.z); // Bounce up
        
        float riseTime = 0.2f; // Time to rise from underground
        float bounceTime = 0.3f; // Time to bounce up and down
        float fallTime = 0.2f; // Time to fall back to ground
        
        // Rise from underground to ground level
        float elapsed = 0f;
        while (elapsed < riseTime && waveBlock != null)
        {
            float progress = elapsed / riseTime;
            waveBlock.transform.position = Vector3.Lerp(startPos, groundPos, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (waveBlock != null)
            waveBlock.transform.position = groundPos;
        
        // Bounce up
        elapsed = 0f;
        while (elapsed < bounceTime && waveBlock != null)
        {
            float progress = elapsed / bounceTime;
            float bounceProgress = Mathf.Sin(progress * Mathf.PI); // Smooth arc
            Vector3 currentPos = Vector3.Lerp(groundPos, peakPos, bounceProgress);
            waveBlock.transform.position = currentPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Fall back down
        elapsed = 0f;
        Vector3 fallStart = waveBlock != null ? waveBlock.transform.position : peakPos;
        while (elapsed < fallTime && waveBlock != null)
        {
            float progress = elapsed / fallTime;
            waveBlock.transform.position = Vector3.Lerp(fallStart, groundPos, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Wait a moment then destroy
        yield return new WaitForSeconds(0.3f);
        
        if (waveBlock != null)
            Destroy(waveBlock);
    }
    
    private void FindStormParticlePoint()
    {
        // Find existing SSParticlePoint in player hierarchy
        Transform particlePoint = transform.Find("SSParticlePoint");
        if (particlePoint == null)
        {
            // Search in children recursively
            particlePoint = GetComponentInChildren<Transform>().Find("SSParticlePoint");
        }
        
        if (particlePoint != null)
        {
            stormParticlePoint = particlePoint.gameObject;
            Debug.Log("Found SSParticlePoint at: " + particlePoint.position);
        }
        else
        {
            Debug.LogError("SSParticlePoint not found! Please create an empty GameObject named 'SSParticlePoint' as a child of the player.");
            // Create a fallback point
            stormParticlePoint = new GameObject("SSParticlePoint_Fallback");
            stormParticlePoint.transform.SetParent(transform);
            stormParticlePoint.transform.localPosition = new Vector3(1.5f, 1f, 0);
        }
    }
    
    private void UpdateStormParticlePosition()
    {
        if (stormParticlePoint == null) return;
        
        // Get player's facing direction
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        
        bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
        
        // Position the particle point based on facing direction
        // Adjust these values based on your desired positioning
        float xOffset = facingLeft ? -staffRange : staffRange;
        float yOffset = 1f; // Height above player center
        
        stormParticlePoint.transform.localPosition = new Vector3(xOffset, yOffset, 0);
    }
    
    private void CreateDaggerStrike()
    {
        if (playerTransform == null) return;
        
        // Get player's facing direction
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        
        bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
        Vector3 attackPosition = playerTransform.position + (Vector3.right * (facingLeft ? -daggerRange : daggerRange));
        
        StartCoroutine(CreateDaggerAttack(attackPosition));
    }
    
    private IEnumerator CreateDaggerAttack(Vector3 startPosition)
    {
        // Create temporary dagger damage object (smaller than sword)
        GameObject daggerAttack = new GameObject("DaggerAttack");
        daggerAttack.transform.position = startPosition;
        
        // Add collider for damage detection
        BoxCollider2D attackCollider = daggerAttack.AddComponent<BoxCollider2D>();
        attackCollider.size = new Vector2(daggerWidth, daggerHeight);
        attackCollider.isTrigger = true;
        
        // Add damage object component
        DamageObject damageComponent = daggerAttack.AddComponent<DamageObject>();
        damageComponent.damageAmount = daggerDamage;
        damageComponent.damageRate = 0.1f;
        
        // Configure damage object
        var excludeField = typeof(DamageObject).GetField("excludePlayerLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeField != null)
        {
            excludeField.SetValue(damageComponent, true);
        }
        
        var enemyDamageField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyDamageField != null)
        {
            enemyDamageField.SetValue(damageComponent, true);
        }
        
        // Visual indicator (blue for dagger)
        SpriteRenderer daggerRenderer = daggerAttack.AddComponent<SpriteRenderer>();
        
        // Create larger texture scaled to match collider size
        int textureWidth = Mathf.RoundToInt(daggerWidth * 64);
        int textureHeight = Mathf.RoundToInt(daggerHeight * 64);
        Texture2D daggerTexture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0f, 0.5f, 1f, 0.8f); // Blue color for dagger, more visible
        }
        daggerTexture.SetPixels(pixels);
        daggerTexture.Apply();
        
        daggerRenderer.sprite = Sprite.Create(daggerTexture, new Rect(0, 0, textureWidth, textureHeight), Vector2.one * 0.5f);
        daggerRenderer.sortingOrder = 10;
        
        // Store facing direction
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
        
        // Track attack duration and follow player
        float elapsed = 0f;
        while (elapsed < daggerDuration)
        {
            if (playerTransform != null)
            {
                Vector3 newPosition = playerTransform.position + (Vector3.right * (facingLeft ? -daggerRange : daggerRange));
                daggerAttack.transform.position = newPosition;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(daggerAttack);
    }
    
    private void ThrowDaggerProjectile()
    {
        if (playerTransform == null) return;
        
        // Get mouse position in world space
        Vector3 mousePosition = Vector3.zero;
        if (Mouse.current != null && Camera.main != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Camera.main.nearClipPlane));
            mousePosition.z = 0; // Ensure z is 0 for 2D
        }
        
        // Calculate throw direction from player to mouse
        Vector3 throwDirection = (mousePosition - playerTransform.position).normalized;
        
        // If mouse position is invalid or too close, use facing direction as fallback
        if (throwDirection.magnitude < 0.1f)
        {
            SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = GetComponentInChildren<SpriteRenderer>();
            
            bool facingLeft = playerSprite != null ? playerSprite.flipX : false;
            throwDirection = facingLeft ? Vector3.left : Vector3.right;
        }
        
        // Start position is slightly in front of player in throw direction
        Vector3 startPosition = playerTransform.position + (throwDirection * 0.5f);
        
        StartCoroutine(CreateProjectileDagger(startPosition, throwDirection));
    }
    
    private IEnumerator CreateProjectileDagger(Vector3 startPosition, Vector3 direction)
    {
        // Create projectile dagger (1/3 player size)
        GameObject projectile = new GameObject("DaggerProjectile");
        projectile.transform.position = startPosition;
        
        // Add rigidbody for physics
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.3f; // Reduced gravity for longer horizontal flight
        rb.linearVelocity = direction * projectileSpeed;
        
        // Add collider (smaller size - 1/3 player size)
        BoxCollider2D projectileCollider = projectile.AddComponent<BoxCollider2D>();
        projectileCollider.size = new Vector2(0.4f, 0.6f); // 1/3 player size approximately
        projectileCollider.isTrigger = true;
        
        // Add damage object component
        DamageObject damageComponent = projectile.AddComponent<DamageObject>();
        damageComponent.damageAmount = projectileDamage;
        damageComponent.damageRate = 0.1f;
        
        // Configure damage object
        var excludeField = typeof(DamageObject).GetField("excludePlayerLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeField != null)
        {
            excludeField.SetValue(damageComponent, true);
        }
        
        var enemyDamageField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyDamageField != null)
        {
            enemyDamageField.SetValue(damageComponent, true);
        }
        
        // Visual indicator (smaller blue dagger)
        SpriteRenderer projectileRenderer = projectile.AddComponent<SpriteRenderer>();
        
        // Create texture scaled to match collider size (projectile is smaller)
        int textureWidth = Mathf.RoundToInt(0.4f * 80); // Scale based on collider width (0.4f)
        int textureHeight = Mathf.RoundToInt(0.6f * 80); // Scale based on collider height (0.6f)
        Texture2D projectileTexture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0f, 0.7f, 1f, 0.9f); // Bright blue for projectile, more visible
        }
        projectileTexture.SetPixels(pixels);
        projectileTexture.Apply();
        
        projectileRenderer.sprite = Sprite.Create(projectileTexture, new Rect(0, 0, textureWidth, textureHeight), Vector2.one * 0.5f);
        projectileRenderer.sortingOrder = 10;
        
        // Wait for projectile lifetime
        yield return new WaitForSeconds(projectileLifetime);
        
        // Destroy projectile
        Destroy(projectile);
    }
    
    private void CreateElectricArc()
    {
        if (playerTransform == null || stormParticlePoint == null) return;
        
        // Update particle point position based on facing direction
        UpdateStormParticlePosition();
        
        // Find nearest enemy within range
        GameObject nearestEnemy = FindNearestEnemy(lightningRange);
        if (nearestEnemy == null)
        {
            Debug.Log("No enemy in range for electric arc");
            return;
        }
        
        Vector3 startPos = stormParticlePoint.transform.position;
        Vector3 endPos = nearestEnemy.transform.position;
        
        // Check for obstacles between player and enemy
        if (IsPathBlocked(startPos, endPos))
        {
            Debug.Log("Electric arc blocked by terrain");
            return;
        }
        
        StartCoroutine(CreateLightningArc(startPos, endPos, nearestEnemy));
    }
    
    private void CreateLightningBolt()
    {
        if (playerTransform == null) return;
        
        // Find nearest enemy within bolt range
        GameObject nearestEnemy = FindNearestEnemy(boltRange);
        if (nearestEnemy == null)
        {
            Debug.Log("No enemy in range for lightning bolt");
            return;
        }
        
        Vector3 skyPosition = new Vector3(playerTransform.position.x, playerTransform.position.y + boltHeight, 0);
        Vector3 groundPosition = new Vector3(nearestEnemy.transform.position.x, nearestEnemy.transform.position.y - 0.5f, 0); // Slightly below enemy to touch ground
        
        // Check if ground position is blocked by terrain
        if (IsGroundBlocked(groundPosition))
        {
            Debug.Log("Lightning bolt blocked by terrain");
            return;
        }
        
        StartCoroutine(CreateSkyBolt(skyPosition, groundPosition, nearestEnemy));
    }
    
    private GameObject FindNearestEnemy(float maxRange)
    {
        // Find all objects with EnemyBehavior component
        EnemyBehavior[] enemyBehaviors = FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None);
        if (enemyBehaviors.Length == 0) return null;
        
        GameObject nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (EnemyBehavior enemyBehavior in enemyBehaviors)
        {
            if (enemyBehavior == null || enemyBehavior.gameObject == null) continue;
            
            // Skip if enemy is dead
            if (enemyBehavior.IsDead) continue;
            
            float distance = Vector3.Distance(playerTransform.position, enemyBehavior.transform.position);
            if (distance <= maxRange && distance < nearestDistance)
            {
                nearest = enemyBehavior.gameObject;
                nearestDistance = distance;
            }
        }
        
        return nearest;
    }
    
    private IEnumerator CreateLightningArc(Vector3 startPos, Vector3 endPos, GameObject target)
    {
        // Create lightning arc visual
        GameObject lightning = new GameObject("ElectricArc");
        lightning.transform.position = startPos;
        
        LineRenderer lineRenderer = lightning.AddComponent<LineRenderer>();
        lineRenderer.material = CreateLightningMaterial();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 10; // More points for bending effect
        lineRenderer.sortingOrder = 15;
        
        // Create bending arc points
        Vector3[] arcPoints = CreateBendingArc(startPos, endPos, 10);
        lineRenderer.SetPositions(arcPoints);
        
        // Deal damage to target
        EnemyBehavior enemyBehavior = target.GetComponent<EnemyBehavior>();
        if (enemyBehavior != null)
        {
            enemyBehavior.TakeDamage(lightningDamage);
        }
        
        // Lightning visual effect duration
        yield return new WaitForSeconds(lightningDuration);
        
        Destroy(lightning);
    }
    
    private IEnumerator CreateSkyBolt(Vector3 startPos, Vector3 endPos, GameObject target)
    {
        // Create lightning bolt from sky
        GameObject bolt = new GameObject("LightningBolt");
        bolt.transform.position = startPos;
        
        LineRenderer lineRenderer = bolt.AddComponent<LineRenderer>();
        lineRenderer.material = CreateLightningMaterial();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 6; // More points for subtle bending
        lineRenderer.sortingOrder = 15;
        
        // Create slightly squiggly lightning bolt path
        Vector3[] boltPoints = CreateBendingBolt(startPos, endPos, 6);
        lineRenderer.SetPositions(boltPoints);
        
        // Deal damage to target
        EnemyBehavior enemyBehavior = target.GetComponent<EnemyBehavior>();
        if (enemyBehavior != null)
        {
            enemyBehavior.TakeDamage(boltDamage);
        }
        
        // Create ground impact effect
        GameObject impact = new GameObject("BoltImpact");
        impact.transform.position = endPos;
        
        // Add impact damage area
        BoxCollider2D impactCollider = impact.AddComponent<BoxCollider2D>();
        impactCollider.size = Vector2.one * 2f; // 2x2 impact area
        impactCollider.isTrigger = true;
        
        DamageObject impactDamage = impact.AddComponent<DamageObject>();
        impactDamage.damageAmount = boltDamage / 2; // Half damage for impact area
        impactDamage.damageRate = 0.1f;
        
        // Configure impact damage
        var excludeField = typeof(DamageObject).GetField("excludePlayerLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (excludeField != null)
        {
            excludeField.SetValue(impactDamage, true);
        }
        
        var enemyDamageField = typeof(DamageObject).GetField("canDamageEnemies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyDamageField != null)
        {
            enemyDamageField.SetValue(impactDamage, true);
        }
        
        // Visual impact effect (larger and more visible)
        SpriteRenderer impactRenderer = impact.AddComponent<SpriteRenderer>();
        int impactTextureSize = Mathf.RoundToInt(2f * 80); // Scale based on 2x2 impact area
        Texture2D impactTexture = new Texture2D(impactTextureSize, impactTextureSize);
        Color[] pixels = new Color[impactTextureSize * impactTextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 1f, 0f, 0.8f); // Yellow impact, more visible
        }
        impactTexture.SetPixels(pixels);
        impactTexture.Apply();
        
        impactRenderer.sprite = Sprite.Create(impactTexture, new Rect(0, 0, impactTextureSize, impactTextureSize), Vector2.one * 0.5f);
        impactRenderer.sortingOrder = 10;
        
        // Lightning and impact duration (using serialized parameters)
        yield return new WaitForSeconds(lightningDuration);
        
        Destroy(bolt);
        
        // Impact lasts for bolt duration
        yield return new WaitForSeconds(boltDuration);
        
        Destroy(impact);
    }
    
    private Vector3[] CreateBendingArc(Vector3 start, Vector3 end, int pointCount)
    {
        Vector3[] points = new Vector3[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            
            // Linear interpolation between start and end
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            // Create much more dramatic bending effect
            float bendAmount = Mathf.Sin(t * Mathf.PI) * 2f; // Increased from 0.5f to 2f
            
            // Add both perpendicular and random offsets for more chaotic lightning
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
            
            // Create multiple bend points for more chaotic effect
            float chaos1 = Mathf.Sin(t * Mathf.PI * 3f) * bendAmount * 0.8f;
            float chaos2 = Mathf.Sin(t * Mathf.PI * 5f) * bendAmount * 0.4f;
            float randomBend = Random.Range(-0.5f, 0.5f) * bendAmount;
            
            Vector3 bendOffset = perpendicular * (chaos1 + chaos2 + randomBend);
            
            // Add some vertical chaos too
            Vector3 verticalOffset = Vector3.up * Random.Range(-0.3f, 0.3f) * bendAmount;
            
            points[i] = basePoint + bendOffset + verticalOffset;
        }
        
        return points;
    }
    
    private Vector3[] CreateBendingBolt(Vector3 start, Vector3 end, int pointCount)
    {
        Vector3[] points = new Vector3[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            
            // Linear interpolation between start and end
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            // Add subtle bending effect (much less than arc)
            float bendAmount = Mathf.Sin(t * Mathf.PI) * 0.8f; // Moderate bending
            
            // Add perpendicular offset for slight zigzag
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
            
            // Create subtle zigzag pattern
            float zigzag = Mathf.Sin(t * Mathf.PI * 4f) * bendAmount * 0.3f; // Gentle zigzag
            float randomOffset = Random.Range(-0.1f, 0.1f) * bendAmount; // Small random variation
            
            Vector3 bendOffset = perpendicular * (zigzag + randomOffset);
            
            points[i] = basePoint + bendOffset;
        }
        
        return points;
    }
    
    private Material CreateLightningMaterial()
    {
        // Create a simple lightning material
        Material lightningMat = new Material(Shader.Find("Sprites/Default"));
        lightningMat.color = new Color(0.5f, 0.8f, 1f, 0.9f); // Electric blue
        return lightningMat;
    }
    
    private bool IsPathBlocked(Vector3 startPos, Vector3 endPos)
    {
        // Check for colliders between start and end positions
        // Using LayerMask to check for ground/terrain layers
        int groundLayerMask = LayerMask.GetMask("Ground", "Default"); // Adjust layer names as needed
        
        RaycastHit2D hit = Physics2D.Linecast(startPos, endPos, groundLayerMask);
        if (hit.collider != null)
        {
            // Check if it's a CompositeCollider2D (common for tilemaps)
            CompositeCollider2D compositeCollider = hit.collider.GetComponent<CompositeCollider2D>();
            if (compositeCollider != null)
            {
                Debug.Log("Electric arc blocked by CompositeCollider2D: " + hit.collider.name);
                return true;
            }
            
            // Also check for any other ground colliders
            if (hit.collider.gameObject.name.ToLower().Contains("ground") || 
                hit.collider.gameObject.name.ToLower().Contains("terrain") ||
                hit.collider.gameObject.name.ToLower().Contains("tilemap"))
            {
                Debug.Log("Electric arc blocked by terrain: " + hit.collider.name);
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsGroundBlocked(Vector3 groundPos)
    {
        // Check if the ground position is inside a collider
        int groundLayerMask = LayerMask.GetMask("Ground", "Default");
        
        // Check point collision
        Collider2D groundCollider = Physics2D.OverlapPoint(groundPos, groundLayerMask);
        if (groundCollider != null)
        {
            // Check if it's a CompositeCollider2D
            CompositeCollider2D compositeCollider = groundCollider.GetComponent<CompositeCollider2D>();
            if (compositeCollider != null)
            {
                Debug.Log("Lightning bolt blocked by CompositeCollider2D: " + groundCollider.name);
                return true;
            }
            
            // Check for ground/terrain objects
            if (groundCollider.gameObject.name.ToLower().Contains("ground") || 
                groundCollider.gameObject.name.ToLower().Contains("terrain") ||
                groundCollider.gameObject.name.ToLower().Contains("tilemap"))
            {
                Debug.Log("Lightning bolt blocked by terrain: " + groundCollider.name);
                return true;
            }
        }
        
        return false;
    }
    
    private void UpdateSlotDisplay(int slotIndex)
    {
        ShardType shardType = equippedShards[slotIndex];
        
        if (shardType != ShardType.None && shardSprites.ContainsKey(shardType))
        {
            slotImages[slotIndex].sprite = shardSprites[shardType];
            slotImages[slotIndex].color = Color.white;
        }
        else
        {
            slotImages[slotIndex].sprite = null;
            slotImages[slotIndex].color = Color.clear;
        }
    }
    
    private void UpdateActiveSlotIndicator()
    {
        if (activeSlotIndicator != null)
        {
            RectTransform indicatorRect = activeSlotIndicator.GetComponent<RectTransform>();
            indicatorRect.anchoredPosition = new Vector2(-(activeSlotIndex * (slotSize.x + slotSpacing)), 0);
        }
    }
    
    private bool HasShardEquipped(ShardType shardType)
    {
        return equippedShards[0] == shardType || equippedShards[1] == shardType;
    }
    
    private int GetEmptySlotIndex()
    {
        for (int i = 0; i < equippedShards.Length; i++)
        {
            if (equippedShards[i] == ShardType.None)
                return i;
        }
        return -1; // No empty slots
    }
    
    // Public method to check if weapon menu is open (for PlayerMovement to disable movement)
    public bool IsWeaponMenuOpen()
    {
        return isWeaponMenuOpen;
    }
    
    // Public method to get active weapon type
    public string GetActiveWeaponName()
    {
        ShardType activeShard = equippedShards[activeSlotIndex];
        return activeShard != ShardType.None ? activeShard.ToString() : "None";
    }
}