using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void NewJourney()
    {
        SceneManager.LoadScene("StartingScene");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("StartingScene");
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
