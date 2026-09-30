# Localization — English, Simplified Chinese, Traditional Chinese, Japanese

Date: 2026-09-29. Shipped in the v1.0.0.1 desktop release.

## What was asked

Both executables (the Avalonia Desktop app) and the Web version (Blazor
WebAssembly) should support localization, initially English, Simplified
Chinese, Traditional Chinese and Japanese.

## Design

One new library, `src/IBEBarcode.Localization`, serving both apps. The
alternative — `.resx` + `IStringLocalizer` per app, or a copy of the strings
inside each project — was rejected because it would have meant two sets of
translations that drift. A standalone WebAssembly client can host a
localization stack, but a satellite-assembly `.resources` set has to be
built, deployed and kept in step with the markup, and the Desktop app uses
plain XAML bindings rather than an `IStringLocalizer` anyway; a shared
`StringTable` the compiler checks is smaller than either alternative.

The whole feature is one file, `Strings.cs`:

- `AppLanguage` — the four languages, in menu order.
- `AppLanguageExtensions` — `DisplayName()` (the endonym shown in the
  picker: `English`, `简体中文`, `繁體中文`, `日本語`), `CultureTag()`
  (`en`, `zh-Hans`, `zh-Hant`, `ja`), `FromCultureName()` and
  `FromPreferredLanguages()`, and `AllLanguages`.
- `StringTable` — a `sealed record` with every user-visible string as a
  `required string` property, and four `private static readonly` instances.
  `required` is the whole trick: a table that omits a key is a compile
  error, so no language can silently fall back to English for a caption
  somebody forgot.
- `Strings` — the facade, `Strings.Get(language)` plus `AllLanguages`.

Translation decisions worth recording:

- Menu accelerators are translated too, in the platform's own convention:
  `_File` / `文件(_F)` / `檔案(_F)` / `ファイル(_F)`. Avalonia renders the
  underscore as the access key.
- Chinese and Japanese menu captions therefore need explicit access keys
  (`文件(_F)`) rather than the position of an English letter.
- "inch(es)" became a suffix string (`inch(es)` / `英寸` / `英吋` /
  `インチ`) interpolated between the number and the `*` in the status bar,
  so no language has to hard-code English word order.
- `UnexpectedError` and `PdfGenerationFailed` keep a `{0}` placeholder, and
  `StringTable.Format` does the substitution — these were the only two
  messages that embedded an exception message.
- Paper template names (`PaperTemplate.ToString()`) stay in English: they
  are product data (Avery 5160), not interface text.
- `docs/`-visible scope: the interface only. `help.html` is still English
  (see CLAUDE.md → Known gaps).

## Desktop wiring

- `MainViewModel` gained `SelectedLanguage` (an `[ObservableProperty]`),
  `S => Strings.Get(SelectedLanguage)`, `AvailableLanguages`, and
  `DetectLanguage()`, which reads `CultureInfo.CurrentUICulture`. Like the
  rest of the app it does not persist the choice.
- `OnSelectedLanguageChanged` raises `S` plus `StatusPageSize`,
  `StatusLabelSize` and `StatusSymbology` (they are computed strings that
  embed `S`), then calls `Regenerate()`.
- `MainWindow.axaml` binds every caption through `{Binding S.<Key>}`, and
  the menu bar gained a **Language** menu with one item per language
  (`Tag` = enum name, `Click="OnLanguageClick"`). The earlier attempt to
  drive that menu from `ItemsSource` with an `ItemContainerTheme` was
  dropped: `MenuItem.Header` is an `object`, and the header had to come
  from `DisplayName()`, which is not a bindable property. Four literal
  items are simpler and show the endonyms directly.
- `MainWindow.axaml.cs` handles `OnLanguageClick` and localizes the save
  dialog's title.

## Web wiring

- `Pages/Home.razor` holds `S` as its own field, refreshed by
  `SetLanguage()`, and renders a language `<select>` above the preview.
- Resolution order on first render: `?lang=` query string, then
  `localStorage["lang"]`, then `navigator.languages`, then English. It has
  to happen in `OnAfterRenderAsync` because JS interop is unavailable
  before the first render.
- `wwwroot/index.html` defines four tiny `window.ibeLang` helpers (`get`,
  `set`, `preferredLanguages`, `setDocumentLang`) so the C# side needs no
  JS module of its own. Every call site is wrapped in `try/catch`: private
  browsing blocks `localStorage`, and the page must still work.
- The layout is not part of the page, and Blazor re-renders a component in
  isolation, so the sidebar brand, Home/Help and the About link needed a
  shared source of truth: `LanguageState`, a singleton registered in
  `Program.cs`. `Home` is the only writer (it is the component that owns the
  selector and does the resolving); `NavMenu`, `MainLayout` and
  `NotFound.razor` read `LanguageState.Current` and subscribe to its
  `Changed` event. Without it the sidebar stayed on its first-render English
  captions after a language switch, and disagreed with the page whenever the
  language came from `?lang=` rather than from storage — which is exactly
  what the first `?lang=ja` smoke test showed.
- The Web app stores the choice; the Desktop app does not. The asymmetry is
  deliberate — the browser has `localStorage`, the Desktop app has no
  settings file.

## Tests

`tests/IBEBarcode.Localization.Tests`, 47 tests:

- `StringTableTests` reflects over the record, so the three keys added for
the Web layout (`NavigationMenu`, `NotFoundTitle`, `NotFoundMessage`) are
covered without touching the test file.
- `AppLanguageTests` — the culture-tag table (including `zh`, `zh-CN`,
  `zh-Hans-CN`, `zh-SG`, `zh-TW`, `zh-HK`, `zh-MO`, `zh-Hant-TW`, `ja-JP`,
  `en-GB`, `fr-FR`, null and blank), first-match-wins over a preferred
  language list, quality-value stripping (`zh-TW;q=0.9`), `CultureTag()`,
  `DisplayName()` and `AllLanguages` covering the enum.
- `StringTableTests` — every string property of every table is present and
  not blank (reflection, so a new key is covered automatically), the two
  `{0}` templates keep their placeholder, the four `AppTitle`s are all
  different, an unknown enum value falls back to English, and `Format`
  substitutes ASCII and non-ASCII arguments.

The projects are wired into `IBEBarcodeGenerator.slnx` like every other
library-and-test pair.

## Verification

- `dotnet build IBEBarcodeGenerator.slnx` — 0 warnings, 0 errors.
- Both apps run: the Web app was exercised in a browser in all four
  languages, including the `?lang=` deep link, the stored choice surviving
  a reload, the switch propagating to the layout, and a label-sheet PDF
  being produced (no console errors); the Desktop app was started to verify
  the localized XAML loads and the window builds.
- `dotnet test` — Core 319, Rendering 27, Templates 20, Localization 47.
  The Printing project still hangs when its five tests share one run in
  some sandboxes (pre-existing, PdfSharp/font related, never reproduced on
  a normal machine); each passes individually via `--filter`.
