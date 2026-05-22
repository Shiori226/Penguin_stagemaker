using UnityEngine;

/// <summary>
/// CSV�ŏo�͂������f�[�^���܂Ƃ߂Ă����N���X�B
/// �o�͂������f�[�^��ǉ�����ꍇ�́A�����I�ɎQ�Ƃ��w��B
/// </summary>
public class LogDataManager : MonoBehaviour
{
    private Transform playerTransform;

    [StreamData("player_pos")] private Vector3 PlayerPosition => playerTransform != null ? playerTransform.position : Vector3.zero;
    [SnapshotData("score")] private int Score => ScoreManager.Instance.GetTotalScore();
    [SnapshotData("clear_time_bonus")] private int ClearTimeBonusCount => ScoreManager.Instance.GetCount("ClearTimeBonus");
    [SnapshotData("fish_normal")] private int FishNormalCount => ScoreManager.Instance.GetCount("FishNormal");
    [SnapshotData("fish_gold")] private int FishGoldCount => ScoreManager.Instance.GetCount("FishGold");
    [SnapshotData("course_out")] private int CourceOutCount => ScoreManager.Instance.GetCount("CourseOut");

    private void Start()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null) { playerTransform = playerController.transform; }
    }
}
