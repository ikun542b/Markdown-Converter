# Golden regression fixtures

Each fixture is converted with a fixed style/page size by `GoldenTests`:

| Fixture | Style | Page | Key checks |
|---|---|---|---|
| Basic | modern | A4 | headings, links, footnotes |
| Academic | academic | A5 | TOC, table, A5 layout |
| Math | modern | A4 | OMML in DOCX, math text in PDF |
| Chinese | academic | A4 | Chinese text/font, searchable PDF |
| Tables | academic | A4 | table structure and content |
| LongDocument | minimal | A4 | multi-page output, bookmarks |

The tests compare structure and extracted text, not pixel-identical output. Fonts, Word versions and PDF rasterizers differ across machines, so byte-for-byte snapshots would be brittle.

Run only these tests:

```powershell
dotnet test src/MarkdownDocumentConverter.Tests/MarkdownDocumentConverter.Tests.csproj --filter "FullyQualifiedName~GoldenTests"
```

## Manual visual checklist

For each fixture, open the generated DOCX in Word and the PDF in a PDF viewer:

- A4/A5 page size and margins match the selected style.
- Heading hierarchy appears in the navigation pane and TOC.
- Header/footer/page numbers are present; front matter pages are unnumbered.
- Tables, code blocks, footnotes and hyperlinks remain readable.
- Word formulas are editable OMML objects; PDF formulas are typeset.
- Chinese characters render without missing glyphs; PDF text is searchable.
- Check both Windows light and dark system themes in the app.
- Check 100%, 150% and 200% display scaling for clipping/overlap.
