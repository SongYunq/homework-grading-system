# API 手册

后端首版使用 `/api/v1`。开发环境可打开 `http://localhost:5080/swagger` 查看并试调接口，机器可读的 OpenAPI 定义位于 `/swagger/v1/swagger.json`。启动方式见 [开发环境](06-development/01-setup.md)，实测范围见 [测试与验证](07-testing.md)。

本页对应 [需求说明](02-requirements.md) 中的 `REQ-001` 至 `REQ-005`。服务探活在 API 前缀之外，`GET /health` 返回 `200` 与 `{ "status": "ok" }`。

## 前端页面与请求

下表中路径均相对 `/api/v1`。列表把筛选结果与状态计数合在同一响应，详情按当前角色一并返回该页所需的数据。网页路由和交互设计见[前端设计](03-design/02-frontend.md)。

| 页面或动作 | 调用 | 如何使用响应 |
| --- | --- | --- |
| 登录 | `POST /auth/login` | `200` 返回账号与角色并建立会话，直接据此进入对应页面。 |
| 注册学生 | `POST /auth/register` | `201` 返回学生账号并建立会话，`Location` 指向 `/api/v1/auth/me`。 |
| 刷新页面后需要恢复姓名和角色 | `GET /auth/me` | `200` 返回当前账号；已有登录响应时可直接复用它。 |
| 退出 | `POST /auth/logout` | `204`，清除前端保存的账号与页面数据。 |
| 教师工作台、学生作业页、切换筛选或翻页 | `GET /assignments?status=all&page=1&pageSize=20` | `200` 同时返回本页作业和状态计数。 |
| 教师打开自己发布的作业或翻阅提交列表 | `GET /assignments/{assignmentId}?page=1&pageSize=20` | `200` 返回作业和当前页提交摘要。 |
| 学生打开作业或查看成绩 | `GET /assignments/{assignmentId}` | `200` 返回题目；本人已提交时同时返回答案、分数和反馈。 |
| 教师发布作业 | `POST /assignments` | `201` 返回作业详情和 `Location`。可用响应更新页面。 |
| 教师保存或删除作业 | `PUT /assignments/{assignmentId}` 或 `DELETE /assignments/{assignmentId}` | 保存返回 `200` 和详情，删除返回 `204`。已有提交时返回 `409`。 |
| 学生交作业 | `POST /assignments/{assignmentId}/submissions` | `201` 返回本人提交及 `Location`。可直接更新详情页的提交状态。 |
| 教师打开某一份提交 | `GET /submissions/{submissionId}` | `200` 返回完整作答与现有成绩。本人学生也可直接访问自己的提交。 |
| 教师生成评分建议（可选） | `POST /submissions/{submissionId}/grade-suggestions` | `200` 返回建议分数、反馈和模型。可填入编辑框供教师复核。 |
| 教师保存或更新评分 | `PUT /submissions/{submissionId}/grade` | `200` 返回已保存的提交。可用响应更新当前评分和摘要。 |

在业务请求前无需调用 `/auth/me` 检查登录。每个业务请求由服务端验证会话并检查角色、作业归属或提交归属。权限失效时直接处理业务请求的 `401`。翻页、作业筛选和模型建议也可以直接请求各自的接口。

## 浏览器接入

- 浏览器用相对路径请求 `/api/v1/...`，由前端开发服务器或部署端把 `/api` 同源转发给后端。保持浏览器访问的协议、主机和端口相同，例如统一使用 `localhost`，避免把 `localhost` 与 `127.0.0.1` 混用。后端未开启跨域访问。
- 登录和注册成功后浏览器会保存服务端设置的 `HttpOnly` 会话 Cookie。后续同源请求自动携带它，前端无需读取或保存凭据。会话凭据从签发起有效 8 小时，生产环境通过 HTTPS 发送。账号响应为 `{ id, name, email, role }`，`role` 是 `teacher` 或 `student`。
- 所有写操作，包括登录、注册、退出、提交、保存评分和生成评分建议，发送固定请求头 `X-Homework-Request: 1`。有 JSON 请求体时发送 `Content-Type: application/json`，无请求体时可以省略。
- API 成功响应直接返回 JSON 对象。`204` 没有正文，前端跳过 JSON 解析。资源 ID 是 UUID 字符串，字段名为 `camelCase`。有正文时，数值字段应发送 JSON 数字。时间使用带 `Z` 或 `±HH:mm` 的 ISO 8601 时间，例如 `2030-01-15T20:00:00+08:00`，后端统一返回 UTC 时间。

保存评分示例，假设 `submissionId` 来自作业详情的提交摘要：

```js
const response = await fetch(`/api/v1/submissions/${submissionId}/grade`, {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json',
    'X-Homework-Request': '1',
  },
  body: JSON.stringify({ score: 8.5, feedback: '结论正确，请补充推导过程。' }),
});
const submissionOrProblem = await response.json();
```

## 请求与响应字段

| 操作 | JSON 请求体与限制 |
| --- | --- |
| 注册 | `{ name, email, password }`。姓名去除首尾空白后为 1–80 字符，有效邮箱不超过 254 字符，密码 8–128 字符且不能全为空白。注册只创建学生账号。 |
| 登录 | `{ email, password }`。邮箱忽略大小写。 |
| 发布、保存作业 | `{ title, subject, description, referenceAnswer?, rubric?, maxScore, dueAt }`。标题、学科、说明必填，长度上限依次为 160、40、10,000 字符。参考答案和评分标准可省略或置 `null`，长度上限分别为 10,000 和 5,000 字符。满分大于 0 且不超过 1,000，最多两位小数；截止时间须晚于当前时间。`PUT` 传入完整作业字段。 |
| 学生提交 | `{ content }`。作答为 1–30,000 字符的文本，每个学生在截止前对每份作业可提交一次。 |
| 保存评分 | `{ score, feedback }`。分数从 0 到作业满分，最多两位小数；反馈必填，最多 5,000 字符。修改分数或反馈时会更新批改时间，重复发送相同值保留原批改时间。 |
| 退出、生成建议、删除 | 无请求体。 |

**作业列表**的外层为 `{ items, page, pageSize, totalCount, summary }`。每项包含 `id`、`title`、`subject`、`maxScore`、`dueAt`、`createdAt` 和 `status`，教师端另有 `submissionCount` 和 `gradedCount`。页码从 1 开始，默认每页 20 条，支持 1–100 条。按发布时间从新到旧排序。`totalCount` 表示当前筛选命中的总数，`summary` 始终统计当前账号可见作业的 `open`、`closed`、`pending`、`graded` 四种状态，不受筛选或翻页影响。`status` 可省略或传 `all`，也可以传其中一种状态。

| 列表状态 | 教师视角 | 学生视角 |
| --- | --- | --- |
| `pending` | 有至少一份待批改 | 本人已提交，待批改 |
| `graded` | 有提交且全部已批改 | 本人提交已出分 |
| `open` | 尚无提交且未截止 | 本人尚未提交且未截止 |
| `closed` | 尚无提交且已截止 | 本人尚未提交且已截止 |

`pending` 和 `graded` 优先显示提交进度。作业能否继续接收新提交应根据 `dueAt` 判断。

**作业详情**的共有字段是 `id`、`title`、`subject`、`description`、`maxScore`、`dueAt`、`createdAt`。教师只能获取自己的作业，详情包含 `submissions`，结构为 `{ items, page, pageSize, totalCount }`，按提交时间从新到旧排序，摘要项为 `{ id, studentId, studentName, submittedAt, score, gradedAt }`。教师专用的 `referenceAnswer` 和 `rubric` 仅在设置后出现。学生可查看此首版共享空间中的作业，详情在本人已提交时包含 `mySubmission`，结构与提交详情相同。

**提交详情**为 `{ id, assignmentId, studentId, studentName, content, submittedAt, score, feedback, gradedAt }`，未批改时最后三项为 `null`。只有本人学生与发布这份作业的教师可查看。**评分建议**为 `{ score, feedback, model }`，要求作业有评分标准，生成结果不改变已保存的成绩。确认或调整建议后，再调用保存评分接口。

## 错误响应

错误使用 `application/problem+json`，前端按 HTTP 状态码和稳定的 `code` 分支，`detail` 用于显示提示。绑定或格式校验失败时可能有 `errors`，键指向请求字段或字段路径。`traceId` 可用于关联服务端日志。[Problem Details 规范](https://www.rfc-editor.org/rfc/rfc9457.html)

```json
{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "detail": "你已提交这份作业。",
  "instance": "/api/v1/assignments/4d5f5eb7-8003-4f61-ab28-0d00f04d326a/submissions",
  "code": "submission_already_exists",
  "traceId": "sample-trace-id"
}
```

| 状态码 | 场景与页面处理 |
| --- | --- |
| `400` | 缺字段、输入不合法、状态或分页参数无效，或缺少固定写请求头。显示提示并允许修改。 |
| `401` | 登录接口的 `invalid_credentials` 表示登录失败；业务接口的 `authentication_required` 表示需要重新登录。响应带有 `WWW-Authenticate: Session realm="homework"`。 |
| `403` | 当前角色无法执行该操作。 |
| `404` | 资源不存在或不在当前账号可见范围内。教师查看他人作业、学生查看他人提交均按此处理。 |
| `405`、`413`、`415` | 分别表示请求方法错误、请求体过大或请求格式不支持。 |
| `409` | 当前状态不允许操作，常见 `code` 为 `email_already_registered`、`submission_already_exists`、`assignment_closed`、`assignment_has_submissions` 或 `grading_rubric_required`。 |
| `429` | 登录、注册或生成评分建议过于频繁。等待 `Retry-After` 指定的秒数后再试（如果响应提供该头）。 |
| `500`、`502`、`503`、`504` | 分别包括内部错误、评分服务响应异常、暂时不可用或评分服务超时。评分建议不可用时仍可人工批改。 |
