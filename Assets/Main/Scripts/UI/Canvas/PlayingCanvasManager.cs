using System;
using UnityEngine;
using TMPro;

public class PlayingCanvasManager : MonoBehaviour
{
    [SerializeField]
    private PlayerRespawnController playerRespawnController;

    [SerializeField]
    private TextMeshProUGUI timerText;
    [SerializeField]
    private TextMeshProUGUI scoreText;
    [SerializeField]
    private Transform checkPointRoot;

    public void Initialize()
    {
        if (playerRespawnController == null)
        {
            playerRespawnController = FindObjectOfType<PlayerRespawnController>();
        }

        if(timerText == null)
        {
            timerText = transform.Find("TimeBackImage/TimerText").GetComponent<TextMeshProUGUI>();
        }

        if(scoreText == null)
        {
            scoreText = transform.Find("ScoreBackImage/ScoreText").GetComponent<TextMeshProUGUI>();
        }

        if(checkPointRoot == null)
        {
            checkPointRoot = transform.Find("ProgressBar/CheckPoints");
            Debug.Log(checkPointRoot);
        }

        if (playerRespawnController.GetRespawnMode() == RespawnMode.NearestPlatform)
        {
            foreach (Transform child in checkPointRoot)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public void UpdatePlayingUI(TimeKeeper timeKeeper)
    {
        var timeSpan = TimeSpan.FromSeconds(timeKeeper.GetRemainingTime());
        timerText.text = timeSpan.ToString(@"mm\:ss");
        scoreText.text = ScoreManager.Instance.GetTotalScore().ToString();

        if (playerRespawnController.GetRespawnMode() == RespawnMode.NearestPlatform)
        {
            foreach (Transform child in checkPointRoot)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public void StartWarnBlink()
    {
        timerText.color = Color.red;
    }
}
