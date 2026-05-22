using UnityEngine;

public class DefaultInput : IInput
{
    public Vector3 Direction { get; set; }
    public bool Submit { get; set; }
    public bool Pause { get; set; }

    public void UpdateInput()
    {
        // 移動方向の入力（ポーズ時はゼロ）
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Direction = PauseUtility.IsPause ? Vector3.zero : new Vector3(horizontal, 0.0f, vertical).normalized;

        // サブミット入力（加速時など）（ポーズ時はfalse）
        Submit = PauseUtility.IsPause ? false : Input.GetButtonDown("Submit");

        // ポーズ入力
        Pause = Input.GetButtonDown("Cancel");
    }
}
