using UnityEngine;

public class HighScoreSoundEffect : MonoBehaviour, IItemEffect
{
    public void ApplyEffect(Collider other, string itemName)
    {
        AudioManager.Instance.playerSe.Play(SeTypePlayer.CaptureGold);
    }
}
