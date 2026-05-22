using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [SerializeField]
    private bool isReached = false;
    public bool IsReached { get { return isReached; } }

    [SerializeField]
    private bool isCheckPoint = false;
    public bool IsCheckPoint { get { return isCheckPoint; } }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isReached = true;

            if (CheckPointController.checkPointDict.TryGetValue(this, out var checkPointUIController))
            {
                checkPointUIController.SetStatus(isReached);
            }
        }
    }
}
