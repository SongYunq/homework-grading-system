# 发布与交付

当前交付为首版后端源码，开发启动见[开发环境](06-development/01-setup.md)。API 和实际验证范围分别见[API 手册](04-api.md)与[测试与验证](07-testing.md)。

如需在已有 ASP.NET Core 8 运行时的机器运行，可从仓库根目录生成发布目录：

```sh
mise exec -- dotnet publish src/HomeworkGrading.Api -c Release -o ./artifacts/backend
```

在发布目录运行 `dotnet HomeworkGrading.Api.dll`。正式运行使用 `Production` 环境，将 `Storage__DataDirectory` 指向可写的持久目录，并在首次启动前配置教师账号。由同源 HTTPS 入口访问 API，按实际域名配置 `AllowedHosts`。配置说明见[开发环境](06-development/01-setup.md#本地数据与可选配置)。

数据库迁移在服务启动时执行。单实例运行时，备份和恢复需同时考虑 SQLite 数据与该实例的会话密钥。其他操作系统或架构的发布结果以目标机器运行验证为准。[.NET 发布说明](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
