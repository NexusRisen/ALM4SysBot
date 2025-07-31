using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PKHeX.Core.AutoMod.AutoMod.Legalization.Analysis.Helpers;

public static class TextFormattingHelper
{
    public class TextSection
    {
        public string Content { get; set; } = "";
        public bool IsHeader { get; set; }
        public bool IsShowdownSet { get; set; }
    }

    private static readonly Regex HeaderPattern = new(@"^#{1,6}\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex CodeBlockPattern = new(@"```[^\n]*\n(.*?)\n```", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new(@"`(.+?)`", RegexOptions.Compiled);
    private static readonly Regex ShowdownMovePattern = new(@"^(\s*)-(\s+\w+.*?)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BulletPointPattern = new(@"^\s*[-*]\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex NumberedListPattern = new(@"^(\d+)\.\s+", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex SectionHeaderPattern = new(@"(==\s*[^=]+\s*==)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex ExcessiveNewlinesPattern = new(@"(\r\n){3,}", RegexOptions.Compiled);
    private static readonly Regex PunctuationBulletPattern = new(@"([.!?])\s*•", RegexOptions.Compiled);
    private static readonly Regex PunctuationDashPattern = new(@"([.!?])\s*-\s+", RegexOptions.Compiled);
    private static readonly Regex NumberedItemPattern = new(@"(\d+)\.\s+", RegexOptions.Compiled);
    private static readonly Regex PunctuationCapitalPattern = new(@"([.!?])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    public static string CleanMarkdownForDisplay(string markdown)
    {
        var cleaned = markdown;

        cleaned = HeaderPattern.Replace(cleaned, "\r\n== $1 ==\r\n");
        cleaned = BoldPattern.Replace(cleaned, "$1");
        cleaned = CodeBlockPattern.Replace(cleaned, "\r\n$1\r\n");
        cleaned = InlineCodePattern.Replace(cleaned, "$1");

        cleaned = ShowdownMovePattern.Replace(cleaned, "SHOWDOWNMOVE$1-$2");
        cleaned = BulletPointPattern.Replace(cleaned, "\r\n• ");
        cleaned = cleaned.Replace("SHOWDOWNMOVE", "");

        cleaned = NumberedListPattern.Replace(cleaned, "\r\n$1. ");
        cleaned = SectionHeaderPattern.Replace(cleaned, "\r\n\r\n$1\r\n");
        cleaned = ExcessiveNewlinesPattern.Replace(cleaned, "\r\n\r\n");
        cleaned = cleaned.Replace("\n", "\r\n");

        return cleaned.Trim();
    }

    public static List<TextSection> SplitIntoSections(string aiResponse)
    {
        var sections = new List<TextSection>();
        var cleaned = CleanMarkdownForDisplay(aiResponse);

        var parts = cleaned.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith("==") || trimmed.EndsWith("==") || trimmed.StartsWith("【"))
            {
                sections.Add(new TextSection { Content = trimmed, IsHeader = true });
            }
            else if (IsShowdownSet(trimmed))
            {
                sections.Add(new TextSection { Content = trimmed + "\r\n", IsShowdownSet = true });
            }
            else
            {
                var formatted = FormatParagraph(trimmed);
                sections.Add(new TextSection { Content = formatted + "\r\n\r\n", IsHeader = false });
            }
        }

        return sections;
    }

    public static bool IsShowdownSet(string text) =>
        text.Contains(" @ ") ||
        (text.Contains("Level:") && text.Contains("Nature")) ||
        (text.Contains("EVs:") || text.Contains("IVs:")) ||
        (text.StartsWith("-") && text.Contains("\n-"));

    public static string FormatParagraph(string text)
    {
        text = PunctuationBulletPattern.Replace(text, "$1\r\n•");
        text = PunctuationDashPattern.Replace(text, "$1\r\n- ");
        text = NumberedItemPattern.Replace(text, "\r\n$1. ");
        text = PunctuationCapitalPattern.Replace(text, "$1 $2");
        text = WhitespacePattern.Replace(text, " ");
        text = text.Trim();

        return text;
    }

    public static bool IsDarkTheme(System.Drawing.Color bgColor)
    {
        var brightness = (bgColor.R * 0.299 + bgColor.G * 0.587 + bgColor.B * 0.114);
        return brightness < 128;
    }
}