using UnityEngine;

public enum Scores
{
    None,
    ClearTimeBonus,
    Fish,
    GoldFish,
    CourceOut
}

[System.Serializable]
public class DetailScore
{
    public Scores scoreType;
    public string label;
    public Sprite icon;
}
