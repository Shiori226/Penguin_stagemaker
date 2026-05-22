using UnityEngine;

public interface IInput
{
    public Vector3 Direction { get; set; }
    public bool Submit { get; set; }
    public bool Pause { get; set; }
    public void UpdateInput();
}
