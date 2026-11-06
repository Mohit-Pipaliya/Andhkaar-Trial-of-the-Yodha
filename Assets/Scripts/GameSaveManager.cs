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

        // Ensure weapon isn't saved as None if player has collected a sword
        int weaponEnumVal = (int)player.currentWeapon;
        if (weaponEnumVal == 0)
        {
            if (player.hasSword2) weaponEnumVal = (int)PlayerController.WeaponType.Sword2;
            else if (player.hasSword1) weaponEnumVal = (int)PlayerController.WeaponType.Sword1;
        }
        PlayerPrefs.SetInt("CurrentWeapon", weaponEnumVal);

        if (player.handLampLight != null)
        {
            PlayerPrefs.SetFloat("LampIntensity", player.handLampLight.intensity);
        }

        // Save current scene index
        PlayerPrefs.SetInt("SavedSceneIndex", SceneManager.GetActiveScene().buildIndex);

        PlayerPrefs.Save();
        Debug.Log($"[GameSaveManager] Saved state: Torch={player.hasTorch}, Sword1={player.hasSword1}, Sword2={player.hasSword2}, Weapon={weaponEnumVal}, Health={player.currentHealth}");
    }

    // Loads the player's state from PlayerPrefs
    public static void LoadGameState(PlayerController player)
    {
        if (player == null) return;

        // Check if any saved equipment or player state exists
        if (PlayerPrefs.HasKey("HasTorch") || PlayerPrefs.HasKey("HasSword1") || PlayerPrefs.HasKey("HasSword2") || PlayerPrefs.HasKey("CurrentWeapon") || PlayerPrefs.HasKey("PlayerHealth"))
        {
            if (PlayerPrefs.HasKey("PlayerHealth"))
            {
                float savedHealth = PlayerPrefs.GetFloat("PlayerHealth");
                player.currentHealth = savedHealth > 0 ? savedHealth : player.maxHealth;
            }
            
            player.hasSword1 = PlayerPrefs.GetInt("HasSword1", 0) == 1;
            player.hasSword2 = PlayerPrefs.GetInt("HasSword2", 0) == 1;
            player.hasTorch  = PlayerPrefs.GetInt("HasTorch", 0) == 1;

            int weaponEnumVal = PlayerPrefs.GetInt("CurrentWeapon", 0);
            if (!player.hasSword1 && !player.hasSword2)
            {
                weaponEnumVal = 0;
            }
            else if (weaponEnumVal == 0)
            {
                if (player.hasSword2) weaponEnumVal = (int)PlayerController.WeaponType.Sword2;
                else if (player.hasSword1) weaponEnumVal = (int)PlayerController.WeaponType.Sword1;
            }
            player.currentWeapon = (PlayerController.WeaponType)weaponEnumVal;
            player.UpdateWeaponState(player.currentWeapon);

            if (player.hasTorch && player.handLampLight != null && PlayerPrefs.HasKey("LampIntensity"))
            {
                float savedIntensity = PlayerPrefs.GetFloat("LampIntensity");
                player.handLampLight.intensity = savedIntensity > 0 ? savedIntensity : player.maxLampIntensity;
            }

            Debug.Log($"[GameSaveManager] Loaded state: Torch={player.hasTorch}, Sword1={player.hasSword1}, Sword2={player.hasSword2}, Weapon={player.currentWeapon}");
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
        PlayerPrefs.DeleteKey("LampIntensity");
        PlayerPrefs.DeleteKey("SavedSceneIndex");
        PlayerPrefs.DeleteKey("IsCrossSceneTeleport");
        PlayerPrefs.DeleteKey("SkipMenuOnRetry");
        PlayerPrefs.Save();
        Debug.Log("Game Save Cleared!");
    }
}
