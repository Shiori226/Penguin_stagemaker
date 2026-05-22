using UnityEngine;

public class SharkController : MonoBehaviour
{
    private SharkData sharkData;

    private void Start()
    {
        sharkData = transform.GetComponentInParent<SharkManager>().GetParameter();
    }

    private void Update()
    {
        Move(sharkData.Speed);
    }

    private void Move(float speed)
    {
        transform.Translate(Vector3.up * Time.deltaTime * speed);
    }
}
