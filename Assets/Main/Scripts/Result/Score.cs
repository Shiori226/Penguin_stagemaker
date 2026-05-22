public class Score
{
    public string Label { get; }
    public int Count { get; private set; }
    public float BaseScore { get;}
    public bool ShowInResult { get; }

    public Score(string label, float baseScore, bool showInResult)
    {
        Label = label;
        Count = 0;
        BaseScore = baseScore;
        ShowInResult = showInResult;
    }

    public void AddCount(int countUp = 1)
    {
        Count += countUp;
    }

    public float GetSubTotal()
    {
        return Count * BaseScore;
    }
}
