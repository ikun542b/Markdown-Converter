# Markdown-Converter

将 Markdown 文档转换为 Word（`.docx`）与 PDF 的 Windows 桌面程序。

技术栈固定为 **C# + WinUI 3 + Windows App SDK**，完全离线运行。Word 由 Pandoc 直接生成（数学公式为原生 OMML），PDF 由 Pandoc → Typst 独立生成，不经过 DOCX。

## 当前状态

Phase 0（工具链与最小转换闭环）、Phase 1（核心模型与基础设施）、Phase 2（Word 管线）与 Phase 3（PDF 管线）已完成：

- 解决方案、WinUI 3 应用、Core 类库和 xUnit 测试项目已建立。
- Pandoc 3.13 与 Typst 0.15.1 已内置在 `resources/engines/win-x64`，运行时不会联网下载。
- 应用内可检测引擎版本，并执行最小 Markdown → DOCX / PDF 转换。
- DOCX 已验证包含原生 OMML 公式；PDF 已验证为有效 PDF。
- 已建立转换请求/结果/任务/进度模型、三套内置样式定义、配置持久化与迁移、统一错误映射。
- Word 管线已支持三套内置 reference.docx、A4/A5、页眉页脚、页码、自动目录（中文标题）、字体与全局字号比例、导航窗格标题层级。
- PDF 管线已支持三套 Typst 模板、A4/A5、页眉页脚、正文起始页码、目录与 PDF 书签、文档元数据、超链接、脚注、代码块、中文与数学排版。
- 批量队列、完整设置页、安装包等属于后续阶段，尚未实现。

## 环境要求

- Windows 10/11 x64
- .NET SDK 10.0.400（由 `global.json` 固定）
- Windows App SDK 2.6.4（项目使用自包含部署）
- 开发时无需单独安装 Pandoc 或 Typst

## 构建与测试

```powershell
dotnet build MarkdownDocumentConverter.sln -c Debug
dotnet test src/MarkdownDocumentConverter.Tests/MarkdownDocumentConverter.Tests.csproj -c Debug
```

只运行某个测试：

```powershell
dotnet test src/MarkdownDocumentConverter.Tests/MarkdownDocumentConverter.Tests.csproj --filter "FullyQualifiedName~EngineIntegrationTests"
```

## 运行应用

```powershell
dotnet build src/MarkdownDocumentConverter.App/MarkdownDocumentConverter.App.csproj -c Debug
```

然后启动：

```text
src/MarkdownDocumentConverter.App/bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/MarkdownDocumentConverter.App.exe
```

界面中点击“检测引擎”查看 Pandoc/Typst 版本，点击“运行最小转换”在 `%TEMP%/MDC/phase0-*` 下生成示例 DOCX/PDF。

## 验证记录（2026-10-31）

| 项目 | 结果 |
|---|---|
| `dotnet build MarkdownDocumentConverter.sln` | 0 警告，0 错误 |
| `dotnet test` | 32 个测试全部通过 |
| Pandoc 版本 | `pandoc 3.13` |
| Typst 版本 | `typst 0.15.1 (9dfd3a08)` |
| DOCX 检查 | ZIP/OOXML 可读，包含 `<m:oMath>`，无 `<w:drawing>` |
| PDF 检查 | `%PDF-` 文件头，非空，独立于 DOCX 链路 |
| UI 自动化 | 引擎检测与最小转换按钮均成功执行 |

Phase 1 验证补充：设置 JSON 往返、旧版本迁移、非法值回退、任务状态机、样式目录和错误映射均有单元/集成测试覆盖。用户配置写入 `%APPDATA%/MarkdownDocumentConverter/settings.json`。

Phase 2 验证补充：

- DOCX 页面尺寸 A4 为 `11906 × 16838` twips，A5 为 `8391 × 11906` twips；页边距按样式写入 `w:pgMar`。
- 页眉读取文档 core properties 的标题/作者；页脚使用 `PAGE`/`NUMPAGES` 域，并写入 `w:updateFields` 以便 Word 更新目录和页码。
- `--reference-doc` 只负责样式，页面设置与页眉页脚由 `DocxPostProcessor` 后处理；三套模板由 `tools/build-reference-docs.cs` 生成。
- 使用 Open XML SDK 校验器检查时，后处理新增内容没有产生额外 schema 错误；仅保留 Pandoc 原生 `w:tblLook` 属性在旧 schema 下的已知警告。
- 页眉页脚、目录和页码在 Word 中的最终视觉检查仍需在安装 Word 的验收环境执行。

Phase 3 验证补充：

- 使用 PdfPig 验证 PDF 文本可搜索，中文标题、脚注、代码块均可提取。
- 验证 A4/A5 页面尺寸、文档标题/作者元数据、`/Outlines` 书签结构、外部链接注解。
- 验证页码从正文页开始，标题页和目录页不显示页码。
- 三套 Typst 模板分别覆盖现代、学术、简约样式；模板编译失败时会删除不完整输出。

转换链路只调用本机 `resources/engines/win-x64` 下的可执行文件，代码中不包含网络请求。物理断网环境的完整验收将在发布阶段按提示词第 23、27 节执行。

## 仓库结构

```text
src/MarkdownDocumentConverter.App/      WinUI 3 应用
src/MarkdownDocumentConverter.Core/     转换核心（不引用 WinUI）
src/MarkdownDocumentConverter.Tests/    xUnit 单元与集成测试
resources/engines/win-x64/              内置 Pandoc、Typst 与校验信息
resources/docx/                         内置 Word reference.docx 样式模板
resources/typst/                        内置 PDF Typst 模板
tools/build-reference-docs.cs           重新生成 Word 样式模板
tests/fixtures/                         测试用 Markdown 样例
初始开发参考资源/                        需求合同与界面参考图
```

`resources/engines/win-x64/*.exe` 通过 Git LFS 跟踪。新环境克隆后如缺少引擎，请先执行 `git lfs install` 和 `git lfs pull`。
