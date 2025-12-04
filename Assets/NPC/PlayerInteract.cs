using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1f;
    [SerializeField] private LayerMask interactLayer;

    private InputAction interactAction;

    private void OnEnable()
    {
        // Find and enable the Interact action
        var input = new InputSystem_Actions(); // This auto-generated class comes from your .inputactions file
        input.Player.Enable();
        interactAction = input.Player.Interact;
        interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        // Check for NPCs in range
        Collider2D npc = Physics2D.OverlapCircle(transform.position, interactRadius, interactLayer);
        if (npc != null)
        {
            Debug.Log("Interacting with: " + npc.name);
            npc.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

