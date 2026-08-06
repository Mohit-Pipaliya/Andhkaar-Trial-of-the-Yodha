using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, HashSet<GameObject>> pooledObjects = new Dictionary<string, HashSet<GameObject>>();

    public int TotalActiveObjects { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        string poolKey = prefab.name;
        EnsurePool(poolKey);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = poolKey;
            obj.SetActive(false);
            EnqueueUnique(poolKey, obj);
        }
    }

    public void PrewarmProcedural(string poolKey, int count, System.Func<GameObject> factory)
    {
        if (factory == null || count <= 0) return;

        EnsurePool(poolKey);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = factory.Invoke();
            obj.name = poolKey;
            obj.SetActive(false);
            EnqueueUnique(poolKey, obj);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string poolKey = prefab.name;
        EnsurePool(poolKey);

        if (poolDictionary[poolKey].Count > 0)
        {
            GameObject objToSpawn = poolDictionary[poolKey].Dequeue();
            if (objToSpawn != null)
            {
                objToSpawn.transform.SetPositionAndRotation(position, rotation);
                objToSpawn.SetActive(true);
                TotalActiveObjects++;
                return objToSpawn;
            }
        }

        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = poolKey;
        TotalActiveObjects++;
        return newObj;
    }

    public void RegisterProceduralObject(GameObject obj, string poolKey)
    {
        obj.name = poolKey;
        TotalActiveObjects++;
    }

    public GameObject SpawnProcedural(string poolKey, Vector3 position)
    {
        EnsurePool(poolKey);

        if (poolDictionary[poolKey].Count > 0)
        {
            GameObject objToSpawn = poolDictionary[poolKey].Dequeue();
            if (objToSpawn != null)
            {
                objToSpawn.transform.position = position;
                objToSpawn.SetActive(true);
                TotalActiveObjects++;
                return objToSpawn;
            }
        }

        return null;
    }

    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        string poolKey = obj.name;
        obj.SetActive(false);
        TotalActiveObjects--;

        EnsurePool(poolKey);
        EnqueueUnique(poolKey, obj);
    }

    public void Despawn(GameObject obj, float delay)
    {
        StartCoroutine(DespawnCoroutine(obj, delay));
    }

    private IEnumerator DespawnCoroutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(obj);
    }

    private void EnsurePool(string poolKey)
    {
        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary[poolKey] = new Queue<GameObject>();
            pooledObjects[poolKey] = new HashSet<GameObject>();
        }
    }

    private void EnqueueUnique(string poolKey, GameObject obj)
    {
        if (pooledObjects[poolKey].Add(obj))
            poolDictionary[poolKey].Enqueue(obj);
    }
}
