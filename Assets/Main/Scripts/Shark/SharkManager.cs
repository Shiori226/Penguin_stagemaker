using UnityEngine;

public class SharkManager : MonoBehaviour
{
    [SerializeField]
    private SharkData sharkData = new SharkData(true, 0.3f);

    private void Start()
    {
        SetChildrenActive(sharkData.IsActive);
    }

    private void SetChildrenActive(bool isActive)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }

    public SharkData GetParameter()
    {
        return sharkData;
    }
}
