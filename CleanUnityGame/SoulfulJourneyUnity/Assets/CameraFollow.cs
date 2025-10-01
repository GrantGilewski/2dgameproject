using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The player to follow
    
    [Header("Screen Bounds")]
    [SerializeField] private float leftBound = 2f;    // Distance from left edge before camera moves
    [SerializeField] private float rightBound = 2f;   // Distance from right edge before camera moves
    [SerializeField] private float topBound = 1.5f;   // Distance from top edge before camera moves
    [SerializeField] private float bottomBound = 1.5f; // Distance from bottom edge before camera moves
    
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 2f;  // How fast camera follows player
    [SerializeField] private bool followX = true;     // Follow player on X axis
    [SerializeField] private bool followY = true;     // Follow player on Y axis
    
    [Header("Offset")]
    [SerializeField] private Vector3 offset = Vector3.zero; // Offset from player position
    
    private Camera cam;
    private Vector3 targetPosition;
    private bool isFollowing = false;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        // If no target assigned, try to find player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        
        // Initialize target position to current camera position
        targetPosition = transform.position;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        CheckScreenBounds();
        
        if (isFollowing)
        {
            UpdateCameraPosition();
        }
    }
    
    private void CheckScreenBounds()
    {
        // Convert player world position to viewport position (0-1 range)
        Vector3 viewportPos = cam.WorldToViewportPoint(target.position);
        
        bool shouldFollow = false;
        
        // Check if player is outside the defined bounds
        if (followX)
        {
            float leftViewport = leftBound / (cam.orthographicSize * 2f * cam.aspect);
            float rightViewport = 1f - (rightBound / (cam.orthographicSize * 2f * cam.aspect));
            
            if (viewportPos.x < leftViewport || viewportPos.x > rightViewport)
            {
                shouldFollow = true;
            }
        }
        
        if (followY)
        {
            float bottomViewport = bottomBound / (cam.orthographicSize * 2f);
            float topViewport = 1f - (topBound / (cam.orthographicSize * 2f));
            
            if (viewportPos.y < bottomViewport || viewportPos.y > topViewport)
            {
                shouldFollow = true;
            }
        }
        
        isFollowing = shouldFollow;
    }
    
    private void UpdateCameraPosition()
    {
        // Calculate desired camera position
        Vector3 desiredPosition = target.position + offset;
        
        // Maintain current position for axes we're not following
        if (!followX)
            desiredPosition.x = transform.position.x;
        if (!followY)
            desiredPosition.y = transform.position.y;
            
        // Always keep the same Z position for 2D camera
        desiredPosition.z = transform.position.z;
        
        // Smoothly move camera towards target
        targetPosition = Vector3.Lerp(targetPosition, desiredPosition, followSpeed * Time.deltaTime);
        transform.position = targetPosition;
    }
    
    // Method to manually center camera on player
    [ContextMenu("Center on Player")]
    public void CenterOnPlayer()
    {
        if (target != null)
        {
            Vector3 newPosition = target.position + offset;
            newPosition.z = transform.position.z;
            transform.position = newPosition;
            targetPosition = newPosition;
        }
    }
    
    // Method to set new target at runtime
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // Gizmos to visualize bounds in Scene view
    void OnDrawGizmosSelected()
    {
        if (cam == null) cam = GetComponent<Camera>();
        
        // Calculate screen bounds in world space
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        
        Vector3 center = transform.position;
        
        // Draw outer bounds (screen edges)
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, new Vector3(width, height, 0));
        
        // Draw inner bounds (follow trigger area)
        float innerWidth = width - (leftBound + rightBound);
        float innerHeight = height - (topBound + bottomBound);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(innerWidth, innerHeight, 0));
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
}