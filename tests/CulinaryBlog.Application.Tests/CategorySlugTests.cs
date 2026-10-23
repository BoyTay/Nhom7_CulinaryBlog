using CulinaryBlog.Application.Categories;

namespace CulinaryBlog.Application.Tests;

// The API container (aspnet:10.0-alpine) runs with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true, where
// string.Normalize does not decompose Vietnamese letters. This project runs in invariant mode too, so
// these tests exercise the same behavior as production.
public sealed class CategorySlugTests
{
    [Theory]
    [InlineData("Món chính", "mon-chinh")]
    [InlineData("Bánh tráng đặc biệt", "banh-trang-dac-biet")]
    [InlineData("Tráng miệng", "trang-mieng")]
    [InlineData("Món hải sản", "mon-hai-san")]
    [InlineData("Món khai vị", "mon-khai-vi")]
    [InlineData("ĐỒ UỐNG CÓ CỒN", "do-uong-co-con")]
    [InlineData("Phở bò & bún bò Huế", "pho-bo-bun-bo-hue")]
    [InlineData("  Gỏi cuốn,   nem rán!  ", "goi-cuon-nem-ran")]
    [InlineData("Ưu đãi Ơn trời Ắc quy", "uu-dai-on-troi-ac-quy")]
    [InlineData("Quả mận Ỳ Ỷ Ỹ Ỵ", "qua-man-y-y-y-y")]
    public void GenerateRemovesVietnameseDiacriticsWithoutDroppingLetters(string name, string expected) =>
        Assert.Equal(expected, CategorySlug.Generate(name));

    [Fact]
    public void GenerateHandlesDecomposedInputTheSameAsComposedInput()
    {
        var composed = "Món chính";
        var decomposed = "Mo\u0302\u0301n chi\u0301nh";

        Assert.Equal(CategorySlug.Generate(composed), CategorySlug.Generate(decomposed));
    }

    [Fact]
    public void GenerateCoversEveryVietnameseLowercaseLetter()
    {
        const string letters =
            "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ";

        var slug = CategorySlug.Generate(letters);

        Assert.Matches("^[a-z]+$", slug);
        Assert.Equal(letters.Length, slug.Length);
    }

    [Fact]
    public void GenerateReturnsEmptyWhenNoLetterOrDigitSurvives() =>
        Assert.Equal(string.Empty, CategorySlug.Generate("!!! ???"));
}
