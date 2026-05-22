using UnityEngine;
using TMPro;

public class ParameterTabSetter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    public void SetLabelText(string labetText)
    {
        label.text = labetText;
    }
}
