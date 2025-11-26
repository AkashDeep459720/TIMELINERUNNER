using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pool
{
    public string tag;
    public GameObject prefab;
    public int size = 10;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    [Header("Pools")]
    public List<Pool> pools;

    // runtime dictionary
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"❌ Pool with tag {tag} doesn’t exist.");
            return null;
        }

        Queue<GameObject> poolQueue = poolDictionary[tag];
        GameObject objectToSpawn = null;

        // Try to find an inactive object in the pool
        foreach (var obj in poolQueue)
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                objectToSpawn = obj;
                break;
            }
        }

        // If all are in use → expand pool
        if (objectToSpawn == null)
        {
            Pool poolConfig = pools.Find(p => p.tag == tag);
            if (poolConfig != null && poolConfig.prefab != null)
            {
                objectToSpawn = Instantiate(poolConfig.prefab);
                objectToSpawn.SetActive(false);
                objectToSpawn.transform.SetParent(this.transform);
                poolQueue.Enqueue(objectToSpawn);
                Debug.Log($"➕ Expanded pool: {tag}, new size = {poolQueue.Count}");
            }
            else
            {
                Debug.LogWarning($"❌ No prefab found for pool {tag}");
                return null;
            }
        }

        // Activate and place object
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        if (parent != null) objectToSpawn.transform.SetParent(parent);
        else objectToSpawn.transform.SetParent(this.transform);

        return objectToSpawn;
    }

    /// <summary>
    /// Return an object back to its pool (simply deactivates it).
    /// Safe to call from CollectCoin, PowerUps, etc.
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        obj.transform.SetParent(this.transform); // keep hierarchy tidy
    }
}
