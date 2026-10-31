# AGENTS.md

## Repository state

- Phases 0–6 are complete: solution, WinUI 3 app, Core library, xUnit tests, Word/PDF pipelines, conversion/settings UI, concurrent batch processing, and Golden regression fixtures exist. Phase 7 (installer/offline release verification) remains.
- `resources/engines/win-x64/*.exe` is tracked with Git LFS because Pandoc exceeds GitHub's file-size limit. Run `git lfs install` / `git lfs pull` on a fresh clone; do not delete or re-download the engines.
- `初始开发参考资源/初始开发提示词.md` is the requirements contract (~1600 lines, Chinese). Read the relevant sections before changing code. If it conflicts with config/scripts, trust the executable source but call out the conflict.
- Highest-value sections: §4 architecture, §5 layout, §16–18 concurrency/process/temp, §24 resources, §26 tests, §28–29 rules and phases.
- UI mockups: `初始开发参考资源/浅色主题参考界面.png` and `深色主题参考界面.png`. All user-facing text is Simplified Chinese; theme follows Windows, not a fixed `RequestedTheme`.

## Fixed stack and layout

- C# + WinUI 3 + Windows App SDK 2.6.4, .NET 10 (`dotnet` SDK 10.0.400), `win-x64`. No Python/WPF/WinForms/Electron/WebView substitute.
- `src/MarkdownDocumentConverter.App` = WinUI 3 host/ViewModels; `src/MarkdownDocumentConverter.Core` = engines, pipelines, infrastructure (never reference `Microsoft.UI.Xaml`); `src/MarkdownDocumentConverter.Tests` = xUnit.
- MVVM: `.xaml.cs` handles only view lifecycle/UI events; ViewModels do not start processes; the UI thread never runs or blocks on Pandoc/Typst.
- Ship unpackaged, Windows App SDK self-contained, packaged with Inno Setup. Do not add `PublishSingleFile`, trimming, or Native AOT without verifying them. Pin SDK/package versions; no Preview/Experimental packages.

## Conversion engines (vendored and pre-verified)

- `resources/engines/win-x64/pandoc.exe` = Pandoc 3.13; `typst.exe` = Typst 0.15.1.
- `resources/engines/win-x64/engines.json` records versions, SHA-256, licenses, verified command lines, and known warnings; `SHA256SUMS.txt` covers the whole folder. Update both if engines change. Never download engines at runtime or during tests.
- Verified command lines:
  - `resources/engines/win-x64/pandoc.exe <in.md> -o <out.docx> --standalone`
  - `resources/engines/win-x64/pandoc.exe <in.md> -t typst -o <out.typ>`
  - `resources/engines/win-x64/typst.exe compile <out.typ> <out.pdf>`
- Keep upstream license files (`COPYING.rtf`, `COPYRIGHT.txt`, `LICENSE`, `NOTICE`, `MANUAL.html`, `README.md`); they are attribution, not junk.
- Known harmless warning: Pandoc with `lang: zh-CN` reports missing `translations/zh.yaml` and untranslated Abstract/Table, but exits 0 and output is fine.
- Word reference docs live in `resources/docx/`; regenerate them with `dotnet run tools/build-reference-docs.cs` after changing template styling. `DocxPostProcessor` applies page size, margins, font/scale, header/footer, and page-number fields at conversion time — Pandoc's `--reference-doc` alone does not supply those.
- PDF templates live in `resources/typst/`. `TypstTemplateRenderer` replaces `@@...@@` placeholders before Pandoc runs; keep Pandoc's `$...$` syntax intact. Header/footer/page numbering applies only to body pages, and `PdfPipelineTests` verifies Chinese text, TOC/bookmarks, metadata, links, and A5 sizing with PdfPig.

## Application UI

- `AppServices` is the composition root for engines, pipelines, the coordinator, settings, and font enumeration. `MainWindow` creates `ConvertPage` and `SettingsPage` once and swaps them through the compact `NavigationView`.
- `ConvertViewModel` owns the queue and runs files through `BatchService` (1–4 concurrent jobs); `SettingsViewModel` saves `%APPDATA%/MarkdownDocumentConverter/settings.json` through `SettingsService`. Folder additions retain their relative subdirectories; fixed output directories receive that structure.
- Picker calls must be initialized with the owning window handle (`WindowNative.GetWindowHandle` + `InitializeWithWindow.Initialize`). Drag/drop and picker handlers stay in the page code-behind; conversion logic stays in ViewModels/Core.
- The app accepts `.md`/`.markdown` paths on the command line and preloads them into the queue (used for UI automation); no need to click through the picker.
- System fonts are enumerated with GDI `EnumFontFamiliesEx` in `Services/SystemFontService.cs`; do not add a UI framework dependency just for font enumeration.
- After saving settings, `SettingsChanged` reapplies defaults to the conversion page. Keep new settings wired through `AppSettings`, `SettingsViewModel`, and `ConvertViewModel` together.

## Non-negotiable behavior

- Two independent chains, both from the `.md` source: Word = Pandoc→DOCX; PDF = Pandoc→Typst→PDF. Never produce PDF via DOCX.
- Word math must be native OMML, never images; PDF math must typeset normally.
- Fully offline: no telemetry, update checks, online fonts/images, or online APIs.
- Images are out of scope by design. Only built-in styles (modern/academic/minimal); no user Word template import.
- Fonts: user selects a system font and one global scale only — no per-heading/body/code font-size controls.
- Never overwrite source `.md`. Default output-conflict policy is auto-rename.
- Do not write a Markdown parser or internal document model: hand parsing/conversion to Pandoc, and only validate inputs and map settings.
- No CommunityToolkit.Mvvm, ReactiveUI, or large UI libraries by default; justify any new NuGet (need, license, size, maintenance) before adding it.
- External processes: `ProcessStartInfo.ArgumentList` only, `UseShellExecute = false`, read stdout/stderr concurrently, support timeout, cancellation, and process-tree kill.

## Paths and state

- Temp work per job: `%TEMP%/MDC/<job-id>/`; delete on success.
- Config: `%APPDATA%/MarkdownDocumentConverter/settings.json`; JSON via `System.Text.Json` with `config_version`.
- Technical logs: `%APPDATA%/MarkdownDocumentConverter/logs/app.log`, rotated at 2 MB (10 files by default). `FileLogger` must stay append-only and must not log Markdown body content.
- `ConversionRequest.Timeout` drives the per-request linked cancellation/timeout; keep it wired from `AppSettings.ConversionTimeoutSeconds`.
- Never write user data into the install directory. Locate engines/templates only via `ResourceLocator` / `AppContext.BaseDirectory`, never hard-coded paths. `MDC_RESOURCE_ROOT` can override the resources directory for tests or portable layouts.

## Build and test

- `dotnet build MarkdownDocumentConverter.sln -c Debug`
- `dotnet test src/MarkdownDocumentConverter.Tests/MarkdownDocumentConverter.Tests.csproj -c Debug`
- Single test: `dotnet test src/MarkdownDocumentConverter.Tests/MarkdownDocumentConverter.Tests.csproj --filter "FullyQualifiedName~<TestName>"`
- App executable after build: `src/MarkdownDocumentConverter.App/bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MarkdownDocumentConverter.App.exe`
- Tests are xUnit and must run offline against the vendored engines; do not fetch anything. Integration tests inspect DOCX (OOXML/OMML) and PDF directly — no Word/Office automation.
- `tests/Golden` is the regression corpus. `GoldenTests` compares structure/extracted text, not pixels; `tests/Golden/README.md` holds the manual visual checklist (A4/A5, TOC/bookmarks, headers/footers, Chinese, math, links, theme, DPI).
- Spec §28.1 requires `dotnet build` plus affected tests after each change. Work phase by phase per §29; do not generate the whole app in one pass.
