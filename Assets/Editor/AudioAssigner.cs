using UnityEngine;
using UnityEditor;

public class AudioAssigner : MonoBehaviour
{
    [MenuItem("Tools/Assign AAA Audio Clips")]
    public static void AssignAudioClips()
    {


        // 4. Assign Boss Audio
        BossEnemyAi[] bosses = FindObjectsByType<BossEnemyAi>(FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss.bossAudio == null) boss.bossAudio = boss.gameObject.GetComponent<AudioSource>();
            if (boss.bossAudio == null) boss.bossAudio = boss.gameObject.AddComponent<AudioSource>();

            boss.roarSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Growl 2.mp3");
            boss.attackSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Jump attack.mp3");
            boss.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Impact.mp3");
            boss.deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Collapse.mp3");
            boss.footstepSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Great_Footsteps.mp3");

            EditorUtility.SetDirty(boss);
            Debug.Log("<color=green><b>Boss Audio Assigned for " + boss.gameObject.name + "</b></color>");
        }

        Debug.Log("<color=cyan><b>All AAA Audio Clips have been assigned perfectly!</b></color>");
    }
}
