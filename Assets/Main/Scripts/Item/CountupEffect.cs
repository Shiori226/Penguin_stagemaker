using UnityEngine;

public class CountupEffect : MonoBehaviour, IItemEffect
{
    public void ApplyEffect(Collider other, string itemName)
    {
        AudioManager.Instance.playerSe.Play(SeTypePlayer.Capture);

        ScoreManager.Instance.AddCount(itemName);

        DataManager.Instance.scoreData.TryGetValue(itemName, out ScoreData fishData);

        if (fishData == null)
        {
            Debug.LogError($"ãõÇÃÉfÅ[É^Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒ: {itemName}");
            return;
        }

        float baseScore = fishData.baseScore;

        Color32 displayColor = fishData.displayColor;

        PopupTextSpawner.Instance.SpawnPopupText(other.transform, baseScore, displayColor);
    }
}
