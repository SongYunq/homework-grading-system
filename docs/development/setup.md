# 开发环境

当前仓库还没有 .NET 解决方案、应用入口或项目级 SDK 版本配置。以下步骤用于检查已有环境；实际构建、启动和测试命令在项目初始化后补齐。

## 本机优先使用 mise

已在 macOS ARM64 上验证，现有 mise 环境通过 `mise exec -- dotnet --version` 选中 `8.0.428`。这是本机当前可用的 SDK，不是项目版本要求。进入仓库根目录后检查：

```sh
mise --version
mise exec -- dotnet --list-sdks
mise exec -- dotnet --version
```

如果检查结果与预期不同，用 `mise which dotnet` 查看执行位置，用 `mise exec -- dotnet --info` 查看实际使用的 SDK 和架构。日常开发优先复用已有安装。项目 SDK 确定后，再考虑由 `global.json` 统一声明版本和兼容策略，并让 mise 与直接使用 `dotnet` 的开发环境遵循同一要求。mise 的共享安装可能包含多个 SDK，最终以 `dotnet --version` 的输出为准。

## 其他开发机器

愿意使用 mise 的开发者可以复用同一套版本配置。未使用 mise 的开发者，可以在确定项目版本后，按 [Microsoft 的 .NET 安装指南](https://learn.microsoft.com/en-us/dotnet/core/install/) 选择适合操作系统和 CPU 架构的 SDK。进入仓库根目录，用 `dotnet --list-sdks` 查看可用版本，再用 `dotnet --version` 检查实际选中版本。Windows、Linux 和其他 macOS 机器的可用性，需要用实际应用与依赖验证。

如果对方只需要运行项目，参见 [发布与交付](../release/README.md)；运行发布包不应依赖开发者本机的 mise 配置。

## 项目初始化后补充

记录项目要求的 SDK 和目标框架、外部依赖、必要配置及示例，以及从干净检出到构建、启动、测试的可执行步骤。验证时记录操作系统、架构、实际选中的 SDK 和结果。

参考：[mise 的 .NET 指南](https://mise.jdx.dev/lang/dotnet.html)、[Microsoft 的 `global.json` 说明](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)。
