using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;  // For TextMeshPro

public class NPCInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public GameObject interactionPromptUI; // The "Press E" prompt
    public GameObject dialogueBox;         // UI panel holding the dialogue
    [TextArea(2, 5)]
    public string dialogueText = "Hello, traveler!";

    private InputSystem_Actions input;
    private bool playerInRange = false;
    private bool dialogueOpen = false;

    private TMP_Text dialogueTMP; // Reference to the TextMeshPro UI component

    private void Awake()
    {
        input = new InputSystem_Actions();

        // Find the TMP_Text from the dialogue box (child component)
        if (dialogueBox != null)
        {
            dialogueTMP = dialogueBox.GetComponentInChildren<TMP_Text>();
            if (dialogueTMP == null)
            {
                Debug.LogWarning("TMP_Text component not found in dialogueBox!");
            }
        }
        else
        {
            Debug.LogWarning("DialogueBox not assigned in NPCInteract.");
        }
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Update()
    {
        if (playerInRange && input.Player.Interact.triggered)
        {
            HandleInteraction();
        }
    }

    void HandleInteraction()
    {
        if (dialogueBox == null)
        {
            Debug.LogWarning("DialogueBox is not assigned!");
            return;
        }

        if (dialogueOpen)
        {
            dialogueBox.SetActive(false);
            dialogueOpen = false;
        }
        else
        {
            dialogueBox.SetActive(true);
            dialogueOpen = true;

            if (dialogueTMP != null)
            {
                dialogueTMP.text = dialogueText;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(false);

            if (dialogueBox != null)
                dialogueBox.SetActive(false);

            dialogueOpen = false;
        }
    }
}
