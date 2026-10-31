# Windows installer

Phase 7 uses **unpackaged WinUI 3 + Windows App SDK self-contained + Inno Setup 6**.

## Build

From the repository root:

```powershell
dotnet publish src/MarkdownDocumentConverter.App/MarkdownDocumentConverter.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:WindowsAppSDKSelfContained=true -o publish
```

Confirm the publish folder contains:

```text
publish/MarkdownDocumentConverter.App.exe
publish/MarkdownDocumentConverter.App.pri
publish/resources/engines/win-x64/pandoc.exe
publish/resources/engines/win-x64/typst.exe
publish/resources/docx/*.docx
publish/resources/typst/*.typ
```

Install Inno Setup 6:

```powershell
winget install --id JRSoftware.InnoSetup --silent --accept-package-agreements --accept-source-agreements
```

Compile the installer:

```powershell
iscc installer\windows\setup.iss
```

Output: `artifacts\installer\MarkdownDocumentConverter-Setup-0.1.0.exe`.

## Offline install verification

On a clean Windows 10/11 x64 machine with no .NET SDK, Windows App SDK,
Pandoc or Typst installed, and with networking disabled:

1. Run the generated `Setup.exe`.
2. Launch from the Start menu.
3. Convert a Markdown file containing Chinese text and math to DOCX and PDF.
4. Confirm both files open and the DOCX formula is editable OMML.
5. Uninstall from Windows Settings.
6. Confirm `%APPDATA%\MarkdownDocumentConverter` user data is retained unless
   the user removes it manually.

## Notes

- The installer packages the complete self-contained publish directory; it
  does not download runtimes or engines.
- `THIRD-PARTY-NOTICES.txt` lists bundled engine licenses. The project's own
  source license is still undecided in this repository; select one before
  public distribution and add it as `LicenseFile` in `setup.iss`.
- If a clean machine reports a missing Visual C++ runtime, add the legally
  redistributable VC++ runtime to the publish/installer payload rather than
  asking the user to install it separately.
