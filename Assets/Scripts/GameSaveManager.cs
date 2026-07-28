using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    // Saves the player's current state to PlayerPrefs
    public static void SaveGameState(PlayerController player)
    {
        if (player == null) return;

        PlayerPrefs.SetFloat("PlayerHealth", player.currentHealth);
        PlayerPrefs.SetInt("HasSword1", player.hasSword1 ? 1 : 0);
        PlayerPrefs.SetInt("HasSword2", player.hasSword2 ? 1 : 0);
        PlayerPrefs.SetInt("HasTorch", player.hasTorch ? 1 : 0);
        PlayerPrefs.SetInt("CurrentWeapon", (int)player.currentWeapon);

        // Optional: Save current scene index
        PlayerPrefs.SetInt("SavedSceneIndex", SceneManager.GetActiveScene().buildIndex);

        PlayerPrefs.Save();
        Debug.Log("Game Saved Successfully!");
    }

    // Loads the player's state from PlayerPrefs
    public static void LoadGameState(PlayerController player)
    {
        if (player == null) return;

        // Check if a save exists (by checking health)
        if (PlayerPrefs.HasKey("PlayerHealth"))
        {
            player.currentHealth = PlayerPrefs.GetFloat("PlayerHealth");
            
            player.hasSword1 = PlayerPrefs.GetInt("HasSword1", 0) == 1;
            player.hasSword2 = PlayerPrefs.GetInt("HasSword2", 0) == 1;
            player.hasTorch = PlayerPrefs.GetInt("HasTorch", 0) == 1;

            int weaponEnumVal = PlayerPrefs.GetInt("CurrentWeapon", 0);
            player.UpdateWeaponState((PlayerController.WeaponType)weaponEnumVal);

            Debug.Log("Game Loaded Successfully!");
        }

        // --- SPAWN POINT LOGIC (Cross-Scene Door Transitions) ---
        string targetDoor = PlayerPrefs.GetString("TargetSpawnDoor", "");
        if (!string.IsNullOrEmpty(targetDoor))
        {
            // Find that specific door/object in the current scene
            GameObject doorObj = GameObject.Find(targetDoor);
            if (doorObj != null)
            {
                // Teleport Player safely
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false; // Unity requirement for teleporting CC
                
                player.transform.position = doorObj.transform.position;
                // player.transform.rotation = doorObj.transform.rotation; // Uncomment if you want them facing the same way
                
                if (cc != null) cc.enabled = true;
                
                Debug.Log($"Player successfully spawned at door: {targetDoor}");
            }
            else
            {
                Debug.LogWarning($"Target door '{targetDoor}' was not found in the scene! Spawning at default location.");
            }
            
            // Clear the target so it doesn't loop next time they just resume game
            PlayerPrefs.DeleteKey("TargetSpawnDoor");
            PlayerPrefs.Save();
        }
    }

    // Call this if the player dies or wants to restart the game completely
    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey("PlayerHealth");
        PlayerPrefs.DeleteKey("HasSword1");
        PlayerPrefs.DeleteKey("HasSword2");
        PlayerPrefs.DeleteKey("HasTorch");
        PlayerPrefs.DeleteKey("CurrentWeapon");
        PlayerPrefs.DeleteKey("SavedSceneIndex");
        PlayerPrefs.Save();
        Debug.Log("Game Save Cleared!");
    }
}
