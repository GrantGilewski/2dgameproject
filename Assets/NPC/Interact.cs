using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    public GameObject promptUI;
    void Awake(){ if (promptUI) promptUI.SetActive(false); }
    public void Show(){ if (promptUI) promptUI.SetActive(true); }
    public void Hide(){ if (promptUI) promptUI.SetActive(false); }
}