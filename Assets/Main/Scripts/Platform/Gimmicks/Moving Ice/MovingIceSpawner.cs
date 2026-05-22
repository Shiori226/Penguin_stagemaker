using UnityEngine;

public class MovingIceSpawner : MonoBehaviour
{
    [SerializeField]
    private MovingIceData movingIceData;

    private Vector3 spawnPosition;
    private float spawnTimer;

    private GimmickPoolController pool;

    private void Start()
    {
        pool = GetComponentInChildren<GimmickPoolController>();
        spawnPosition = transform.position;
        spawnTimer = movingIceData.SpawnSpan;
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer > movingIceData.SpawnSpan)
        {
            GameObject newMovingIce = pool.GetPooledObject();

            // スポーン位置や移動の情報を入力
            MovingIceController movingIceController = newMovingIce.GetComponent<MovingIceController>();
            movingIceController.Init(spawnPosition, movingIceData.MoveVelocity, movingIceData.AngularVelocity, movingIceData.LifeTime);

            spawnTimer = 0.0f;
        }
    }

    public void ResetSpawner()
    {
        spawnTimer = movingIceData.SpawnSpan;
        GameObject[] pooledObjects = pool.GetAllPooledObjects();
        foreach (var obj in pooledObjects)
        {
            obj.SetActive(false);
        }
    }
}
