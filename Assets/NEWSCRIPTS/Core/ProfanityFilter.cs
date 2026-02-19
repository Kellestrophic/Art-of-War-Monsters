using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class AdvancedProfanityFilter
{
    // =============================
    // 1. BASE BAD WORDS LIST
    // =============================
    private static readonly string[] bannedWords =
    {
        "fuck","shit","bitch","ass","bastard","cunt","whore",
        "slut","dick","pussy","cock","fag","nigger","retard"
    };

    // =============================
    // 2. LEET / UNICODE NORMALIZATION
    // =============================
    private static readonly Dictionary<char, char> normalizationMap =
        new Dictionary<char, char>
        {
            // Leetspeak
            {'0','o'},{'1','i'},{'3','e'},{'4','a'},{'5','s'},{'7','t'},{'8','b'},

            // Unicode lookalikes (small subset, enough to block abuse)
            {'а','a'}, // Cyrillic
            {'е','e'},
            {'і','i'},
            {'о','o'},
            {'ѕ','s'},
            {'р','p'},
            {'с','c'},
            {'у','y'},
            {'ａ','a'}, // Full-width
            {'ｂ','b'},
            {'ｃ','c'},
            {'ｄ','d'},
            {'ｅ','e'},
            {'ｉ','i'},
            {'ｏ','o'},
            {'ｓ','s'},
            {'ｔ','t'},
        };

    // =============================
    // 3. NORMALIZATION FUNCTION
    // =============================
    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        StringBuilder sb = new StringBuilder(input.Length);

        foreach (char c in input.ToLower())
        {
            if (normalizationMap.ContainsKey(c))
                sb.Append(normalizationMap[c]);
            else if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    // =============================
    // 4. REMOVE SPACES / SYMBOLS
    // =============================
    private static string StripSpacing(string input)
    {
        return new string(input.Where(char.IsLetterOrDigit).ToArray());
    }

    // =============================
    // 5. FINAL PROFANITY CHECK
    // =============================
    public static bool ContainsProfanity(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        string cleaned = StripSpacing(name);
        string normalized = Normalize(cleaned);

        foreach (string bad in bannedWords)
        {
            if (normalized.Contains(bad))
                return true;
        }

        return false;
    }
}
