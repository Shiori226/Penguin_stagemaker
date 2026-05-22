using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CheckPointColorSwitcher : MonoBehaviour
{
    [SerializeField]
    private Image image;

    [SerializeField]
    private Color reachedColor;

    [SerializeField]
    private Color unreachedColor;

    public bool isPlayerReached { get; private set; } = false;

    private void Start()
    {
        if(image == null)
        {
            image = transform.GetComponentInChildren<Image>();
        }
    }

    public void SetStatus(bool isReached)
    {
        isPlayerReached = isReached;
        image.color = isReached ? reachedColor : unreachedColor;
    }
}
