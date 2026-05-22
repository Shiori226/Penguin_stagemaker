using System.Collections.Generic;
using UnityEngine;

public class GimmickPoolController : MonoBehaviour
{
    [SerializeField]
    private GameObject gimmickObjectPrefab;

    private List<GameObject> pool;
    private readonly int poolSize = 5;

    private void Awake()
    {
        pool = new List<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(gimmickObjectPrefab, transform);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject GetPooledObject()
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(gimmickObjectPrefab, transform);
        newObj.SetActive(true);
        pool.Add(newObj);
        return newObj;
    }

    public GameObject[] GetAllPooledObjects()
    {
        return pool.ToArray();
    }
}
