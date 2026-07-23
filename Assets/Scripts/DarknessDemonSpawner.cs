using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DarknessDemonSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Jo enemy spawn karna hai (jaise EnemyAiNoPatrol prefab) use yahan daalein")]
    public GameObject demonPrefab; 
    
    [Tooltip("Player se kitni door enemy spawn hoga")]
    public float spawnDistance = 15f; 
    
    private PlayerController player;
    private GameObject activeDemon;
    private bool isSpawning = false;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("DarknessDemonSpawner: PlayerController nahi mila!");
        }
    }

    void Update()
    {
        if (player == null || demonPrefab == null) return;
        
        // Agar player mar gaya hai to naya dushman nahi aayega
        if (player.currentHealth <= 0) return; 

        // Check ki andhera (darkness) hai ya nahi
        bool isDark = false;
        
        // Agar player ke paas torch hai aur uski intensity 0 ho gayi hai
        if (player.hasTorch && player.handLampLight != null)
        {
            if (player.handLampLight.intensity <= 0.05f) 
            {
                isDark = true;
            }
        }

        if (isDark)
        {
            // Agar andhera hai, aur abhi tak koi demon nahi aaya hai, to spawn karo
            if (!isSpawning && activeDemon == null)
            {
                StartCoroutine(SpawnDemonRoutine());
            }
        }
    }

    private IEnumerator SpawnDemonRoutine()
    {
        isSpawning = true;
        
        // Thoda intezaar karo taaki intensity 0 hote hi turant na aa jaye (2 second delay)
        yield return new WaitForSeconds(2f);

        // 2 second baad wapas check karo ki player ne oil to nahi le liya
        if (player.handLampLight != null && player.handLampLight.intensity > 0.05f)
        {
            isSpawning = false;
            yield break; // Player ne oil le liya, isiliye spawn cancel
        }

        // Spawn position nikalo (Player ke thik SAMNE, 25 meter door)
        Vector3 spawnPos = player.transform.position + (player.transform.forward * 25f);
        
        // NavMesh par safe position dhundho
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 10f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // Demon ko spawn karo
        activeDemon = Instantiate(demonPrefab, spawnPos, Quaternion.identity);
        Debug.Log("<color=red>Andhera hone ki wajah se ek Demon saamne se aa raha hai!</color>");

        EnemyAiNoPatrol aiNoPatrol = activeDemon.GetComponent<EnemyAiNoPatrol>();
        if (aiNoPatrol == null) aiNoPatrol = activeDemon.GetComponentInChildren<EnemyAiNoPatrol>();

        // Spawn hote hi chase karna shuru kar de!
        if (aiNoPatrol != null)
        {
            aiNoPatrol.forceChase = true;
        }

        EnemyAi ai = activeDemon.GetComponent<EnemyAi>();
        
        // Jab tak ye demon mar nahi jata, tab tak wait karo aur check karo ki player ne oil to nahi liya
        bool isDemonDead = false;

        while (activeDemon != null && !isDemonDead)
        {
            if (ai != null && ai.isDead) isDemonDead = true;
            if (aiNoPatrol != null && aiNoPatrol.isDead) isDemonDead = true;
            
            // Agar demon abhi zinda hai, check karo ki player ne oil collect kar liya kya?
            if (!isDemonDead && player.handLampLight != null && player.handLampLight.intensity > 0.05f)
            {
                float distance = Vector3.Distance(player.transform.position, activeDemon.transform.position);
                float triggerRad = (aiNoPatrol != null) ? aiNoPatrol.triggerRadius : 15f;
                
                // Agar demon trigger area se bahar hai (player trap nahi hua hai)
                if (distance > triggerRad)
                {
                    Debug.Log("<color=green>Player ne oil collect kar liya, demon gayab ho gaya!</color>");
                    Destroy(activeDemon); // Gayab kar do
                    activeDemon = null;
                    isDemonDead = true; // Loop break karne ke liye
                }
                // Agar trigger area me aa chuka hai (trap ban chuka hai), to gayab nahi hoga, marna hi padega!
            }
            
            yield return null; // Har frame check karo
        }

        // Demon mar gaya ya gayab ho gaya
        activeDemon = null; 
        
        // Naya demon aane se pehle 3 second ka break do
        yield return new WaitForSeconds(3f); 
        
        isSpawning = false;
    }
}
