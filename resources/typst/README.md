# Typst templates

`modern.typ`、`academic.typ`、`minimal.typ` 是 Pandoc 的 Typst 模板（`--template`），由 `TypstTemplateRenderer` 在每次转换前填充参数后交给 Pandoc。

- 模板中的 `@@...@@` 是运行时占位符（纸张、页边距、字体、字号、标题/标题层级字号、页眉页脚代码），由 `TypstTemplateRenderer` 替换。
- 模板中的 `$if(...)$`、`$title$`、`$body$` 等是 Pandoc 模板语法，不能被误删。
- 页眉页脚和页码只应用到正文页；标题页与目录页不显示页码。
- 修改模板后运行 PDF 集成测试：`dotnet test ... --filter "FullyQualifiedName~PdfPipelineTests"`。
- 所有模板只使用系统已安装字体和 Typst 内置字体，不联网。
