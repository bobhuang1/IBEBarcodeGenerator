using System.Reflection;

namespace IBEBarcode.Localization.Tests;

/// <summary>
/// The desktop app picks its language from <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>
/// and the Web app from the browser's language list, so both go through the same tag parsing.
/// These tests pin that parsing down, then check that every table is complete.
/// </summary>
public class AppLanguageTests
{
    [Theory]
    [InlineData(null, AppLanguage.English)]
    [InlineData("", AppLanguage.English)]
    [InlineData("   ", AppLanguage.English)]
    [InlineData("en", AppLanguage.English)]
    [InlineData("en-US", AppLanguage.English)]
    [InlineData("en-GB", AppLanguage.English)]
    [InlineData("fr-FR", AppLanguage.English)]
    [InlineData("zh", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-CN", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-Hans", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-Hans-CN", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-SG", AppLanguage.SimplifiedChinese)]
    [InlineData("ZH-cn", AppLanguage.SimplifiedChinese)]
    [InlineData("zh-TW", AppLanguage.TraditionalChinese)]
    [InlineData("zh-HK", AppLanguage.TraditionalChinese)]
    [InlineData("zh-MO", AppLanguage.TraditionalChinese)]
    [InlineData("zh-Hant", AppLanguage.TraditionalChinese)]
    [InlineData("zh-Hant-TW", AppLanguage.TraditionalChinese)]
    [InlineData("ja", AppLanguage.Japanese)]
    [InlineData("ja-JP", AppLanguage.Japanese)]
    public void FromCultureName_MapsKnownTags(string? cultureName, AppLanguage expected) =>
        Assert.Equal(expected, AppLanguageExtensions.FromCultureName(cultureName));

    [Fact]
    public void FromPreferredLanguages_NullYieldsEnglish() =>
        Assert.Equal(AppLanguage.English, AppLanguageExtensions.FromPreferredLanguages(null));

    [Fact]
    public void FromPreferredLanguages_EmptyListYieldsEnglish() =>
        Assert.Equal(AppLanguage.English, AppLanguageExtensions.FromPreferredLanguages([]));

    [Fact]
    public void FromPreferredLanguages_SkipsUnknownTagsAndTakesTheFirstMatch() =>
        Assert.Equal(
            AppLanguage.Japanese,
            AppLanguageExtensions.FromPreferredLanguages(["fr-FR", "de", "ja-JP", "zh-CN"]));

    [Fact]
    public void FromPreferredLanguages_ExplicitEnglishStopsTheSearch() =>
        Assert.Equal(
            AppLanguage.English,
            AppLanguageExtensions.FromPreferredLanguages(["en-US", "zh-CN"]));

    [Fact]
    public void FromPreferredLanguages_StripsQualityValues() =>
        Assert.Equal(
            AppLanguage.TraditionalChinese,
            AppLanguageExtensions.FromPreferredLanguages(["zh-TW;q=0.9", "en;q=0.8"]));

    [Theory]
    [InlineData(AppLanguage.English, "en")]
    [InlineData(AppLanguage.SimplifiedChinese, "zh-Hans")]
    [InlineData(AppLanguage.TraditionalChinese, "zh-Hant")]
    [InlineData(AppLanguage.Japanese, "ja")]
    public void CultureTag_IsTheExpectedBcp47Tag(AppLanguage language, string expected) =>
        Assert.Equal(expected, language.CultureTag());

    [Fact]
    public void AllLanguages_CoversEveryEnumValue()
    {
        Assert.Equal(
            Enum.GetValues<AppLanguage>().OrderBy(value => value),
            AppLanguageExtensions.AllLanguages.OrderBy(value => value));
        Assert.Equal(4, AppLanguageExtensions.AllLanguages.Distinct().Count());
    }

    [Theory]
    [InlineData(AppLanguage.English, "English")]
    [InlineData(AppLanguage.SimplifiedChinese, "简体中文")]
    [InlineData(AppLanguage.TraditionalChinese, "繁體中文")]
    [InlineData(AppLanguage.Japanese, "日本語")]
    public void DisplayName_IsTheEndonym(AppLanguage language, string expected) =>
        Assert.Equal(expected, language.DisplayName());
}

/// <summary>
/// Every <see cref="StringTable"/> entry is <c>required</c>, so the compiler already refuses a
/// table that omits a key. What it cannot catch is an empty or untranslated value, which is what
/// these tests are for.
/// </summary>
public class StringTableTests
{
    private static readonly PropertyInfo[] TextProperties = typeof(StringTable)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.PropertyType == typeof(string) && property.CanRead)
        .ToArray();

    public static TheoryData<AppLanguage> AllLanguages()
    {
        var data = new TheoryData<AppLanguage>();

        foreach (var language in AppLanguageExtensions.AllLanguages)
        {
            data.Add(language);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void EveryStringIsPresentAndNotBlank(AppLanguage language)
    {
        var table = Strings.Get(language);

        Assert.NotEmpty(TextProperties);

        foreach (var property in TextProperties)
        {
            var value = (string?)property.GetValue(table);
            Assert.False(
                string.IsNullOrWhiteSpace(value),
                $"{language}.{property.Name} is empty.");
        }
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void ErrorTemplatesKeepTheirPlaceholder(AppLanguage language)
    {
        var table = Strings.Get(language);

        Assert.Contains("{0}", table.UnexpectedError, StringComparison.Ordinal);
        Assert.Contains("{0}", table.PdfGenerationFailed, StringComparison.Ordinal);
    }

    [Fact]
    public void AppTitleIsTranslatedInEveryLanguage()
    {
        var titles = AppLanguageExtensions.AllLanguages
            .Select(language => Strings.Get(language).AppTitle)
            .ToArray();

        Assert.Equal(4, titles.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Get_UnknownValueFallsBackToEnglish()
    {
        var unknown = (AppLanguage)999;

        Assert.Equal(Strings.Get(AppLanguage.English).AppTitle, Strings.Get(unknown).AppTitle);
    }

    [Fact]
    public void Format_SubstitutesTheArgument() =>
        Assert.Equal(
            "Unexpected error: boom",
            Strings.Get(AppLanguage.English).Format(Strings.Get(AppLanguage.English).UnexpectedError, "boom"));

    [Fact]
    public void Format_SubstitutesNonAsciiArguments() =>
        Assert.Equal(
            "意外错误：找不到文件",
            Strings.Get(AppLanguage.SimplifiedChinese).Format(
                Strings.Get(AppLanguage.SimplifiedChinese).UnexpectedError,
                "找不到文件"));

    [Fact]
    public void Format_LeavesATemplateWithoutAPlaceholderAlone() =>
        Assert.Equal("no placeholder here", Strings.Get(AppLanguage.English).Format("no placeholder here", "ignored"));
}
