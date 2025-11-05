using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewJourney()
    {
        SceneManager.LoadScene("StartingScene"); // Loads new game 
    }

    public void ContinueGame()
    {
        // For now, just load the starting scene
        SceneManager.LoadScene("StartingScene");
    }

    public void OpenOptions()
    {
        Debug.Log("Options menu opened!");
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
