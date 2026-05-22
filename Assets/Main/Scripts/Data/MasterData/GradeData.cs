using System;
using UnityEngine;

[Serializable]
public class GradeData
{
    [Header("•]‰¿ƒOƒŒ[ƒhİ’è")]
    [Range(0.0f, 1.0f)]
    public float gradeThreshold = 0.7f;
    public Color32 displayColor = new Color32(255, 200, 120, 255);
}
