using IBEBarcode.Localization;

namespace IBEBarcode.Web;

/// <summary>
/// The language the whole app renders in.
///
/// The page that owns the language selector (<c>Pages/Home.razor</c>) is the only writer; the
/// layout components — the sidebar brand, Home/Help and the About link — are readers. They have to
/// be, because Blazor re-renders a component in isolation: without this shared state the sidebar
/// would keep its first-render English captions after the visitor switched language, and it would
/// disagree with the page whenever the language came from <c>?lang=</c> rather than from storage.
///
/// Registered as a singleton in <c>Program.cs</c>.
/// </summary>
public sealed class LanguageState
{
    public AppLanguage Current { get; private set; } = AppLanguage.English;

    /// <summary>Raised after <see cref="Current"/> changes so readers can re-render.</summary>
    public event Action? Changed;

    public void Set(AppLanguage language)
    {
        if (language == Current)
        {
            return;
        }

        Current = language;
        Changed?.Invoke();
    }
}
