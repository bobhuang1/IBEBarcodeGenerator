# CLAUDE.md

Guidance for Claude Code (or any future session) picking up this repo cold.

## What this project is

IBE Barcode Generator: a from-scratch, MIT-licensed, open-source recreation
of a discontinued commercial VB Windows program ("IBE Barcode Studio",
IBE Group Inc., lost source). See `README.md` for the user-facing feature
list and build instructions.

## Standing architectural decisions (do not revisit without asking)

These were deliberately chosen by the project owner during initial
brainstorming and re-confirmed multiple times since. Don't propose
alternatives unless something has genuinely changed.

- **Desktop**: Avalonia (native XAML), not Blazor Hybrid/MAUI/Photino.
- **Web**: Blazor WebAssembly *standalone* (static hosting), not Blazor Server.
- **Barcode encoding**: 100% custom-written encoders in `IBEBarcode.Core`,
  no third-party barcode library (no ZXing.Net). ZXing's Apache-2.0 Java
  source may be fetched and read as a verification/porting reference only
  — never copied wholesale as a dependency, and never reproduced as large
  verbatim excerpts even in comments.
- **Printing**: PDF-first via PdfSharp (MIT), not native per-OS print APIs.
  Avalonia's own print API depends on a commercially-licensed package
  (`Avalonia.Controls.PdfViewer` → `AvaloniaUI.Licensing`), which conflicts
  with this project's free/open-source requirement — don't add it.
- **Windows installer tooling**: WiX, pinned to v5.0.2. WiX v7 introduced a
  paid "Open Source Maintenance Fee" EULA gate — do not upgrade past v5
  without re-checking that.
- **Localization is one shared library, four languages, always all four
  together**: English, Simplified Chinese, Traditional Chinese, Japanese,
  in `src/IBEBarcode.Localization`. A plain `StringTable` record with
  `required` members — *not* `.resx`, not a third-party i18n package, and
  not one copy of the strings per app. Both the Desktop and the Web app
  consume the same tables, so a caption added for one app is one string in
  one file, and the compiler refuses a table that is missing a key.
  `tests/IBEBarcode.Localization.Tests` fails the build if a translation is
  blank or untranslated. Translate the interface, never the data: barcode
  values, user-field captions and paper-template names stay as entered.
- Full feature scope upfront, not a phased MVP.

## Git / GitHub workflow

- **The repo is on GitHub** at `bobhuang1/IBEBarcodeGenerator` (public),
  with `origin` configured. Push to `origin master` when the owner asks for
  a release or an update; `gh repo create`, additional remotes and pushing
  to other targets still need their direct ask. (This supersedes the
  earlier "local `git init` only, no remote" note.)
- **No AI attribution of any kind.** Do not add `Co-Authored-By: Claude ...`,
  `Co-Authored-By: Codebuff ...`, `Claude-Session:`, `🤖 Generated with ...`
  or a `noreply@anthropic.com` / `noreply@codebuff.com` address to a commit
  message or a trailer, in this repo or any sibling — the owner explicitly
  opted out workspace-wide. `.githooks/commit-msg` rejects it; enable the
  hook once per clone with `git config core.hooksPath .githooks`.
- Prefer small, focused commits (this repo's history is one commit per
  bounded change, not squashed).
- Tag pushes matching `v*` (e.g. `v1.0.0`) trigger
  `.github/workflows/desktop-release.yml`, which builds and — for tag
  pushes only — publishes installers/portables for win-x64, win-x86,
  linux-x64, osx-x64, osx-arm64 to a GitHub Release. Plain pushes to
  `master` just build (no release).

## Working style expected in this repo

- **Execution mode: always inline** (`executing-plans`-style), never
  subagent-driven-development, for this project specifically.
- **TDD discipline**: every new unit of encoder/renderer logic gets a
  failing test first, then the implementation, then a pass — and a
  *second, independently-written* round-trip decoder (never reusing the
  encoder's own internals) that recovers the original input. This has
  caught real bugs; don't skip it for "obviously correct" code.
- **Large data tables** (barcode pattern tables, error-correction
  coefficients, the GS1 AI table) are fetched from authoritative source and
  mechanically converted with a small script (length/count-asserted) —
  never hand-retyped, never recalled from memory.
- When a spec detail is genuinely ambiguous, prefer empirically probing
  this project's own already-verified code (e.g. via reflection in a
  throwaway test/console program) over guessing.
- Scope things down deliberately when the "real" algorithm is
  disproportionately complex for the value it adds (e.g. Aztec text mode
  and PDF417 text mode both use a greedy, non-bit-optimal encoder instead
  of porting ZXing's full dynamic-programming optimizer) — document the
  simplification in a code comment, don't silently under-implement.
- Before adding a new `PaperTemplateCatalog` entry, verify the numbers:
  solve for an exact page fit (see existing entries' comments) — public
  "Avery template" sites are frequently wrong (one source had 5164 and
  5167's dimensions transposed, causing page heights of 12"+ on an
  11"-tall page — caught only by checking the arithmetic).

## Solution layout

See `README.md`'s "Solution layout" section for the up-to-date project
list. In short: `IBEBarcode.Core` (encoders), `IBEBarcode.Rendering`
(SkiaSharp), `IBEBarcode.Templates` (paper templates), `IBEBarcode.Printing`
(PdfSharp), `IBEBarcode.Localization` (the four-language strings),
`IBEBarcode.Desktop` (Avalonia), `IBEBarcode.Web` (Blazor WASM), plus one
xUnit test project per library. `installer/` holds the WiX/macOS/Linux
packaging config for `desktop-release.yml`.

The Desktop app binds captions with `{Binding S.<Key>}`, where `S` is
`Strings.Get(SelectedLanguage)` on `MainViewModel`; changing
`SelectedLanguage` re-raises `S` and the status strings. The Web app keeps
its own `S` in `Pages/Home.razor` and stores the choice under the
`lang` key through the small `window.ibeLang` helpers in
`wwwroot/index.html` (WebAssembly cannot touch `localStorage` before the
first render, so the pick-up happens in `OnAfterRenderAsync`). The Desktop
app detects the OS UI culture at start-up but does not persist the choice —
the app has no settings file by design.

## Build and test

```bash
dotnet build
dotnet test
dotnet run --project src/IBEBarcode.Desktop
dotnet run --project src/IBEBarcode.Web
```

Note: in some sandboxed/constrained environments `dotnet test` has been
observed to hang non-deterministically on the Printing test project
specifically (PdfSharp/font-loading related, never reproduced on a normal
machine) — if that happens, verify by running each test individually with
`--filter`, and via a standalone throwaway console app calling the same
code directly, rather than assuming the code is broken.

## Where deeper docs live

- `docs/superpowers/specs/` — architecture/design spec(s).
- `docs/superpowers/plans/` — one implementation plan per subsystem
  (each format/feature that got a dedicated plan). Not every later
  extension has its own plan doc — check the relevant source file's
  history/comments if a plan doc doesn't cover something.
- `src/IBEBarcode.Web/wwwroot/help.html` — the in-app user help page
  (shared by Desktop, which copies the same file at build time). Update
  this when user-facing features change; it's meant to stay accurate to
  what's actually shipped, not the original 2005 legacy manual. It is
  written in English only, deliberately — it is the reference the four UI
  translations are checked against.

## Known gaps (deliberate, not oversights)

- **The in-app help page is English only.** The four UI languages cover
  every caption, menu, button, status line and error message an app draws;
  `wwwroot/help.html` (315 lines, shared by Desktop and Web) is still the
  English original. Translating it means four HTML files kept in sync — a
  follow-up, not an oversight.
- **The Desktop app does not remember the chosen language** across runs,
  because it has no settings file at all. It re-detects from the OS UI
  language each launch. Persisting one preference would mean introducing
  settings storage, which the project has so far deliberately avoided.
- Data Matrix EDIFACT mode, and its one ZXing-special-cased size
  (`DataMatrixSymbolInfo144`).
- Avery 22805 paper template — no source found with actual numeric specs
  (only preview images), so it was never added; add it if you find one.
- OS-native print dialogs — PDF export is the only print path, by design
  (see "Standing architectural decisions" above).
