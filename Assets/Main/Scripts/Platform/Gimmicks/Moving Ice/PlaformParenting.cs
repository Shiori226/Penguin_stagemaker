using UnityEngine;

public class PlaformParenting : MonoBehaviour
{
    void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Player"))
            other.transform.SetParent(transform.GetChild(0));
    }
    void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Player"))
            other.transform.SetParent(null);
    }
}
