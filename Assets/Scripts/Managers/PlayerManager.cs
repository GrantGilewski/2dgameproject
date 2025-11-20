using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public Transform playerTransform;
    public int health = 100;
    public string[] inventory = new string[0];

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public SaveData GetSaveData()
    {
        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name; // critical line
                                                             // ... other fields
        return data;
    }



    public void ApplySaveData(SaveData data)
    {
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform not assigned!");
            return;
        }

        playerTransform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
        health = data.health;
        inventory = data.inventory;
        Debug.Log("Applied save data: position " + playerTransform.position);
    }

}
