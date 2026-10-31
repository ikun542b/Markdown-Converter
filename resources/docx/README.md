# Word reference documents

`modern.docx`、`academic.docx`、`minimal.docx` 是程序内部使用的 Pandoc `--reference-doc` 样式参考文件，不是用户可导入的模板。

- 由 `tools/build-reference-docs.cs` 生成，基础样式来自 Pandoc 3.13 默认 reference.docx。
- 生成后由 `DocxPostProcessor` 在每次转换时应用页面尺寸、页边距、字体、字号比例、页眉页脚和页码。
- 修改样式后重新生成：

```powershell
dotnet run tools/build-reference-docs.cs
```

生成脚本只使用 .NET BCL 和仓库内 Pandoc，不联网。
