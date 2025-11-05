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
    
    // Weapon System
    private enum ShardType { None, ValorShard, WhisperShard, StormShard }
    private ShardType[] equippedShards = new ShardType[2]; // Two slots
    private int activeSlotIndex = 0;
    private bool isWeaponMenuOpen = false;
    
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
    
    // Shard Sprites (will be loaded from the GameObjects)
    private Dictionary<ShardType, Sprite> shardSprites = new Dictionary<ShardType, Sprite>();
    
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
        if (leftClickPressed && !isWeaponMenuOpen)
        {
            UseActiveWeapon();
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
    
    private void UseActiveWeapon()
    {
        ShardType activeShard = equippedShards[activeSlotIndex];
        
        switch (activeShard)
        {
            case ShardType.ValorShard:
                UseValorShard();
                break;
            case ShardType.WhisperShard:
                // TODO: Implement WhisperShard ability
                Debug.Log("WhisperShard ability not implemented yet");
                break;
            case ShardType.StormShard:
                // TODO: Implement StormShard ability
                Debug.Log("StormShard ability not implemented yet");
                break;
            default:
                Debug.Log("No weapon equipped in active slot");
                break;
        }
    }
    
    private void UseValorShard()
    {
        if (playerTransform == null) return;
        
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
        
        // Create larger red rectangle sprite for sword attack
        Texture2D swordTexture = new Texture2D(48, 64); // Larger texture to match new size
        Color[] pixels = new Color[48 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 0f, 0f, 0.6f); // Semi-transparent red
        }
        swordTexture.SetPixels(pixels);
        swordTexture.Apply();
        
        swordRenderer.sprite = Sprite.Create(swordTexture, new Rect(0, 0, 48, 64), Vector2.one * 0.5f);
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