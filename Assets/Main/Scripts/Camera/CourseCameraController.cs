using UnityEngine;

public class CourseCameraController : MonoBehaviour
{
    [Header("Goal Object")]
    [SerializeField]
    private GameObject goalObject;

    [Header("Movement Settings")]
    [SerializeField]
    private float duration = 3.0f;

    private Vector3 startPoint;
    private Vector3 goalPoint;
    private float elapsed = 0f;
    private float fixedY;

    public bool IsInitialized { get; private set; } = false;

    public bool IsIntroductionFinished { get; private set; } = false;

    public void Init()
    {        
        startPoint = transform.position;
        fixedY = startPoint.y;

        // インスペクターで設定されていない場合、"Goal"タグのオブジェクトを探す
        if (goalObject == null)
        {
            goalObject = GameObject.FindWithTag("Goal");
        }

        // goalObjectが見つからない場合はエラーメッセージを表示
        if (goalObject == null)
        {
            goalPoint = startPoint;
            Debug.LogError("Goal object not found. Please assign a GameObject.");
            return;
        }

        goalPoint = goalObject.transform.position;
    }

    public void InitializeState()
    {
        IsIntroductionFinished = false;
        elapsed = 0f;
        IsInitialized = true;
    }

    public void UpdateIntroCamera()
    {
        if(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 lerped = Vector3.Lerp(Vector3.zero, goalPoint, t);
            lerped += startPoint;
            lerped.y = fixedY;

            transform.position = lerped;
        }
        else
        {
            // 目的地に到達したら、位置を固定する
            transform.position = new Vector3(goalPoint.x + startPoint.x, fixedY, goalPoint.z + startPoint.z);
            IsIntroductionFinished = true;
        }
    }

    public float GetDuration()
    {
        return duration;
    }
}
