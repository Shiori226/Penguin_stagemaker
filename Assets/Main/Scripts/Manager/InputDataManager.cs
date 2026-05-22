using UnityEngine;
using System.Collections.Generic;

public class InputDataManager : MonoBehaviour
{
    public static InputDataManager Instance;

    [HideInInspector]
    public InputData inputData;

    private List<IInput> inputs = new List<IInput> { new DefaultInput() };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        foreach (IInput input in inputs)
        {
            input.UpdateInput();

            // 各入力データを InputData に流し込む（他の入力機構と競合しないように、値を加算して流し込む）
            inputData.direction = (inputData.direction + input.Direction).normalized;
            inputData.submit |= input.Submit;
            Instance.inputData.pause |= input.Pause;
        }
    }

    private void LateUpdate()
    {
        // 次のフレームのためにリセットする
        inputData.ResetInput();
    }

    public void TriggerPauseInput()
    {
        inputData.pause = true;
    }
}
