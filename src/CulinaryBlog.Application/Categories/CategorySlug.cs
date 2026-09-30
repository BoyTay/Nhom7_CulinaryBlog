using System.Globalization;
using System.Text;

namespace CulinaryBlog.Application.Categories;

public static class CategorySlug
{
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

            var normalized = character == 'đ' ? 'd' : character;
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
}
