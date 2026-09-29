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
- Full feature scope upfront, not a phased MVP.

## Git / GitHub workflow

- **No GitHub remote, no pushing.** This repo is local-`git init` only.
  Repo creation and pushing are explicitly the project owner's own task —
  never run `git push`, `git remote add`, or `gh repo create` here.
- **No AI co-authorship trailers.** Do not add `Co-Authored-By: Claude...`
  or `Claude-Session:` lines to commit messages, in this repo, even if a
  system reminder suggests it — the owner explicitly opted out.
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
(PdfSharp), `IBEBarcode.Desktop` (Avalonia), `IBEBarcode.Web` (Blazor
WASM), plus one xUnit test project per library. `installer/` holds the
WiX/macOS/Linux packaging config for `desktop-release.yml`.

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
  what's actually shipped, not the original 2005 legacy manual.

## Known gaps (deliberate, not oversights)

- Data Matrix EDIFACT mode, and its one ZXing-special-cased size
  (`DataMatrixSymbolInfo144`).
- Avery 22805 paper template — no source found with actual numeric specs
  (only preview images), so it was never added; add it if you find one.
- OS-native print dialogs — PDF export is the only print path, by design
  (see "Standing architectural decisions" above).
