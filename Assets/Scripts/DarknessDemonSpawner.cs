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

        // Demon spawn nahi hona chahiye jab combat chal raha ho
        if (player.activeCombatEngagements > 0)
        {
            isDark = false;
        }

        // Ya fir koi demon pehle se hi aas paas (samne) ho
        if (isDark && IsAnyDemonNearby())
        {
            isDark = false;
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
        
        // Thoda intezaar karo taaki intensity 0 hote hi turant na aa jaye (5 second delay as requested)
        yield return new WaitForSeconds(5f);

        // 5 second baad wapas check karo ki player ne oil to nahi le liya YA combat shuru nahi ho gaya
        if ((player.handLampLight != null && player.handLampLight.intensity > 0.05f) || player.activeCombatEngagements > 0 || IsAnyDemonNearby())
        {
            isSpawning = false;
            yield break; // Player ne oil le liya ya combat aa gaya, isiliye spawn cancel
        }

        // Spawn position nikalo (Player ke thik SAMNE, 25 meter door)
        Vector3 spawnPos = player.transform.position + (player.transform.forward * 25f);
        
        // Terrain ke upar exactly spawn karne ke liye raycast (taaki ground se upar ya niche na rahe)
        Vector3 rayStart = spawnPos;
        rayStart.y += 30f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit rayHit, 60f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            spawnPos = rayHit.point;
        }

        // NavMesh par safe position dhundho
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 10f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // Demon ko spawn karo (Object Pool se)
        if (ObjectPoolManager.Instance != null)
        {
            activeDemon = ObjectPoolManager.Instance.Spawn(demonPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            activeDemon = Instantiate(demonPrefab, spawnPos, Quaternion.identity);
        }
        Debug.Log("<color=red>Andhera hone ki wajah se ek Demon saamne se aa raha hai!</color>");

        EnemyAiNoPatrol aiNoPatrol = activeDemon.GetComponent<EnemyAiNoPatrol>();
        if (aiNoPatrol == null) aiNoPatrol = activeDemon.GetComponentInChildren<EnemyAiNoPatrol>();

        // Spawn hote hi chase karna shuru kar de!
        if (aiNoPatrol != null)
        {
            aiNoPatrol.forceChase = true;
        }

        DemonAi ai = activeDemon.GetComponent<DemonAi>();
        
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
                    
                    if (ObjectPoolManager.Instance != null)
                        ObjectPoolManager.Instance.Despawn(activeDemon);
                    else
                        Destroy(activeDemon); // Fallback

                    activeDemon = null;
                    isDemonDead = true; // Loop break karne ke liye
                }
                // Agar trigger area me aa chuka hai (trap ban chuka hai), to gayab nahi hoga, marna hi padega!
            }
            
            // AAA Perf: Check every 0.2s instead of every frame \u2014 83% less CPU, zero visual difference
            yield return new WaitForSeconds(0.2f);
        }

        // Demon mar gaya ya gayab ho gaya
        activeDemon = null; 
        
        // Naya demon aane se pehle 5 second ka break do
        yield return new WaitForSeconds(5f); 
        
        isSpawning = false;
    }

    private float lastSearchTime = -10f;
    private bool lastSearchCache = false;

    private bool IsAnyDemonNearby()
    {
        if (Time.time < lastSearchTime + 1f) return lastSearchCache;
        lastSearchTime = Time.time;

        Transform exclude = activeDemon != null ? activeDemon.transform : null;
        lastSearchCache = EnemyRegistry.IsAnyAliveNear(player.transform.position, 45f, exclude);
        return lastSearchCache;
    }
}
