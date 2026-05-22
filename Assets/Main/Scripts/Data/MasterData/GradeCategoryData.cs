using System;
using UnityEngine;

[Serializable]
public class GradeCategoryData
{
    [Header("ƒOƒŒ[ƒh•]‰¿İ’è")]
    public GradeEvaluationType evaluationType = GradeEvaluationType.NotEvaluate; // ‚‚¢‚Ù‚Ç•]‰¿‚ª‚‚¢ or ’á‚¢‚Ù‚Ç•]‰¿‚ª‚‚¢
    [Range(0.0f, 1.0f)]
    public float gradeWeight = 0.0f; // •]‰¿‚Ö‚Ìd‚İ•t‚¯
}
