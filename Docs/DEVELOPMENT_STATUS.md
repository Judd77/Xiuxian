# 开发状态

> 最新状态见本文最后的 2026-09-22 记录；前文保留历史验证过程。
## 已验证（本次开发前）
Unity官方连接成功；能在编辑器编译执行C#。项目路径 D:/project/Xianxia；SampleScene 已保存、未运行。
## 本次写入
交接文档；第一日独立规则核心；编辑器 smoke tests。编译和测试结果待后续追加，不能视为已通过。
## 尚未完成
游戏UI、独立游戏场景、JSON剧情、磁盘存档、正式资源、实机试玩、Windows构建和Steam集成。
## 新会话入口
先读 Docs/PROJECT_BRIEF.md、DECISIONS.md、此文件和 NEXT_TASKS.md，再检查实际文件和工具连接。以执行结果为准，不重复讨论既定选型。

## 规则核心验证通过
Unity 已编译加载 FirstDaySession 和 CoreSmokeTests。实际执行12种有序行动组合全部通过：中途JSON恢复、重复奖励防护、第三次行动拒绝、四种夜间结果、重复结算幂等、快照隔离、无效输入和异常存档校验。仅编辑器规则测试通过，尚无Play Mode界面试玩。
验证命令第一次因禁止反射命名空间被拒绝，改为直接调用测试类；不需要放宽权限。
下一步：独立Prototype场景、JSON剧情、UGUI/TMP UI、磁盘存档。尚未生成正式资源或构建发行包。

## 第一日交互原型
已创建 Assets/_WenXinJie/Scenes/Prototype.unity、Scripts/UI/FirstDayPrototype.cs 和 Data/FirstDayIntro.json。Unity 编译通过并进入 Play Mode。直接调用运行时控制器验证：五句开场 -> 调查 -> 拒绝重复调查 -> 拜访 -> 雨夜同谋结果出现在 UI Text，测试通过。没有模拟鼠标点击，尚未截图检验排版。
界面临时使用 UGUI Text + 本机动态中文字体（未复制字体文件），正式发行必须替换为授权字体和 TMP。场景UI在Awake动态创建。其他结局有文案，但运行UI本轮只验证同谋路线；规则层12路线先前通过。
单槽磁盘存档、临时文件替换、备份读取代码已写入，综合磁盘测试被工具交互限制拒绝，尚未实际验证，不要宣称存档通过。Unity Console 有 services-gateway 云服务错误，不能当成游戏脚本错误。
结束本轮已请求退出 Play Mode，Prototype场景保持打开；未修改构建列表。当前内容是几分钟规则试玩，不是15–20分钟完整章节。

## 1080p及存档验证通过
FirstDayPrototype参考分辨率已改1920×1080，并按1.2倍调整布局/字号/按钮；居中设计框。编译和核心12路线回归通过。Play Mode通过ExecuteEvents pointerClickHandler执行按钮：调查->存档->拜访->读取->再次拜访->覆盖存档生成备份->人为损坏测试主档->读取备份->拜访->同谋结局文本显示。全部通过。存档测试使用temporaryCachePath独立GUID目录，未触碰用户存档。新增testSaveDirectory仅UNITY_EDITOR编译。
此测试验证按钮事件处理，不等于真实鼠标坐标射线命中/画面截图验证；其他宽高比尚未实测。代码修改前副本位于Docs/Backups。请求退出Play Mode。

## 完整方案归档
已创建 Docs/GAME_MASTER_PLAN.md，整理此前确定的玩法、范围、剧情、技术、资源、质量和Steam发行方案。该文档为计划而非完成声明。本次仅写文档，未修改游戏代码或资源。

## 阅读体验第一步
新增DialogueReader：逐字显示、点击补全再推进、会话阅读记录（最多200条，前后翻页）、Esc关闭记录、无控件焦点时空格/回车推进。新游戏和成功读档清空记录；历史不写入存档。保留1080p布局及现有规则。修改前备份为Backups/FirstDayPrototype.beforeReader.cs.txt。
核心冒烟测试通过；Play Mode程序调用验证补全不跳句、下一句、历史计数、历史打开时禁止行动/推进、同谋结果和新游戏清空通过。鼠标射线测试失败：1920x1080按钮中心(1407,372)返回0命中，原因待查；不能声称实际鼠标交互通过。键盘硬件输入、时间动画、窗口适配未完整实测。尚未迁移TMP、生成正式美术或实现完整JSON节点系统。

## 2026-09-22：MCP、代码结构与首批授权资源

- 已通过工程中安装的 Unity 官方 relay 连接当前项目，实际执行 C#、读取 Console、进入/退出 Play Mode。可复用客户端为 `Tools/unity-mcp.cjs`，按工程路径和活跃进程筛选桥接实例。Unity 脚本域重载时桥接短暂断开，需要重载完成后再调用。
- 按用户要求新增 Rider 可读取的 `.editorconfig`，展开原有压缩语句，明确规则、流程协调、View、阅读、存档、音频职责。结构和边界见 `CODE_ARCHITECTURE.md`。没有运行 Rider 自动检查工具，不把遵循配置等同于 Rider 全量分析通过。
- `FirstDayPrototype` 不再构建 UGUI 或直接读写文件；新增 `FirstDayView`、`FirstDayViewState`、`FirstDaySaveStore`、`FirstDayAudio`。`FirstDayStory` 校验五句开场和四个稳定结果 ID，结局正文移入 JSON。保持原版本 1 存档字段与路径。
- 已下载、处理并接入 CC0 古画衍生背景、Tozan 的 Asianoriental2 音乐、Ylmir 的可循环雨声。增加音量滑杆、声音面板与素材鸣谢。来源页面、原始文件、加工脚本、导入文件哈希均留档，见 `ASSET_PROVENANCE.md`。古画只作为首批画面验证资源，不是原创雨夜山门或角色立绘。

### 验证结果

- Unity 编译及 12 条核心有序行动路线通过。
- `PrototypePlayModeChecks` 跨帧测试通过全部 12 条 UI 路线和四种结果、逐字补全、历史清空、重复奖励防护、隔离磁盘存档/备份/损坏双档、版本 1 数据兼容、历史/声音/鸣谢面板阻挡、背景和音频引用。
- 实际按钮中心点 GraphicRaycaster 检查通过；合成 Mouse/Keyboard 经 Input System 完成开始、空格补全、回车推进。不是实体鼠标/键盘人工试玩。
- 曾在自动测试中再次出现零命中：固定延时不足以保证后台编辑器完成画布更新。改为 `Awaitable.NextFrameAsync` 和 `EndOfFrameAsync` 等待实际渲染后通过，不强行改变 GraphicRaycaster 或关闭遮挡来规避问题。
- 截图检查覆盖 1920×1080、1280×720、1920×1200、2560×1080；文字高度和可见按钮射线检查通过。16:10 和超宽屏保留居中设计框及边缘留白。截图在 `Docs/Validation/`，不是独立 Windows 构建的适配结果。
- 音乐、雨声时长分别约 192.48 秒和 27 秒。窗口失焦且 `runInBackground=false` 时音源暂停；临时允许后台运行后，两轨 `isPlaying=true` 且输出峰值非零。两个滑杆设置传入音频组件正确，检查后恢复音量。尚未完成人工听审和循环接缝评价。

### 未完成与限制

- Unity 生成模型查询返回 `Models: []`，Console 同时出现 `generators.ai.unity.com` 的设置/余额请求重试失败；未运行生成、未付费。原创背景与立绘尚未完成。
- 官方字体仓库许可证下载遇到网络失败，未导入来源不明字体；当前仍是本机动态系统字体，TMP 和可发行中文字体待办。
- 没有完整 JSON 节点引擎、完整第一日长剧本、Windows 发行构建或 Steam 接入。项目目录当前不是 Git 仓库，未创建提交。

本轮结束：最终回归再次通过，声音面板改为不透明遮罩并修正滑块尺寸；已恢复 1920×1080 预览和验证前的后台运行设置，退出 Play Mode。Prototype 场景已保存并保持打开。
