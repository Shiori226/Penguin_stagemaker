using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointController : MonoBehaviour
{
    [Header("チェックポイント")]
    [SerializeField] private GameObject checkPointPrefab;

    [SerializeField]
    private GameObject platformsRoot;

    [SerializeField] 
    private List<PlatformController> checkPoints = new List<PlatformController>();

    public static Dictionary<PlatformController, CheckPointColorSwitcher> checkPointDict { get; private set; } = new Dictionary<PlatformController, CheckPointColorSwitcher>();

    private void SortCheckPoints()
    {
        checkPoints = checkPoints.Where(t => t != null).ToList(); // null要素は排除
        checkPoints.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
    }

    public void GenerateCheckPointUI(RectTransform rectTransform, Vector3 startPos, float stageDistance)
    {
        // Check Points を昇順にソート
        SortCheckPoints();

        // Progress Bar の高さ
        float progressBarHeight = rectTransform.rect.height;

        foreach (var checkPoint in checkPoints)
        {
            GameObject generatedCheckPoint = Instantiate(checkPointPrefab, transform);

            // ステージ全体の長さから見た Check Point の位置を取得
            float checkPointProgress = Vector3.Distance(checkPoint.transform.position, startPos) / stageDistance;

            // 生成する Check Point に、実際にステージに置かれている Check Point の位置関係を反映
            RectTransform checkPointTransform = generatedCheckPoint.GetComponent<RectTransform>();
            Vector2 checkPointPosition = checkPointTransform.anchoredPosition;
            checkPointPosition.y = progressBarHeight * checkPointProgress;
            checkPointTransform.anchoredPosition = checkPointPosition;

            if (generatedCheckPoint.TryGetComponent(out CheckPointColorSwitcher checkPointUIController))
            {
                // 生成した Check Point をキャッシュしておく
                checkPointDict.Add(checkPoint, checkPointUIController);
            }
        }
    }

    public void Initialize()
    {
        if (platformsRoot == null)
        {
            platformsRoot = GameObject.Find("Platforms");
        }

        PlatformController[] platformControllers = platformsRoot.GetComponentsInChildren<PlatformController>();

        foreach (PlatformController platformController in platformControllers)
        {
            if (platformController.IsCheckPoint)
            {
                checkPoints.Add(platformController);
            }
        }
    }
}
