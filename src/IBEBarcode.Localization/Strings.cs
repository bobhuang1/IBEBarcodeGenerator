using System;
using System.Collections.Generic;
using System.Globalization;

namespace IBEBarcode.Localization;

/// <summary>
/// Supported UI languages. English is the fallback for any string missing
/// from a non-English table.
/// </summary>
public enum AppLanguage
{
    English,
    SimplifiedChinese,
    TraditionalChinese,
    Japanese,
}

public static class AppLanguageExtensions
{
    public static string DisplayName(this AppLanguage language) => language switch
    {
        AppLanguage.English => "English",
        AppLanguage.SimplifiedChinese => "简体中文",
        AppLanguage.TraditionalChinese => "繁體中文",
        AppLanguage.Japanese => "日本語",
        _ => "English",
    };

    /// <summary>BCP-47 tag, used by the Web app's html lang attribute.</summary>
    public static string CultureTag(this AppLanguage language) => language switch
    {
        AppLanguage.English => "en",
        AppLanguage.SimplifiedChinese => "zh-Hans",
        AppLanguage.TraditionalChinese => "zh-Hant",
        AppLanguage.Japanese => "ja",
        _ => "en",
    };

    /// <summary>Pick a language from a browser/OS culture name, falling back to English.</summary>
    public static AppLanguage FromCultureName(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return AppLanguage.English;
        }

        var name = cultureName.ToLowerInvariant();

        if (name.StartsWith("zh"))
        {
            // zh-TW / zh-HK / zh-Hant* → Traditional; zh-CN / zh-Hans* → Simplified.
            return name.Contains("hant") || name.Contains("tw") || name.Contains("hk") || name.Contains("mo")
                ? AppLanguage.TraditionalChinese
                : AppLanguage.SimplifiedChinese;
        }

        if (name.StartsWith("ja"))
        {
            return AppLanguage.Japanese;
        }

        return AppLanguage.English;
    }

    /// <summary>Language for a set of preferred languages, first match wins.</summary>
    public static AppLanguage FromPreferredLanguages(IEnumerable<string?>? preferredLanguages)
    {
        if (preferredLanguages is null)
        {
            return AppLanguage.English;
        }

        foreach (var language in preferredLanguages)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                continue;
            }

            // Strip quality values ("ja;q=0.8") and take the language tag only.
            var tag = language.Split(';')[0].Trim();
            var parsed = FromCultureName(tag);

            if (parsed != AppLanguage.English || tag.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return parsed;
            }
        }

        return AppLanguage.English;
    }

    public static IReadOnlyList<AppLanguage> AllLanguages { get; } = new[]
    {
        AppLanguage.English,
        AppLanguage.SimplifiedChinese,
        AppLanguage.TraditionalChinese,
        AppLanguage.Japanese,
    };
}

/// <summary>
/// All user-visible strings. English is the source of truth: every key is <c>required</c>, so the
/// compiler refuses a table that omits one, and <c>StringTableTests</c> refuses a blank or
/// untranslated one. Adding a key means adding it to all four tables.
///
/// Usage: <c>Strings.Get(AppLanguage.Japanese).ZoomIn</c> — never throws; an unknown
/// <see cref="AppLanguage"/> falls back to English.
/// </summary>
public sealed record StringTable
{
    // App identity
    public required string AppTitle { get; init; }
    public required string Home { get; init; }
    public required string Help { get; init; }
    public required string About { get; init; }
    public required string Language { get; init; }

    // Menu bar
    public required string File { get; init; }
    public required string Symbology { get; init; }
    public required string Tools { get; init; }
    public required string SaveLabelSheetPdf { get; init; }
    public required string GeneratorHelp { get; init; }

    // Toolbar
    public required string RotateLeft { get; init; }
    public required string RotateRight { get; init; }
    public required string ZoomIn { get; init; }
    public required string ZoomOut { get; init; }

    // Rows
    public required string BarcodeValue { get; init; }
    public required string ValueToEncode { get; init; }
    public required string BarcodeIncrement { get; init; }
    public required string Quantity { get; init; }
    public required string UserField1 { get; init; }
    public required string UserField2 { get; init; }
    public required string UserField2Increment { get; init; }
    public required string CurrentPaperType { get; init; }
    public required string PaperTemplate { get; init; }
    public required string Font { get; init; }

    // Status bar
    public required string InchesSuffix { get; init; }

    // Errors
    public required string EnterValueToEncode { get; init; }
    public required string SelectTemplateFirst { get; init; }
    public required string GenerateValidBarcodeFirst { get; init; }
    public required string UnexpectedError { get; init; }
    public required string PdfGenerationFailed { get; init; }

    // Web page extras
    public required string GenerateLabelSheetPdfButton { get; init; }
    public required string DownloadLabelSheetPdf { get; init; }

    // Web layout (sidebar, the not-found page)
    public required string NavigationMenu { get; init; }
    public required string NotFoundTitle { get; init; }
    public required string NotFoundMessage { get; init; }

    private static readonly StringTable English = new()
    {
        AppTitle = "IBE Barcode Generator",
        Home = "Home",
        Help = "Help",
        About = "About",
        Language = "Language",

        File = "_File",
        Symbology = "_Symbology",
        Tools = "_Tools",
        SaveLabelSheetPdf = "Save Label Sheet PDF...",
        GeneratorHelp = "IBE Barcode Generator Help",

        RotateLeft = "Rotate Left",
        RotateRight = "Rotate Right",
        ZoomIn = "Zoom In (+)",
        ZoomOut = "Zoom Out (-)",

        BarcodeValue = "Barcode Value",
        ValueToEncode = "Value to encode",
        BarcodeIncrement = "Barcode Increment",
        Quantity = "Quantity",
        UserField1 = "User Field 1",
        UserField2 = "User Field 2",
        UserField2Increment = "User Field 2 Increment",
        CurrentPaperType = "Current Paper Type",
        PaperTemplate = "Paper template",
        Font = "Font",

        InchesSuffix = "inch(es)",

        EnterValueToEncode = "Enter a value to encode.",
        SelectTemplateFirst = "Select a paper template before exporting a label sheet.",
        GenerateValidBarcodeFirst = "Generate a valid barcode before exporting a label sheet.",
        UnexpectedError = "Unexpected error: {0}",
        PdfGenerationFailed = "PDF generation failed: {0}",

        GenerateLabelSheetPdfButton = "Generate Label Sheet PDF",
        DownloadLabelSheetPdf = "Download label-sheet.pdf",

        NavigationMenu = "Navigation menu",
        NotFoundTitle = "Not Found",
        NotFoundMessage = "Sorry, the content you are looking for does not exist.",
    };

    private static readonly StringTable SimplifiedChinese = new()
    {
        AppTitle = "IBE 条码生成器",
        Home = "主页",
        Help = "帮助",
        About = "关于",
        Language = "语言",

        File = "文件(_F)",
        Symbology = "条码类型(_S)",
        Tools = "工具(_T)",
        SaveLabelSheetPdf = "保存标签页 PDF...",
        GeneratorHelp = "IBE 条码生成器帮助",

        RotateLeft = "向左旋转",
        RotateRight = "向右旋转",
        ZoomIn = "放大 (+)",
        ZoomOut = "缩小 (-)",

        BarcodeValue = "显示条码值",
        ValueToEncode = "编码内容",
        BarcodeIncrement = "条码递增量",
        Quantity = "数量",
        UserField1 = "用户字段 1",
        UserField2 = "用户字段 2",
        UserField2Increment = "用户字段 2 递增量",
        CurrentPaperType = "当前纸张类型",
        PaperTemplate = "纸张模板",
        Font = "字体",

        InchesSuffix = "英寸",

        EnterValueToEncode = "请输入要编码的内容。",
        SelectTemplateFirst = "导出标签页之前，请先选择纸张模板。",
        GenerateValidBarcodeFirst = "导出标签页之前，请先生成有效的条码。",
        UnexpectedError = "意外错误：{0}",
        PdfGenerationFailed = "PDF 生成失败：{0}",

        GenerateLabelSheetPdfButton = "生成标签页 PDF",
        DownloadLabelSheetPdf = "下载 label-sheet.pdf",

        NavigationMenu = "导航菜单",
        NotFoundTitle = "找不到页面",
        NotFoundMessage = "抱歉，您要查找的内容不存在。",
    };

    private static readonly StringTable TraditionalChinese = new()
    {
        AppTitle = "IBE 條碼產生器",
        Home = "首頁",
        Help = "說明",
        About = "關於",
        Language = "語言",

        File = "檔案(_F)",
        Symbology = "條碼類型(_S)",
        Tools = "工具(_T)",
        SaveLabelSheetPdf = "儲存標籤頁 PDF...",
        GeneratorHelp = "IBE 條碼產生器說明",

        RotateLeft = "向左旋轉",
        RotateRight = "向右旋轉",
        ZoomIn = "放大 (+)",
        ZoomOut = "縮小 (-)",

        BarcodeValue = "顯示條碼值",
        ValueToEncode = "編碼內容",
        BarcodeIncrement = "條碼遞增量",
        Quantity = "數量",
        UserField1 = "使用者欄位 1",
        UserField2 = "使用者欄位 2",
        UserField2Increment = "使用者欄位 2 遞增量",
        CurrentPaperType = "目前紙張類型",
        PaperTemplate = "紙張範本",
        Font = "字型",

        InchesSuffix = "英吋",

        EnterValueToEncode = "請輸入要編碼的內容。",
        SelectTemplateFirst = "匯出標籤頁之前，請先選擇紙張範本。",
        GenerateValidBarcodeFirst = "匯出標籤頁之前，請先產生有效的條碼。",
        UnexpectedError = "意外錯誤：{0}",
        PdfGenerationFailed = "PDF 產生失敗：{0}",

        GenerateLabelSheetPdfButton = "產生標籤頁 PDF",
        DownloadLabelSheetPdf = "下載 label-sheet.pdf",

        NavigationMenu = "導覽選單",
        NotFoundTitle = "找不到頁面",
        NotFoundMessage = "抱歉，您要尋找的內容不存在。",
    };

    private static readonly StringTable Japanese = new()
    {
        AppTitle = "IBE バーコードジェネレーター",
        Home = "ホーム",
        Help = "ヘルプ",
        About = "バージョン情報",
        Language = "言語",

        File = "ファイル(_F)",
        Symbology = "シンボル(_S)",
        Tools = "ツール(_T)",
        SaveLabelSheetPdf = "ラベルシート PDF を保存...",
        GeneratorHelp = "IBE バーコードジェネレーター ヘルプ",

        RotateLeft = "左に回転",
        RotateRight = "右に回転",
        ZoomIn = "拡大 (+)",
        ZoomOut = "縮小 (-)",

        BarcodeValue = "バーコード値を表示",
        ValueToEncode = "エンコードする値",
        BarcodeIncrement = "バーコード増分",
        Quantity = "数量",
        UserField1 = "ユーザー項目 1",
        UserField2 = "ユーザー項目 2",
        UserField2Increment = "ユーザー項目 2 増分",
        CurrentPaperType = "現在の用紙タイプ",
        PaperTemplate = "用紙テンプレート",
        Font = "フォント",

        InchesSuffix = "インチ",

        EnterValueToEncode = "エンコードする値を入力してください。",
        SelectTemplateFirst = "ラベルシートを書き出す前に、用紙テンプレートを選択してください。",
        GenerateValidBarcodeFirst = "ラベルシートを書き出す前に、有効なバーコードを生成してください。",
        UnexpectedError = "予期しないエラー: {0}",
        PdfGenerationFailed = "PDF の生成に失敗しました: {0}",

        GenerateLabelSheetPdfButton = "ラベルシート PDF を生成",
        DownloadLabelSheetPdf = "label-sheet.pdf をダウンロード",

        NavigationMenu = "ナビゲーション メニュー",
        NotFoundTitle = "ページが見つかりません",
        NotFoundMessage = "お探しの内容は存在しません。",
    };

    public static StringTable Get(AppLanguage language) => language switch
    {
        AppLanguage.SimplifiedChinese => SimplifiedChinese,
        AppLanguage.TraditionalChinese => TraditionalChinese,
        AppLanguage.Japanese => Japanese,
        _ => English,
    };

    /// <summary>Format helper for the {0}-style templates.</summary>
    public string Format(string template, string arg) =>
        template.Contains("{0}", StringComparison.Ordinal) ? template.Replace("{0}", arg) : template;
}

/// <summary>Facade so callers write <c>Strings.Get(language)</c>.</summary>
public static class Strings
{
    public static StringTable Get(AppLanguage language) => StringTable.Get(language);

    public static IReadOnlyList<AppLanguage> AllLanguages => AppLanguageExtensions.AllLanguages;
}
