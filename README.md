# Markdown-Converter

将 Markdown 文档转换为 Word（`.docx`）与 PDF 的 Windows 桌面程序。

技术栈固定为 **C# + WinUI 3 + Windows App SDK**，完全离线运行。Word 由 Pandoc 直接生成（数学公式为原生 OMML），PDF 由 Pandoc → Typst 独立生成，不经过 DOCX。

## 当前状态

Phase 0（工具链与最小转换闭环）已完成：

- 解决方案、WinUI 3 应用、Core 类库和 xUnit 测试项目已建立。
- Pandoc 3.13 与 Typst 0.15.1 已内置在 `resources/engines/win-x64`，运行时不会联网下载。
- 应用内可检测引擎版本，并执行最小 Markdown → DOCX / PDF 转换。
- DOCX 已验证包含原生 OMML 公式；PDF 已验证为有效 PDF。
- 批量队列、内置样式模板、完整设置页、安装包等属于后续阶段，尚未实现。

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

## Phase 0 验证记录（2026-10-31）

| 项目 | 结果 |
|---|---|
| `dotnet build MarkdownDocumentConverter.sln` | 0 警告，0 错误 |
| `dotnet test` | 7 个测试全部通过 |
| Pandoc 版本 | `pandoc 3.13` |
| Typst 版本 | `typst 0.15.1 (9dfd3a08)` |
| DOCX 检查 | ZIP/OOXML 可读，包含 `<m:oMath>`，无 `<w:drawing>` |
| PDF 检查 | `%PDF-` 文件头，非空，独立于 DOCX 链路 |
| UI 自动化 | 引擎检测与最小转换按钮均成功执行 |

转换链路只调用本机 `resources/engines/win-x64` 下的可执行文件，代码中不包含网络请求。物理断网环境的完整验收将在发布阶段按提示词第 23、27 节执行。

## 仓库结构

```text
src/MarkdownDocumentConverter.App/      WinUI 3 应用
src/MarkdownDocumentConverter.Core/     转换核心（不引用 WinUI）
src/MarkdownDocumentConverter.Tests/    xUnit 单元与集成测试
resources/engines/win-x64/              内置 Pandoc、Typst 与校验信息
tests/fixtures/                         测试用 Markdown 样例
初始开发参考资源/                        需求合同与界面参考图
```

`resources/engines/win-x64/*.exe` 通过 Git LFS 跟踪。新环境克隆后如缺少引擎，请先执行 `git lfs install` 和 `git lfs pull`。
