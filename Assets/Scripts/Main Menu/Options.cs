using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    public GameObject OptionsPanel;

    public void OpenOptions()
    {
        Debug.Log("OpenOptions called!");
        OptionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        OptionsPanel.SetActive(false);
    }
}
