using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    [Header("Core Game Settings")]
    [SerializeField] private bool gameIsPaused = false;
    
    private PlayerMovement playerMovement;
    
    void Start()
    {
        // Find the player in the scene
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        
        if (playerMovement == null)
        {
            LogManager.instance.log("GameController: No PlayerMovement found in scene!", LogManager.ERROR);
        }
        
        // Initialize core game systems
        InitializeGame();
    }

    void Update()
    {
        // Handle core game logic (pause, menu, etc.)
        HandleGameInput();
    }
    
    private void InitializeGame()
    {
        // TODO: Initialize game state, UI, score, etc.
        LogManager.instance.log("Game initialized!", LogManager.INFO);
    }
    
    private void HandleGameInput()
    {
        // Handle pause menu, game state changes, etc. using new Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }
    
    private void TogglePause()
    {
        gameIsPaused = !gameIsPaused;
        Time.timeScale = gameIsPaused ? 0f : 1f;
        LogManager.instance.log($"Game {(gameIsPaused ? "Paused" : "Resumed")}",LogManager.INFO);
    }
    
    // Public methods for other systems
    public bool IsGamePaused() => gameIsPaused;
    
    public void RestartLevel()
    {
        // TODO: Implement level restart logic
        LogManager.instance.log("Level restarted!", LogManager.INFO);
    }
    
    public void GameOver()
    {
        // TODO: Implement game over logic
        LogManager.instance.log("Game Over!", LogManager.INFO);
    }
}
