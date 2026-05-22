using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField]
    private string itemName = "FishNormal";

    private IItemEffect[] effects;

    private void Start()
    {
        effects = GetComponents<IItemEffect>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(other, itemName);
            }

            Destroy(gameObject);
        }
    }
}
