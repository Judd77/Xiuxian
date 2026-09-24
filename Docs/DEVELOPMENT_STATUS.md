# 开发状态

> 最新状态见最前面的日期记录；后文保留历史验证过程。

## 2026-09-24：全量脚本清理与表现层重构

- 删除未进入构建的 `FirstDay*` 旧原型代码、`Prototype.unity`、旧数据、旧编辑器测试和对应 MCP 辅助脚本。项目不再同时维护两套剧情、UI 与存档实现。
- 新增 `IStoryCommands`，首页和玩法按钮直接调用语义明确的命令。退出路径现在是 `MainMenuView -> IStoryCommands.QuitGame -> StoryApplication.QuitGame`，移除了九个 View 事件及订阅/解绑样板代码。
- 原 360 行 `StoryView` 拆分为 `StorySceneView`、`StoryGameplayView`、`StoryOverlayView`；`StoryView` 只负责组合与 Unity 输入生命周期。
- 新增 `StoryViewStateFactory`，状态栏、选项可用性和线索簿映射不再堆在 `StoryApplication`。展示状态改为构造后只读。
- `DialogueReader` 移除公开委托字段，改为显式 `Initialize` 依赖；`GameAudio` 删除未使用入口并封闭继承。
- Unity 序列化字段按 Rider Unity 规范统一为 `camelCase`，同步更新场景、媒体资产和 `CampaignSetup`。所有现存正式 C# 文件已执行 Rider 格式化和全量分析。

### 验证

- `Assembly-CSharp.csproj --no-restore` 编译通过，0 错误；仅 Unity 包自身的 `System.Net.Http` 引用版本冲突警告。
- Unity MCP 重新绑定并保存正式场景后，`CampaignSmokeTests` 通过。
- `CampaignPlayModeChecks` 通过：覆盖首页开始/继续/读档、实际选项点击、人物与背景变化、第一夜结盟路线、隔离存读档和 1920x1080 截图。

## 2026-09-24：第一卷扩写与新增场景

- 正式剧情从 15 节点扩为 41 节点。两次主要行动仍是核心限制，但调查、赴约、修炼各自增加内部方法选择，夜间四种结果扩为多段回收剧情。
- 新增玉扣线索、山腹共鸣和宗门注意状态；顶栏显示线索总数与宗门是否被惊动。用户明确开发期不做旧存档兼容，剧情 revision 升为 2，旧开发存档直接失效。
- `StoryGraph` 增加结构可达性校验，孤立节点会在加载时拒绝。编辑器测试新增不可达内容用例。
- 新增原创旧碑廊、弟子居所夜景、山腹石门三张背景，母图位于 `AssetSources/Art/Generated/`，1080p 衍生图已接入 `stele`、`quartersNight`、`mountainHeart` 媒体 ID。
- 图像由 Codex 内置 OpenAI 图像生成工具制作，提示摘要、源文件 SHA-256、衍生图 SHA-256 和准入记录已更新。没有使用现有游戏拆包或来源不明素材。

### 验证

- `Assembly-CSharp.csproj` 与 `Assembly-CSharp-Editor.csproj`：0 警告、0 错误。
- JSON 数据模拟：41 节点全部可达；结盟、孤证、受护、共鸣四条路线均进入预期夜间结果并推进到第二日。
- 6 个剧情背景 ID 均存在于媒体目录；17 项正式资产哈希一致；`Assets/_WenXinJie` 下 69 个 `.meta` GUID 无重复。
- Unity 官方桥接返回 `Unity not detected`，因此本轮未执行 Play Mode 视觉回归。不能把上述静态验证视为 Unity 实机试玩通过。

### 仍未完成

- 当前单路线约 22 个阅读节点、1600–1700 字，尚未通过人工计时证明达到 15–20 分钟。
- 新场景尚未在 Play Mode 中检查 UI 遮挡、淡入和色彩衔接。
- 林月表情差分、第二与第三名主要角色、UI 音效和 Windows 构建仍待推进。

## 2026-09-24：线索簿

- 正式玩法底栏新增“线索”入口，复用现有水墨九宫格按钮与菜单字体。线索簿可切换查看外门名册抄页、青玉扣和山腹共鸣，未发现条目保留名称但隐藏说明。
- 展示层新增 `ClueJournalView`、`ClueJournalState` 与 `ClueEntryViewState`。解锁状态由 `StoryApplication` 从剧情变量单向映射，View 不读取 `StorySession`，没有增加存档字段或旧档兼容代码。
- 修复脚本热重载期间 `StoryView.IsModalOpen` 对尚未恢复引用的空访问；同步修正 Play Mode 测试，使结盟标记在 `c01.shared.vow` 实际结算后再断言和存档。

### 验证

- `Assembly-CSharp.csproj` 与 `Assembly-CSharp-Editor.csproj`：0 错误。
- Rider 对本轮新增代码未报告新的 warning；仍保留项目既有的 Unity 目录命名空间与序列化字段命名规则冲突，不在本轮牵连改名。
- `CampaignSmokeTests` 通过。
- `CampaignPlayModeChecks` 通过：实际点击线索簿，验证初始 `0 / 3`、取得名册与玉扣后的 `2 / 3`、青玉扣标题和分类，并继续覆盖首页、人物/背景切换、结盟路线及隔离存读档。

## 已验证（本次开发前）

Unity官方连接成功；能在编辑器编译执行C#。项目路径 D:/project/Xianxia；SampleScene 已保存、未运行。

## 本次写入

交接文档；第一日独立规则核心；编辑器 smoke tests。编译和测试结果待后续追加，不能视为已通过。

## 尚未完成

游戏UI、独立游戏场景、JSON剧情、磁盘存档、正式资源、实机试玩、Windows构建和Steam集成。

## 新会话入口

先读 Docs/PROJECT_BRIEF.md、DECISIONS.md、此文件和 NEXT_TASKS.md，再检查实际文件和工具连接。以执行结果为准，不重复讨论既定选型。

## 规则核心验证通过

Unity 已编译加载 FirstDaySession 和 CoreSmokeTests。实际执行12种有序行动组合全部通过：中途JSON恢复、重复奖励防护、第三次行动拒绝、四种夜间结果、重复结算幂等、快照隔离、无效输入和异常存档校验。仅编辑器规则测试通过，尚无Play
Mode界面试玩。
验证命令第一次因禁止反射命名空间被拒绝，改为直接调用测试类；不需要放宽权限。
下一步：独立Prototype场景、JSON剧情、UGUI/TMP UI、磁盘存档。尚未生成正式资源或构建发行包。

## 第一日交互原型

已创建 Assets/_WenXinJie/Scenes/Prototype.unity、Scripts/UI/FirstDayPrototype.cs 和 Data/FirstDayIntro.json。Unity 编译通过并进入 Play Mode。直接调用运行时控制器验证：五句开场 ->
调查 -> 拒绝重复调查 -> 拜访 -> 雨夜同谋结果出现在 UI Text，测试通过。没有模拟鼠标点击，尚未截图检验排版。
界面临时使用 UGUI Text + 本机动态中文字体（未复制字体文件），正式发行必须替换为授权字体和 TMP。场景UI在Awake动态创建。其他结局有文案，但运行UI本轮只验证同谋路线；规则层12路线先前通过。
单槽磁盘存档、临时文件替换、备份读取代码已写入，综合磁盘测试被工具交互限制拒绝，尚未实际验证，不要宣称存档通过。Unity Console 有 services-gateway 云服务错误，不能当成游戏脚本错误。
结束本轮已请求退出 Play Mode，Prototype场景保持打开；未修改构建列表。当前内容是几分钟规则试玩，不是15–20分钟完整章节。

## 1080p及存档验证通过

FirstDayPrototype参考分辨率已改1920×1080，并按1.2倍调整布局/字号/按钮；居中设计框。编译和核心12路线回归通过。Play Mode通过ExecuteEvents pointerClickHandler执行按钮：调查->存档->拜访->
读取->再次拜访->覆盖存档生成备份->人为损坏测试主档->读取备份->拜访->同谋结局文本显示。全部通过。存档测试使用temporaryCachePath独立GUID目录，未触碰用户存档。新增testSaveDirectory仅UNITY_EDITOR编译。
此测试验证按钮事件处理，不等于真实鼠标坐标射线命中/画面截图验证；其他宽高比尚未实测。代码修改前副本位于Docs/Backups。请求退出Play Mode。

## 完整方案归档

已创建 Docs/GAME_MASTER_PLAN.md，整理此前确定的玩法、范围、剧情、技术、资源、质量和Steam发行方案。该文档为计划而非完成声明。本次仅写文档，未修改游戏代码或资源。

## 阅读体验第一步

新增DialogueReader：逐字显示、点击补全再推进、会话阅读记录（最多200条，前后翻页）、Esc关闭记录、无控件焦点时空格/回车推进。新游戏和成功读档清空记录；历史不写入存档。保留1080p布局及现有规则。修改前备份为Backups/FirstDayPrototype.beforeReader.cs.txt。
核心冒烟测试通过；Play Mode程序调用验证补全不跳句、下一句、历史计数、历史打开时禁止行动/推进、同谋结果和新游戏清空通过。鼠标射线测试失败：1920x1080按钮中心 (1407,372)
返回0命中，原因待查；不能声称实际鼠标交互通过。键盘硬件输入、时间动画、窗口适配未完整实测。尚未迁移TMP、生成正式美术或实现完整JSON节点系统。

## 2026-09-22：MCP、代码结构与首批授权资源

- 已通过工程中安装的 Unity 官方 relay 连接当前项目，实际执行 C#、读取 Console、进入/退出 Play Mode。可复用客户端为 `Tools/unity-mcp.cjs`，按工程路径和活跃进程筛选桥接实例。Unity
  脚本域重载时桥接短暂断开，需要重载完成后再调用。
- 按用户要求新增 Rider 可读取的 `.editorconfig`，展开原有压缩语句，明确规则、流程协调、View、阅读、存档、音频职责。结构和边界见 `CODE_ARCHITECTURE.md`。没有运行 Rider 自动检查工具，不把遵循配置等同于
  Rider 全量分析通过。
- `FirstDayPrototype` 不再构建 UGUI 或直接读写文件；新增 `FirstDayView`、`FirstDayViewState`、`FirstDaySaveStore`、`FirstDayAudio`。`FirstDayStory` 校验五句开场和四个稳定结果 ID，结局正文移入
  JSON。保持原版本 1 存档字段与路径。
- 已下载、处理并接入 CC0 古画衍生背景、Tozan 的 Asianoriental2 音乐、Ylmir 的可循环雨声。增加音量滑杆、声音面板与素材鸣谢。来源页面、原始文件、加工脚本、导入文件哈希均留档，见
  `ASSET_PROVENANCE.md`。古画只作为首批画面验证资源，不是原创雨夜山门或角色立绘。

### 验证结果

- Unity 编译及 12 条核心有序行动路线通过。
- `PrototypePlayModeChecks` 跨帧测试通过全部 12 条 UI 路线和四种结果、逐字补全、历史清空、重复奖励防护、隔离磁盘存档/备份/损坏双档、版本 1 数据兼容、历史/声音/鸣谢面板阻挡、背景和音频引用。
- 实际按钮中心点 GraphicRaycaster 检查通过；合成 Mouse/Keyboard 经 Input System 完成开始、空格补全、回车推进。不是实体鼠标/键盘人工试玩。
- 曾在自动测试中再次出现零命中：固定延时不足以保证后台编辑器完成画布更新。改为 `Awaitable.NextFrameAsync` 和 `EndOfFrameAsync` 等待实际渲染后通过，不强行改变 GraphicRaycaster
  或关闭遮挡来规避问题。
- 截图检查覆盖 1920×1080、1280×720、1920×1200、2560×1080；文字高度和可见按钮射线检查通过。16:10 和超宽屏保留居中设计框及边缘留白。截图在 `Docs/Validation/`，不是独立 Windows 构建的适配结果。
- 音乐、雨声时长分别约 192.48 秒和 27 秒。窗口失焦且 `runInBackground=false` 时音源暂停；临时允许后台运行后，两轨 `isPlaying=true` 且输出峰值非零。两个滑杆设置传入音频组件正确，检查后恢复音量。尚未完成人工听审和循环接缝评价。

### 未完成与限制

- Unity 生成模型查询返回 `Models: []`，Console 同时出现 `generators.ai.unity.com` 的设置/余额请求重试失败；未运行生成、未付费。原创背景与立绘尚未完成。
- 官方字体仓库许可证下载遇到网络失败，未导入来源不明字体；当前仍是本机动态系统字体，TMP 和可发行中文字体待办。
- 没有完整 JSON 节点引擎、完整第一日长剧本、Windows 发行构建或 Steam 接入。项目目录当前不是 Git 仓库，未创建提交。

本轮结束：最终回归再次通过，声音面板改为不透明遮罩并修正滑块尺寸；已恢复 1920×1080 预览和验证前的后台运行设置，退出 Play Mode。Prototype 场景已保存并保持打开。

## 2026-09-22：正式剧情入口与商用资源基线

- Git 已初始化，基线提交为 `8ed5191`。正式重构与资源变更将在验证完成后单独提交。
- 新增 `StoryGraph`、`StorySession`、条件/效果/选项数据、版本 2 快照与 `CampaignSaveStore`。存档以稳定节点 ID 保存游标，读取时校验剧情 ID、修订号、全部变量和值域，恢复不会重放节点效果。
- 新增 `Game.unity` 正式入口、`Campaign.json`、`StoryApplication`、`StoryView`、`StoryAssetCatalog` 和 `GameAudio`。旧 Prototype 已删除，不保留兼容代码。
- 当前剧情覆盖第一日三段开场、两个时辰的选择、四种夜间结果，以及由第一日状态控制的第二日入口。它证明跨日继承和媒体切换，仍不是完整 30 日内容。
- 已接入三张 CC0 古画衍生场景、林月透明管线立绘、绢纹 UI 图、CC0 音乐/雨声与 OFL 1.1 的 Noto Serif SC。来源、加工脚本、许可证和 SHA-256 清单均归档；未采用任何禁止商用或来源不明资产。
- Unity 生成服务仍返回空模型列表，因此本轮没有生成式美术。当前开放馆藏立绘是可商用管线资产，不等同于最终角色设定。

### 正式入口验证

- `CampaignSmokeTests` 通过：15 节点图校验、四种第一夜结果、第二日状态继承、非法起点拒绝、快照/磁盘存档/备份恢复、媒体目录、字体许可和构建入口。
- `CampaignPlayModeChecks` 首轮通过：实际按钮点击、背景和立绘切换、结盟路线、隔离存档/读档及 1920×1080 截图。截图流程随后加强为完成打字机后分别截取人物、选项和夜间结果，最终回归记录见
  `Docs/Validation/campaign-playmode-checks.txt`。
- Unity 编译无脚本错误。Unity AI 云端生成服务的设置/余额请求错误与游戏运行无关，但说明当前不能依赖该服务生产资产。

### 尚未完成

- 30 日完整剧本、另外两名主要角色、六处完整地点、表情差分、事件 CG、UI 音效、中文 TMP 字体资产、Windows 构建和实体硬件人工试玩。
- 当前林月立绘来自 CC0 古画裁切，风格可用但角色辨识度与表情演出不足；正式美术阶段应做原创统一设定，并继续保留生成/授权证据。

## 2026-09-22：现代修仙视觉升级

- 按《一念逍遥》所代表的抽象品质方向，重新确定清透云海、青绿与暖金、年轻化角色和精致修仙界面的视觉语言；未输入该游戏截图，也未复刻其角色、Logo、图标、界面或具体构图。
- 正式入口已换用项目专用生成的雨晨仙门、松风岭和透明林月立绘；夜间节点切换为独立夜景调色，剧情中的背景与人物会随节点变化。
- 常规对话立绘改为数据驱动的 AVG 半身站位：保留完整透明原图，在 Unity 中放大并让腰腿延伸出屏幕，由画面底边自然截断；每名角色可在媒体目录独立配置尺寸、顶部与右侧位置。
- 对话框和选项改用项目脚本生成的玉色描金半透明纹理。原先用于管线验证的古画立绘和绢纹 UI 已从正式内容移除，早期 CC0 背景只保留给原型回归。
- 生成原图、提示词摘要、SHA-256、衍生处理脚本、资产审批表和正式发行资产清单保存在 `Docs/AssetResearch/`。正式第三方资源仍只有 CC0 音频与 OFL 1.1 字体；修改素材不被视为取得授权的手段。

### 视觉回归

- `CampaignPlayModeChecks` 已覆盖 1920x1080 下的林月登场、选择界面、夜间结果、背景与立绘切换、实际按钮事件及隔离存档/读档；截图保存在 `Docs/Validation/campaign-*.png`。
- 当前仍需补齐林月表情差分、另外两名主要角色、其余地点、事件 CG 和 UI 音效。发行前还需完成人工反向搜图、角色相似性复核、Steam AI 内容披露核查和 Windows 构建实测。

## 2026-09-22：正式首页与会话入口

- 游戏启动后先进入仙门首页，不再直接创建剧情会话。首页提供开始游戏、继续游戏、读取存档、设置、鸣谢和退出游戏。
- 继续游戏优先恢复当前内存会话，冷启动时读取磁盘存档；读取存档始终显式恢复磁盘进度。剧情底栏新增返回主界面入口。
- 首页、剧情层和设置/鸣谢弹窗各自独立；设置与鸣谢可在首页和剧情中复用。Play Mode 检查覆盖首页截图、开始、返回、继续和单槽读取流程。
