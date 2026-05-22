using UnityEngine;

public class PopupTextSpawner : MonoBehaviour
{
    public static PopupTextSpawner Instance;

    [SerializeField]
    private PopupTextPoolController pool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    public void SpawnPopupText(Transform target, float baseScore, Color displayColor)
    {
        GameObject scorePopupObject = pool.GetPooledObject();

        // ˆÈ‰ºAƒ‰ƒxƒ‹î•ñ‚Ìİ’è
        if (!scorePopupObject.TryGetComponent(out PopupTextController popupTextController)) { return; }

        // ƒ‰ƒxƒ‹‚Ì¶¬
        popupTextController.Init(target, baseScore.ToString("+0;-0;0"), displayColor);
    }
}
