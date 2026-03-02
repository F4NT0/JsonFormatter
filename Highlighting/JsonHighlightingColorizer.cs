using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JsonFormatter.Highlighting;

public class JsonHighlightingColorizer : DocumentColorizingTransformer
{
    private static readonly IBrush KeyBrush       = new SolidColorBrush(Color.Parse("#FF79C6"));
    private static readonly IBrush StringBrush    = new SolidColorBrush(Color.Parse("#F1FA8C"));
    private static readonly IBrush NumberBrush    = new SolidColorBrush(Color.Parse("#BD93F9"));
    private static readonly IBrush BoolNullBrush  = new SolidColorBrush(Color.Parse("#FF5555"));
    private static readonly IBrush BracketBrush   = new SolidColorBrush(Color.Parse("#8BE9FD"));
    private static readonly IBrush ColonBrush     = new SolidColorBrush(Color.Parse("#FFFFFF"));

    private static readonly Regex TokenRegex = new Regex(
        @"(""(?:[^""\\]|\\.)*"")\s*(:)|" +
        @"(""(?:[^""\\]|\\.)*"")|" +
        @"(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|" +
        @"(true|false|null)|" +
        @"([{}\[\],:])",
        RegexOptions.Compiled);

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0) return;

        var text = CurrentContext.Document.GetText(line.Offset, line.Length);
        var matches = TokenRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            if (match.Groups[1].Success && match.Groups[2].Success)
            {
                // Key (group 1) + colon (group 2)
                ApplyColor(line.Offset, match.Groups[1].Index, match.Groups[1].Length, KeyBrush, true);
                ApplyColor(line.Offset, match.Groups[2].Index, match.Groups[2].Length, ColonBrush, false);
            }
            else if (match.Groups[3].Success)
            {
                // String value
                ApplyColor(line.Offset, match.Groups[3].Index, match.Groups[3].Length, StringBrush, false);
            }
            else if (match.Groups[4].Success)
            {
                // Number
                ApplyColor(line.Offset, match.Groups[4].Index, match.Groups[4].Length, NumberBrush, false);
            }
            else if (match.Groups[5].Success)
            {
                // true / false / null
                ApplyColor(line.Offset, match.Groups[5].Index, match.Groups[5].Length, BoolNullBrush, false);
            }
            else if (match.Groups[6].Success)
            {
                // Brackets, braces, commas
                ApplyColor(line.Offset, match.Groups[6].Index, match.Groups[6].Length, BracketBrush, false);
            }
        }
    }

    private void ApplyColor(int lineOffset, int start, int length, IBrush brush, bool bold)
    {
        ChangeLinePart(lineOffset + start, lineOffset + start + length, element =>
        {
            element.TextRunProperties.SetForegroundBrush(brush);
            if (bold)
            {
                var tf = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, tf.Style, FontWeight.Bold));
            }
        });
    }
}
