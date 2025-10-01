using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Detection")]
    public LayerMask groundLayerMask = 1;
    public float groundCheckDistance = 0.1f;
    
    [Header("Sprite Animation")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite walkingSprite1;
    [SerializeField] private Sprite walkingSprite2;
    [SerializeField] private Sprite fallingSprite;
    [SerializeField] private float animationSpeed = 0.2f; // Time between frame changes

    // Components
    private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D playerCollider;
    private SpriteRenderer spriteRenderer;
    
    [Header("Sprite Scaling")]
    [SerializeField] private Transform spriteTransform; // Reference to sprite child object
    [SerializeField] private Vector3 spriteScale = Vector3.one; // Custom sprite scale

    // Input
    private float horizontalInput;
    private bool jumpInput;
    private bool leftInput;
    private bool rightInput;
    private bool downInput;

    // State
    private bool isGrounded;
    private float animationTimer = 0f;
    private bool useFirstWalkSprite = true;

    private GameObject currentPlatform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        
        // Try to get SpriteRenderer from child object first, then from this object
        if (spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        else
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevent player from rotating when falling off edges
        rb.freezeRotation = true;
        
        // Apply custom sprite scale if sprite transform is assigned
        ApplySpriteScale();
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        HandleMovement();
        UpdateSprite();
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
            // Jump - spacebar, W key, and up arrow
            jumpInput = Keyboard.current.spaceKey.isPressed ||
                       Keyboard.current.wKey.isPressed ||
                       Keyboard.current.upArrowKey.isPressed;
            
            // Set horizontal input
            if (leftInput)
                horizontalInput = -1f;
            else if (rightInput)
                horizontalInput = 1f;
        }
    }
    
    private void ApplySpriteScale()
    {
        if (spriteTransform != null)
        {
            spriteTransform.localScale = spriteScale;
        }
    }
    
    // Call this method in inspector or at runtime to update sprite scale
    [ContextMenu("Apply Sprite Scale")]
    public void UpdateSpriteScale()
    {
        ApplySpriteScale();
    }
    
    private void HandleMovement()
    {
        // Horizontal movement
        float targetVelocityX = horizontalInput * moveSpeed;
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        
        // Sprite flipping
        if (spriteRenderer != null)
        {
            if (horizontalInput < -0.1f)
                spriteRenderer.flipX = true; // Face left
            else if (horizontalInput > 0.1f)
                spriteRenderer.flipX = false; // Face right
        }
        
        // Jumping
        if (jumpInput && isGrounded)
        {
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
        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        float checkDistance = groundCheckDistance;
        float playerWidth = playerCollider.size.x;
        
        // Create multiple ground check positions across the player's width
        Vector2 leftCheckPos = new Vector2(boxCenter.x - playerWidth * 0.4f, boxCenter.y - (playerCollider.size.y * 0.5f) - 0.05f);
        Vector2 centerCheckPos = new Vector2(boxCenter.x, boxCenter.y - (playerCollider.size.y * 0.5f) - 0.05f);
        Vector2 rightCheckPos = new Vector2(boxCenter.x + playerWidth * 0.4f, boxCenter.y - (playerCollider.size.y * 0.5f) - 0.05f);
        
        // Check ground at multiple points
        bool leftGrounded = CheckGroundAtPosition(leftCheckPos, checkDistance);
        bool centerGrounded = CheckGroundAtPosition(centerCheckPos, checkDistance);
        bool rightGrounded = CheckGroundAtPosition(rightCheckPos, checkDistance);
        
        // Player is grounded if ANY of the check points hit ground
        isGrounded = leftGrounded || centerGrounded || rightGrounded;
    }
    
    private bool CheckGroundAtPosition(Vector2 position, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(position, Vector2.down, distance, groundLayerMask);
        
        foreach (RaycastHit2D hit in hits)
        {
            // Skip our own colliders
            if (hit.collider != playerCollider && hit.collider.gameObject != gameObject)
            {
                return true;
            }
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