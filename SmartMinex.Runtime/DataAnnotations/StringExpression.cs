//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: StringExpression – Расширение операций со строками.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System.Text.RegularExpressions;
    #endregion Using

    /// <summary> Расширение операций со строками.</summary>
    public static class StringExpression
    {
        public static string[] SplitArguments(this string s) =>
            Regex.Matches(s, @"#(?=\d)|(?<=#)\d+|("".*?"")|[=<>]+\s*|(?<=^|\s|[=<>]).*?(?=\s|[=<>]|$)").Cast<Match>()
                .Select(m => m.Value.Trim()).Where(v => v != string.Empty).ToArray();
    }
}