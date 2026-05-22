using System.Collections.Generic;
using UnityEngine;

public class PopupTextPoolController : MonoBehaviour
{
    [SerializeField]
    private GameObject scorePopupObjectPrefab;

    private List<GameObject> pool;
    private readonly int poolSize = 3;

    void Awake()
    {
        pool = new List<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(scorePopupObjectPrefab, transform);
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

        GameObject newObj = Instantiate(scorePopupObjectPrefab, transform);
        newObj.SetActive(true);
        pool.Add(newObj);
        return newObj;
    }
}
