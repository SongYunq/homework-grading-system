# 测试与验证

## 运行接口集成检查

先完成[构建](06-development/01-setup.md)，再从仓库根目录在一个终端启动测试实例。`mktemp` 生成独立目录，脚本会写入测试账号、作业和评分，使用真实数据库文件。

```sh
qa_dir=$(mktemp -d)
Storage__DataDirectory="$qa_dir" mise exec -- dotnet run --no-build --project src/HomeworkGrading.Api -- --urls http://127.0.0.1:5081
```

在另一个终端运行：

```sh
BASE_URL=http://127.0.0.1:5081 mise exec -- python scripts/smoke_test.py
```

脚本使用 Python 3.12 标准库，目标为启用演示数据、未配置模型密钥的开发实例。测试结束后可停止实例并删除临时目录。API 契约见[手册](04-api.md)。

## 验证结果

验证环境为 macOS ARM64，mise 选用 .NET SDK 8.0.428。解决方案编译通过，0 警告、0 错误。

| 关联需求 | 实测内容 | 结果 |
| --- | --- | --- |
| REQ-001 | 演示账号登录、注册、退出、错误凭据、匿名访问和角色隔离、跨来源写请求拦截 | 通过 |
| REQ-002、REQ-005 | 发布、修改、删除、分页与汇总、教师和学生的详情投影、响应状态码及 `Location` | 通过 |
| REQ-003 | 学生交作业、截止后拒绝、并发重复提交、删除与提交并发、评分重复保存、服务重启后学生读取原成绩 | 通过 |
| REQ-004 | 模型未配置时返回 `503`，人工批改可继续 | 通过 |

另已构建 Release 发布目录并在本机运行。检查生成的 OpenAPI 定义覆盖 14 个操作，写操作声明必需请求头，错误响应声明 `application/problem+json`。

真实模型服务调用需要独立密钥，本次未运行。
