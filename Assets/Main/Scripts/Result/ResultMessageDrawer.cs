using UnityEngine;
using TMPro;

public class ResultMessageDrawer : MonoBehaviour
{
    [SerializeField] private TMP_Text resultMessageText;

    [SerializeField] private string clearMessage;
    [SerializeField] private Color clearMessageColor;

    [SerializeField] private string gameOverMessage;
    [SerializeField] private Color gameOverMessageColor;

    public void DrawResultMessage(bool isCleared)
    {
        resultMessageText.text = isCleared ? clearMessage : gameOverMessage;
        resultMessageText.color = isCleared ? clearMessageColor : gameOverMessageColor;
    }
}
