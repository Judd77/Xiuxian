# 代码结构与 C# 约定

更新：2026-09-22。按用户要求，项目必须兼顾 Rider 格式、职责划分和可读性。

## 格式

根目录 `.editorconfig` 是共享配置，Rider 可直接读取。采用四空格缩进、Allman 大括号、独立语句、显式控制流大括号、私有字段 `_camelCase`、类型和方法 PascalCase。不要把方法体或多个语句压成一行。公开 JSON 字段保留原名以兼容版本 1 存档；不要为了命名样式破坏序列化契约。

常量与静态只读字段采用 PascalCase。当前 Unity 使用 C# 9，保持块级 namespace，不使用文件作用域 namespace。注释解释原因和边界；不逐行翻译代码。

## 职责

正式入口采用数据驱动的 Story Graph + Session + Presentation 分层。剧情规则可脱离 Unity 场景测试；Unity 组件只负责组装、输入和演出。不引入全局 Service Locator、常驻单例或把所有功能塞进一个管理器。

| 模块 | 责任 | 不应承担 |
| --- | --- | --- |
| `StoryDefinition` / `StoryNode` | JSON 传输契约：节点、条件、效果、媒体提示 | 运行时字典、Unity 对象、存档 IO |
| `StoryGraph` | 一次性建立索引并验证 ID、出口、变量边界和媒体所需键 | 推进玩家进度、显示界面 |
| `StorySession` | 当前节点、世界变量、条件判断、效果结算、快照与恢复 | Unity 场景、磁盘路径、按钮 |
| `StoryApplication` | Composition Root；组装 Graph、Session、View、音频和存档，协调流程 | 具体 UGUI 布局、剧情规则实现 |
| `StoryView` / `StoryUiFactory` | 剧情可视树、场景淡变、立绘、选项、工具与弹窗；只发出意图事件 | 修改世界变量、访问磁盘 |
| `MainMenuView` | 首页布局、入口可用状态与首页提示；通过回调上报用户意图 | 创建或恢复剧情会话、访问存档 |
| `StoryViewState` | 从运行时到界面的不可持久展示数据 | 规则计算和资源查找策略 |
| `StoryMediaCatalog` | 稳定媒体 ID 到已授权 Unity 资产的映射与完整性校验 | 决定剧情分支 |
| `DialogueReader` | 逐字阅读、补全、会话历史、阅读快捷键 | 日程和奖励 |
| `CampaignSaveStore` | 版本 2 JSON、临时文件替换、备份恢复 | 修改 Session 和 UI |
| `GameAudio` | BGM/环境声、音量与淡入淡出 | 剧情判定、游戏存档 |

界面事件由控制器订阅并在销毁时解除。`StoryApplication` 持有会话生命周期：启动时仅显示首页，开始游戏创建新会话，继续游戏优先恢复内存会话，读取存档显式恢复磁盘快照。读取存档先完整验证，再提交到当前周目。设置可使用 PlayerPrefs，游戏进度不可使用 PlayerPrefs。音乐和背景通过场景序列化引用接入，不在 Update 中查找或加载资源。

`FirstDay*` 代码与 `Prototype.unity` 保留为回归基线，不再是构建入口。正式入口为 `Game.unity` 与 `Campaign.json`。新增章节只增加节点、变量、条件、效果和媒体目录项；只有出现新的通用规则语义时才扩展运行器。稳定节点 ID 是存档游标，已发布内容修改 `revision` 时必须提供迁移方案，不能让读档默默落到错误节点。

## 验证入口

- 编辑模式：`WenXinJie/Validation/Run Core Smoke Tests`。
- 正式剧情编辑模式：`WenXinJie/Validation/Run Campaign Smoke Tests`。
- 正式剧情 Play Mode：`WenXinJie/Validation/Run Campaign Play Mode Checks`，使用隔离临时存档并输出截图。
- Play Mode：`WenXinJie/Validation/Run Prototype Play Mode Checks`，使用隔离临时存档，执行期间会重开原型，不能在需保留的试玩现场运行。
- 资源绑定：`WenXinJie/Setup/Connect Licensed Prototype Assets`，仅对已打开的 Prototype 场景生效。
- 正式场景创建/修复：`WenXinJie/Setup/Create Campaign Scene`，可重复执行，更新媒体目录和唯一构建场景。
- MCP：`node Tools/unity-mcp.cjs Unity_RunCommand Tools/InspectUnity.cs`。脚本固定项目并筛选活跃 Unity 实例；域重载期间连接会短暂消失，等待编译完成再调用。

UI 测试须等待渲染后再检查按钮实际中心的射线，不能仅按设计分辨率猜点击位置。按钮事件、合成 Input System 输入与实体鼠标操作须分别记录。真实结果见 `DEVELOPMENT_STATUS.md` 和 `Docs/Validation/`。
