using System.Globalization;
using System.Text.RegularExpressions;

public static class StringCaseUtility
{
    public static string ToSpacedWords(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) { return string.Empty; }

        // 1) snake_case を空白に
        string s = input.Replace('_', ' ');

        // 2) 単語境界に空白を挿入
        s = Regex.Replace(
            s,
            @"(?<=[a-z])(?=[A-Z0-9])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=\d)(?=[A-Za-z])|(?<=[A-Za-z])(?=\d)"
            , " "
        );

        // 3) 余分な空白を整える
        s = Regex.Replace(s, @"\s+", " ").Trim();

        // 4) Title Case (先頭大文字) に変換
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }
}