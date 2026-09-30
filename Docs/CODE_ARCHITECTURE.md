# 代码结构与 C# 约定

更新：2026-09-30。本文记录当前实现的职责与持续适用的约束；正式剧情与交互见 `STORY.md`、`GAME_SCRIPT.md`，工程实况见 `DEVELOPMENT_STATUS.md`。

## 格式

根目录 `.editorconfig` 是共享配置，Rider 可直接读取。采用四空格缩进、Allman 大括号、独立语句、显式控制流大括号、普通私有字段 `_camelCase`、Unity 序列化字段 `camelCase`、类型和方法
PascalCase。不要把方法体或多个语句压成一行。公开 JSON 字段保留数据文件中的字段名，它们是传输契约，不是业务对象。

常量与静态只读字段采用 PascalCase。当前 Unity 使用 C# 9，保持块级 namespace，不使用文件作用域 namespace。注释解释原因和边界；不逐行翻译代码。

## 职责

正式入口采用数据驱动的 Story Graph + Session + Presentation 分层。剧情规则可脱离 Unity 场景测试；Unity 组件只负责组装、输入和演出。不引入全局 Service Locator、常驻单例或把所有功能塞进一个管理器。

| 模块                                  | 责任                                                                                   | 不应承担                                 |
|---------------------------------------|----------------------------------------------------------------------------------------|------------------------------------------|
| `StoryDefinition` / `StoryNode`       | JSON 传输契约：节点、条件、效果、媒体提示                                              | 运行时字典、Unity 对象、存档 IO          |
| `StoryGraph`                          | 一次性建立索引并验证 ID、出口、变量边界和媒体所需键                                    | 推进玩家进度、显示界面                   |
| `StorySession`                        | 当前节点、世界变量、条件判断、效果结算、快照与恢复                                     | Unity 场景、磁盘路径、按钮               |
| `StoryApplication` / `IStoryCommands` | Composition Root 与明确的用户命令入口；组装 Graph、Session、View、音频和存档，协调流程 | 具体 UGUI 布局、剧情规则实现             |
| `StoryView`                           | 组合场景、玩法、弹窗和首页子视图，处理 Unity 生命周期输入                              | 直接创建所有控件、修改世界变量、访问磁盘 |
| `StorySceneView`                      | 背景交叉淡变、立绘定位与淡入                                                           | 菜单、选项和存档                         |
| `StoryGameplayView`                   | 顶栏、地点、选项、对话框与底栏                                                         | 场景切换、剧情变量计算                   |
| `StoryOverlayView`                    | 设置、鸣谢、前尘和线索簿的组合与开关                                                   | 剧情推进和磁盘存档                       |
| `StoryUiFactory`                      | 统一创建项目 UGUI 控件和九宫格样式                                                     | 持有游戏状态                             |
| `ClueJournalView`                     | 展示已收录与未发现的线索，处理条目切换；只接收展示状态                                 | 查询或修改剧情变量、参与存档             |
| `MainMenuView`                        | 首页布局、入口可用状态与首页提示；直接调用 `IStoryCommands`                            | 创建或恢复剧情会话、访问存档             |
| `StoryViewStateFactory`               | 把会话与媒体目录映射为只读展示状态                                                     | 创建控件、修改会话                       |
| `StoryViewState`                      | 从运行时到界面的不可持久、只读展示数据                                                 | 规则计算和资源查找策略                   |
| `StoryAssetCatalog`                   | 稳定剧情资源 ID 到已授权 Unity 资产的映射与完整性校验                                  | 决定剧情分支                             |
| `DialogueReader`                      | 逐字阅读、补全、会话历史、阅读快捷键                                                   | 日程和奖励                               |
| `CampaignSaveStore`                   | 版本 3 JSON、临时文件替换、备份恢复                                                    | 修改 Session 和 UI                       |
| `GameAudio`                           | BGM/环境声、音量与淡入淡出                                                             | 剧情判定、游戏存档                       |

按钮通过 `IStoryCommands` 调用清晰命令，不再经过 `Action -> View 事件 -> Application` 的二次转发。`StoryApplication`
持有会话生命周期：启动时仅显示首页，开始游戏创建新会话，继续游戏优先恢复内存会话，读取存档显式恢复磁盘快照。读取存档先完整验证，再提交到当前周目。设置可使用 PlayerPrefs，游戏进度不可使用
PlayerPrefs。音乐和背景通过场景序列化引用接入，不在 Update 中查找或加载资源。

旧 `FirstDay*` 原型、`Prototype.unity` 及对应测试工具已经移除。正式版使用 `GameFull.unity` 与 `Campaign.json`；试玩版使用 `GameDemo.unity` 与 `DemoCampaign.json`。两个场景共用运行代码和资产目录，构建命令分别只打入对应场景。试玩与正式版使用独立存档文件，正式版可继承当前修订号的试玩进度。现有 JSON 仍是 revision 15 原型；按 `GAME_SCRIPT.md` 重制时，要以逐场内容明确条件、效果和存档版本，不再从小说段落自动切剧情节点。

`StorySession` 已有可存档复现的随机状态基础，但当前正式剧情尚未使用随机结果；不能把随机事件池描述成已接入或新设计的必做功能。

线索簿的解锁判断在 `StoryViewStateFactory` 中从会话变量映射为 `ClueJournalState`。`ClueJournalView` 不持有 `StorySession`，线索的标题、分类和说明也不写入存档；存档仍只保存稳定剧情变量与当前节点。

## 验证入口

- 正式剧情编辑模式：`WenXinJie/Validation/Run Campaign Smoke Tests`。
- 当前原型 Play Mode：`WenXinJie/Validation/Run Campaign Play Mode Checks`；重制正式剧本时须更新检查场景。
- 正式场景创建/修复：`WenXinJie/Setup/Create Campaign Scene`，可重复执行，更新媒体目录和唯一构建场景。
- MCP：`node Tools/unity-mcp.cjs Unity_RunCommand Tools/InspectUnity.cs`。脚本固定项目并筛选活跃 Unity 实例；域重载期间连接会短暂消失，等待编译完成再调用。

UI 测试须等待渲染后再检查按钮实际中心的射线，不能仅按设计分辨率猜点击位置。按钮事件、合成 Input System 输入与实体鼠标操作须分别记录。当前已验证与未验证的事实见 `DEVELOPMENT_STATUS.md`。
