using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAutoSetup : EditorWindow
{
    [MenuItem("Tools/Auto-Fill Player Empty Fields")]
    public static void AutoSetupPlayer()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Scene me PlayerController nahi mila! Pehle Player ko scene me daalein.");
            return;
        }

        // Record for Undo
        Undo.RecordObject(player, "Auto-Fill Player Fields");

        int assignedCount = 0;

        // 1. UI Prompts (Canvas me dhundna)
        if (player.pressOUI == null)
        {
            GameObject ui = GameObject.Find("Press O TO Pickup Lamp "); // The specific name from earlier grep
            if (ui == null) ui = FindUIWithText("Press O");
            if (ui != null) { player.pressOUI = ui; assignedCount++; }
        }
        
        if (player.pressEUI == null)
        {
            GameObject ui = FindUIWithText("Press E");
            if (ui != null) { player.pressEUI = ui; assignedCount++; }
        }

        if (player.pressMUI == null)
        {
            GameObject ui = FindUIWithText("Press M");
            if (ui != null) { player.pressMUI = ui; assignedCount++; }
        }
        
        if (player.pressPut1UI == null)
        {
            GameObject ui = FindUIWithText("Press 1");
            if (ui != null) { player.pressPut1UI = ui; assignedCount++; }
        }

        // 2. Hand Bones
        Transform rightHand = FindDeepChild(player.transform, "RightHand");
        if (rightHand == null) rightHand = FindDeepChild(player.transform, "mixamorig:RightHand");
        
        Transform leftHand = FindDeepChild(player.transform, "LeftHand");
        if (leftHand == null) leftHand = FindDeepChild(player.transform, "mixamorig:LeftHand");

        if (player.handBone == null && leftHand != null) { player.handBone = leftHand; assignedCount++; }
        if (player.otherHandBone == null && rightHand != null) { player.otherHandBone = rightHand; assignedCount++; }

        // 3. Weapons & Items in Hands
        if (player.handSword1Object == null)
        {
            Transform sword1 = FindDeepChild(player.transform, "Sword1");
            if (sword1 == null) sword1 = FindDeepChild(player.transform, "Sword 1");
            if (sword1 != null) { player.handSword1Object = sword1.gameObject; assignedCount++; }
        }
        
        if (player.handSword2Object == null)
        {
            Transform sword2 = FindDeepChild(player.transform, "Sword2");
            if (sword2 == null) sword2 = FindDeepChild(player.transform, "Sword 2");
            if (sword2 != null) { player.handSword2Object = sword2.gameObject; assignedCount++; }
        }

        if (player.handLampObject == null)
        {
            Transform lamp = FindDeepChild(player.transform, "Lamp");
            if (lamp == null) lamp = FindDeepChild(player.transform, "Oil Lamp");
            if (lamp != null) 
            { 
                player.handLampObject = lamp.gameObject; 
                assignedCount++; 
                if (player.handLampLight == null)
                {
                    Light l = lamp.GetComponentInChildren<Light>();
                    if (l != null) { player.handLampLight = l; assignedCount++; }
                }
            }
        }

        // 4. Quest Objects (Gates, Triggers, Special Objects)
        if (player.specialObjects == null || player.specialObjects.Length < 3) player.specialObjects = new GameObject[3];
        if (player.prayTriggers == null || player.prayTriggers.Length < 3) player.prayTriggers = new Transform[3];
        if (player.placeTriggers == null || player.placeTriggers.Length < 3) player.placeTriggers = new Transform[3];
        if (player.gates == null || player.gates.Length < 3) player.gates = new GameObject[3];

        for (int i = 0; i < 3; i++)
        {
            if (player.specialObjects[i] == null)
            {
                GameObject obj = GameObject.Find($"Special Object {i+1}");
                if (obj == null) obj = GameObject.Find($"Stand for Special Object {i+1}");
                if (obj != null) { player.specialObjects[i] = obj; assignedCount++; }
            }

            if (player.prayTriggers[i] == null)
            {
                GameObject obj = GameObject.Find($"Pray Trigger {i+1}");
                if (obj != null) { player.prayTriggers[i] = obj.transform; assignedCount++; }
            }

            if (player.placeTriggers[i] == null)
            {
                GameObject obj = GameObject.Find($"Place Trigger {i+1}");
                if (obj != null) { player.placeTriggers[i] = obj.transform; assignedCount++; }
            }

            if (player.gates[i] == null)
            {
                GameObject obj = GameObject.Find($"Gate {i+1}");
                if (obj == null) obj = GameObject.Find($"Gate{i+1}");
                if (obj != null) { player.gates[i] = obj; assignedCount++; }
            }
        }

        if (assignedCount > 0)
        {
            EditorUtility.SetDirty(player);
            Debug.Log($"<color=green>Success!</color> Player ki {assignedCount} empty fields automatically fill ho gayi hain!");
        }
        else
        {
            Debug.Log("Koi bhi empty field fill nahi hui. Ya toh sab pehle se set hai, ya Scene me UI/Bones nahi mile.");
        }
    }

    private static GameObject FindUIWithText(string textToFind)
    {
        // Search all TextMeshPro texts
        TextMeshProUGUI[] tmpros = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in tmpros)
        {
            if (t.text.Contains(textToFind)) return t.gameObject;
        }

        // Search all Legacy texts
        Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            if (t.text.Contains(textToFind)) return t.gameObject;
        }
        return null;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
