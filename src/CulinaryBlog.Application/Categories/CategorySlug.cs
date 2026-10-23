using System.Globalization;
using System.Text;

namespace CulinaryBlog.Application.Categories;

public static class CategorySlug
{
    // string.Normalize cannot decompose letters when ICU is absent (the Alpine runtime image sets
    // DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true), so Vietnamese letters are mapped explicitly.
    private static readonly Dictionary<char, char> VietnameseLetters = BuildVietnameseLetters();

    public static string Generate(string name)
    {
        var decomposed = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder();
        var separatorPending = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var normalized = VietnameseLetters.GetValueOrDefault(character, character);
            if (normalized is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (separatorPending && result.Length > 0)
                {
                    result.Append('-');
                }

                result.Append(normalized);
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }

        return result.ToString();
    }

    private static Dictionary<char, char> BuildVietnameseLetters()
    {
        (char Plain, string Variants)[] groups =
        [
            ('a', "àáạảãâầấậẩẫăằắặẳẵ"),
            ('e', "èéẹẻẽêềếệểễ"),
            ('i', "ìíịỉĩ"),
            ('o', "òóọỏõôồốộổỗơờớợởỡ"),
            ('u', "ùúụủũưừứựửữ"),
            ('y', "ỳýỵỷỹ"),
            ('d', "đ"),
        ];

        var map = new Dictionary<char, char>();
        foreach (var (plain, variants) in groups)
        {
            foreach (var variant in variants)
            {
                map[variant] = plain;
                map[char.ToUpperInvariant(variant)] = plain;
            }
        }

        return map;
    }
}
