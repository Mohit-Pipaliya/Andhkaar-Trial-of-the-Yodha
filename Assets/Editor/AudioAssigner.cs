using UnityEngine;
using UnityEditor;

public class AudioAssigner : MonoBehaviour
{
    [MenuItem("Tools/Assign AAA Audio Clips")]
    public static void AssignAudioClips()
    {
        // 1. Assign Player Audio
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            if (player.playerAudio == null) player.playerAudio = player.gameObject.GetComponent<AudioSource>();
            if (player.playerAudio == null) player.playerAudio = player.gameObject.AddComponent<AudioSource>();

            player.jumpSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Jump.wav");
            player.landingSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Landing.mp3");
            player.walkSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/WalkOnGrass.mp3");
            player.swordDrawSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Sword Unsheathing.mp3");
            player.slashSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Slash.mp3");
            player.damageSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Damage.mp3");
            player.deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Death.mp3");

            EditorUtility.SetDirty(player);
            Debug.Log("<color=green><b>Player Audio Assigned Successfully!</b></color>");
        }

        // 2. Assign EnemyAi Audio
        EnemyAi[] enemies = FindObjectsByType<EnemyAi>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.enemyAudio == null) enemy.enemyAudio = enemy.gameObject.GetComponent<AudioSource>();
            if (enemy.enemyAudio == null) enemy.enemyAudio = enemy.gameObject.AddComponent<AudioSource>();

            enemy.roarSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Growl.mp3");
            enemy.attackSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Attack.mp3");
            enemy.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Impact.mp3");
            enemy.deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Enemy fall down.mp3");
            enemy.footstepSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/EnemyFootstep.mp3");

            EditorUtility.SetDirty(enemy);
            Debug.Log("<color=green><b>EnemyAi Audio Assigned for " + enemy.gameObject.name + "</b></color>");
        }

        // 3. Assign EnemyAiNoPatrol Audio
        EnemyAiNoPatrol[] stationaryEnemies = FindObjectsByType<EnemyAiNoPatrol>(FindObjectsSortMode.None);
        foreach (var enemy in stationaryEnemies)
        {
            if (enemy.enemyAudio == null) enemy.enemyAudio = enemy.gameObject.GetComponent<AudioSource>();
            if (enemy.enemyAudio == null) enemy.enemyAudio = enemy.gameObject.AddComponent<AudioSource>();

            enemy.roarSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Growl.mp3");
            enemy.attackSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Attack.mp3");
            enemy.hitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Monster Impact.mp3");
            enemy.deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Enemy fall down.mp3");
            enemy.footstepSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/EnemyFootstep.mp3");

            EditorUtility.SetDirty(enemy);
            Debug.Log("<color=green><b>EnemyAiNoPatrol Audio Assigned for " + enemy.gameObject.name + "</b></color>");
        }

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
