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
        LogManager.instance.log("Exit Button Pressed", LogManager.DEBUG);
        Application.Quit();
    }
}
