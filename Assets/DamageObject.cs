using UnityEngine;

[System.Serializable]
public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage dealt to the player")]
    public int damageAmount = 20;
    
    [Tooltip("Time interval between damage applications (in seconds)")]
    public float damageRate = 1f;
    
    [Tooltip("Apply damage when player enters trigger")]
    [SerializeField] private bool damageOnTrigger = true;
    
    [Tooltip("Apply damage when player collides")]
    [SerializeField] private bool damageOnCollision = true;
    

    
    private bool playerInside = false;
    private float lastDamageTime = -1f;
    private PlayerMovement playerMovement;
    
    void Start()
    {
        // Ensure we have a collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
        }
        
        // Find player reference
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }
    
    void Update()
    {
        // Apply continuous damage while player is inside
        if (playerInside && Time.time - lastDamageTime >= damageRate)
        {
            DealDamage();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (damageOnTrigger && other.CompareTag("Player"))
        {
            playerInside = true;
            DealDamage();
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (damageOnTrigger && other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (damageOnCollision && collision.gameObject.CompareTag("Player"))
        {
            playerInside = true; // Set playerInside so continuous damage works
            DealDamage();
        }
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        if (damageOnCollision && collision.gameObject.CompareTag("Player"))
        {
            playerInside = false; // Stop continuous damage
        }
    }
    
    private void DealDamage()
    {
        if (playerMovement != null && Time.time - lastDamageTime >= damageRate)
        {
            playerMovement.TakeDamageFromObject(damageAmount);
            lastDamageTime = Time.time;
        }
    }
    
    /// <summary>
    /// Public method to deal damage externally (useful for scripted events)
    /// </summary>
    public void TriggerDamage()
    {
        DealDamage();
    }
    
    /// <summary>
    /// Change damage amount at runtime
    /// </summary>
    public void SetDamageAmount(int newAmount)
    {
        damageAmount = newAmount;
    }
    
    /// <summary>
    /// Change damage rate at runtime
    /// </summary>
    public void SetDamageRate(float newRate)
    {
        damageRate = newRate;
    }
}