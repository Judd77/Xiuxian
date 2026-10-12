# 代码结构与 C# 约定

更新：2026-10-12。本文记录当前实现的职责与持续适用的约束；正式剧情与交互见 `STORY.md`、`GAME_SCRIPT.md`，工程实况见 `DEVELOPMENT_STATUS.md`。

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
| `DialogueReader`                      | 逐字阅读、速度偏好、自动翻页、补全、会话历史与阅读快捷键                               | 剧情判断、日程和奖励                     |
| `CampaignSaveStore`                   | 版本 3 JSON、临时文件替换、备份恢复                                                    | 修改 Session 和 UI                       |
| `GameAudio`                           | BGM/环境声、音量与淡入淡出                                                             | 剧情判定、游戏存档                       |

按钮通过 `IStoryCommands` 调用清晰命令，不再经过 `Action -> View 事件 -> Application` 的二次转发。`StoryApplication`
持有会话生命周期：启动时仅显示首页，开始游戏创建新会话，继续游戏优先恢复内存会话，读取存档显式恢复磁盘快照。读取存档先完整验证，再提交到当前周目。设置可使用 PlayerPrefs，游戏进度不可使用
PlayerPrefs。音乐和背景通过场景序列化引用接入，不在 Update 中查找或加载资源。

旧 `FirstDay*` 原型、`Prototype.unity` 及对应测试工具已经移除。正式版使用 `GameFull.unity` 与 `Campaign.json`；试玩版使用 `GameDemo.unity` 与 `DemoCampaign.json`。两个场景共用运行代码和资产目录，构建命令分别只打入对应场景。试玩与正式版使用独立存档文件，正式版可继承当前修订号及明确声明兼容修订号的试玩进度。正式版 JSON 为 revision 39，Demo仍为35；正式版采用单主线，节点按正式游戏剧本制作。

`StorySession` 已有可存档复现的随机状态基础，但当前正式剧情尚未使用随机结果；不能把随机事件池描述成已接入或新设计的必做功能。

线索簿的解锁判断在 `StoryViewStateFactory` 中从会话变量映射为 `ClueJournalState`。`ClueJournalView` 不持有 `StorySession`，线索的标题、分类和说明也不写入存档；存档仍只保存稳定剧情变量与当前节点。

正式线索簿显示“已收录”；未解锁条目的标题及说明不向玩家展示。复测、小试、救援与公开暂守的描述在StoryViewStateFactory中依既有变量及稳定节点ID映射，区分已准备与已执行，不修改会话。e.extraCharm同时用于H05迟撤与E03损耗，进入正式引流以后不单凭它判定腕伤来源。开闸记录从e.bellPlaced收录，在e04.stop才显示第三声撤力完成，出炉须到e04.outside及其后继节点；没有新增持久字段或递增剧情revision。

## 验证入口

- 正式剧情编辑模式：`WenXinJie/Validation/Run Campaign Smoke Tests`。
- 当前原型 Play Mode：`WenXinJie/Validation/Run Campaign Play Mode Checks`；重制正式剧本时须更新检查场景。
- 正式场景创建/修复：`WenXinJie/Setup/Create Campaign Scene`，可重复执行，更新媒体目录和唯一构建场景。
- MCP：`node Tools/unity-mcp.cjs Unity_RunCommand Tools/InspectUnity.cs`。脚本固定项目并筛选活跃 Unity 实例；域重载期间连接会短暂消失，等待编译完成再调用。

UI 测试须等待渲染后再检查按钮实际中心的射线，不能仅按设计分辨率猜点击位置。按钮事件、合成 Input System 输入与实体鼠标操作须分别记录。当前已验证与未验证的事实见 `DEVELOPMENT_STATUS.md`。

## 阅读控制

底栏提供慢（16字/秒）、常（32字/秒）、快（64字/秒）与即时显示，速度偏好独立保存到PlayerPrefs的WenXinJie.TextSpeed，不写入剧情存档。换速度保留当前已显示进度，即时显示仍录入前尘历史。

StoryGameplayView依据展示状态向DialogueReader传入是否可继续、是否结局；自动模式只有在无选项、非结局、文字显示完后才调用既有AdvanceStory命令。停留按文本长度计算为2.5至15秒，可随时手动继续；选项页显示待选，玩家选择后继续自动阅读，不代选。打开前尘或其他弹窗时暂停并重置等待；新开局、读档及返回主界面关闭自动模式，结局也关闭。自动状态不持久化。键盘提交与鼠标按下的帧不同时自动推进，避免一次操作翻两页。

## 术法观测演出

StoryNode.observation 是可选的演出资料，包含双样本名称、已观测现象、余息强度及阵流阶段。StoryGraph 校验模式与范围，并将 JsonUtility 为缺失字段实例化的空资料归一为 null。StoryViewStateFactory 传递资料；StoryObservationView 负责布局，ObservationInk 用 UGUI 绘制旧纸、灰样、符纹及先后点亮。它们不修改剧情变量或解锁选项，也不加入等待门槛。离开观测节点与主界面时随玩法层隐藏，读档由当前节点恢复。

## 存档兼容与试玩接续

StoryApplication用已成功保存或读取的规范快照判断本次进度是否尚未保存，展示状态与保存时间由命令层传给视图；不在Update中访问磁盘。首页摘要通过ReadPreferredSave完整验证后生成，读取摘要不替换Session、不写入正式档。StoryOverlayView共用进度提示面板，打开时阻塞剧情和自动翻页，取消／Escape关闭。新开局与退出可先保存再执行；读取只确认放弃未保存内容，避免单存档槽中的目标被先保存动作覆盖。Demo分界页提供独立接续按钮，保留正文阅读；写入失败仍保留会话，重试成功后更新保存状态。

StoryDefinition.compatibleSaveRevisions 明确列出可直接恢复的旧剧情修订号。当前正式版revision 39兼容revision 33、34、35、36、37、38，保留旧节点与变量定义；H04三项责任改在证词结束后置位，旧快照已核标记仍有效，新增段落不增加存档变量。G07的复测标记在报告后置位，裂符使用既有补符变量；村口亮而炉门尚未回应时使用villageWarning观测阶段，由玩家决定撤力。H05由许澄亲自答应后置h.roles与story.contract，停止小试后才置h.test；迟撤使用既有e.extraCharm，结果节点置位。E02完成试铃、回钟与重新确认后置e.bellPlaced；后续e.extraCharm同时涵盖H05迟撤与E03旧刻度损耗，结局回应不可把两者统一写成同一处伤。revision 39的公开记录由h05.holdCopies实际交付后置e.publicCopies，旧存档已交付标记保持；改道尾声沿用e.delay／e.extraCharm回应已有延误和损耗，不新增变量。Demo仍为35。不开放三路线版revision 32。恢复仍校验版本 3 schema、campaign、节点和完整变量，不重放入口效果；再次保存写当前剧情修订号。后续纯画面改动不必递增剧情 revision。

正式版优先读取可用正式档或其备份。只有正式档无法读取时，才尝试有效 Demo 档；已经存在的正式档不会因试玩继承被自动覆盖。首次继承且尚无正式档时尝试写入正式档，写入失败保留已载入会话并提示手动保存。原档来自不兼容版本而 Demo 有效时，界面明确提示已接续试玩、原正式档保留。
