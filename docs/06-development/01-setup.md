# 开发环境

后端目标为 .NET 8。仓库的 `mise.toml` 选择 .NET 8，`global.json` 限定使用稳定的 8.0 SDK，并允许采用已安装的更新功能带。当前方案沿用本机的 mise 安装。

## 构建与启动

在仓库根目录运行：

```sh
mise exec -- dotnet --version
mise exec -- dotnet restore HomeworkGrading.sln
mise exec -- dotnet build HomeworkGrading.sln --no-restore
mise exec -- dotnet run --no-build --project src/HomeworkGrading.Api
```

首次恢复包时需要访问 NuGet。若 mise 提示信任项目配置，审阅 `mise.toml` 后运行 `mise trust`。用 `mise which dotnet` 或 `mise exec -- dotnet --info` 检查 SDK 来源。安装其他机器所需工具时，可参考 [mise 的 .NET 指南](https://mise.jdx.dev/lang/dotnet.html)和[微软 .NET 安装指南](https://learn.microsoft.com/en-us/dotnet/core/install/)。

默认开发启动地址为 `http://localhost:5080`。在浏览器打开 `/swagger` 查看和试用 API，通过 `/health` 检查服务是否启动。前端调用方式见 [API 手册](../04-api.md)，验证步骤见[测试与验证](../07-testing.md)。

## 启动前端样式预览

前端位于 `src/HomeworkGrading.Web`，使用 React 与 TDesign。需要 Node.js 20.19+（20.x）或 22.12+。从仓库根目录运行：

```sh
cd src/HomeworkGrading.Web
npm ci
npm run dev -- --host 127.0.0.1 --port 5173
```

打开 `http://127.0.0.1:5173/`；使用 `Ctrl+C` 停止。执行 `npm run build` 可检查类型并构建。当前预览无需启动后端，操作入口见[使用手册](../09-usage.md#前端样式预览)。

## 本地数据与可选配置

启动时应用数据库迁移。数据库默认位于 `src/HomeworkGrading.Api/App_Data/homework.db`，Cookie 保护密钥默认位于同目录下的 `keys/`。开发启动配置会在新建空库时写入示例数据；账号及操作见[使用手册](../09-usage.md)。

| 配置 | 用途 |
| --- | --- |
| `Storage__DataDirectory` | 覆盖数据库与会话密钥的默认目录。相对路径以应用内容根目录为准。 |
| `ConnectionStrings__Homework` | 覆盖 SQLite 连接字符串，例如 `Data Source=/tmp/homework-smoke.db`。运行集成脚本时使用独立文件。 |
| `DemoData__Enabled` | 是否初始化演示数据。仅在 `Development` 环境生效；默认启动配置设为 `true`。 |
| `Bootstrap__TeacherEmail`、`Bootstrap__TeacherPassword` | 建立首个正式教师账号。两项一起提供，优先于演示初始化。已有教师时不新增账号。 |
| `Grading__Endpoint`、`Grading__Model`、`Grading__TimeoutSeconds` | 覆盖模型请求地址、模型和超时配置。默认值见 `src/HomeworkGrading.Api/appsettings.json`。 |
| `Grading__ApiKey` | 启用模型调用所需密钥。可通过环境变量或开发环境的 .NET User Secrets 配置。 |

正式运行所需配置见[发布与交付](../08-release.md)。
