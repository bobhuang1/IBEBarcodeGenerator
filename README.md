# IBE Barcode Generator

Free, open-source, cross-platform barcode label generator — a from-scratch
recreation of the discontinued "IBE Barcode Studio" (Windows/VB), released
under the MIT license.

## Status

Early development. See `docs/superpowers/specs/` for the architecture design
and `docs/superpowers/plans/` for implementation plans.

## Solution layout

- `src/IBEBarcode.Core` — barcode data models and custom-written encoders
  (no graphics dependency).
- `tests/IBEBarcode.Core.Tests` — xUnit tests for `IBEBarcode.Core`.

More projects (`IBEBarcode.Rendering`, `IBEBarcode.Printing`,
`IBEBarcode.Templates`, `IBEBarcode.Desktop`, `IBEBarcode.Web`) land in
later plans; see the design spec for the full solution layout.

## Build and test

```bash
dotnet build
dotnet test
```

## License

MIT — see `LICENSE`.
