using UnityEngine;

[System.Serializable]
public class InputData
{
    public Vector3 direction;
    public bool submit;
    public bool pause;

    public void ResetInput()
    {
        direction = Vector3.zero;
        submit = false;
        pause = false;
    }
}
