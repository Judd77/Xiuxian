# 代码结构与 C# 约定

更新：2026-09-22。按用户要求，项目必须兼顾 Rider 格式、职责划分和可读性。

## 格式

根目录 `.editorconfig` 是共享配置，Rider 可直接读取。采用四空格缩进、Allman 大括号、独立语句、显式控制流大括号、私有字段 `_camelCase`、类型和方法 PascalCase。不要把方法体或多个语句压成一行。公开 JSON 字段保留原名以兼容版本 1 存档；不要为了命名样式破坏序列化契约。

常量与静态只读字段采用 PascalCase。当前 Unity 使用 C# 9，保持块级 namespace，不使用文件作用域 namespace。注释解释原因和边界；不逐行翻译代码。

## 职责

采用轻量的 Model / View / Presenter 分离与 Unity 组件组合，不引入全局 Service Locator、单例管理器或不必要的接口层。

| 模块 | 责任 | 不应承担 |
| --- | --- | --- |
| `FirstDaySession` | 行动、属性、证据、结局判定及规则校验 | UI 对象、音频播放、磁盘路径 |
| `FirstDayStory` | 读取和校验第一日 JSON 台词与四个结果 ID | 界面构建、按钮事件 |
| `FirstDayPrototype` | 组件组装、流程推进、调用规则与存档、生成展示状态 | UGUI 布局、直接读写存档文件 |
| `FirstDayView` | 界面创建和展示，通过事件提出玩家操作 | 修改规则状态、结局判定、存档读写 |
| `FirstDayViewState` | 从流程到界面的展示数据 | 游戏逻辑、场景引用 |
| `DialogueReader` | 逐字阅读、补全、会话历史、阅读快捷键 | 日程和奖励 |
| `FirstDaySaveStore` | 版本 1 JSON、临时文件替换、备份恢复与载入校验 | 修改当前界面和周目 |
| `FirstDayAudio` | BGM/雨声、音量与淡入淡出 | 剧情判定、游戏存档 |

界面事件由控制器订阅并在销毁时解除。读取存档先完整验证，再提交到当前周目。设置可使用 PlayerPrefs，游戏进度不可使用 PlayerPrefs。音乐和背景通过场景序列化引用接入，不在 Update 中查找或加载资源。

这是第一日切片的结构，不是完整剧情引擎。行动反馈部分仍在规则原型中；扩展章节时应转为结果 ID / 效果数据，再由剧情层选择文案。全节点系统尚未实现，不提前加入庞大的 FSM 或事件总线。

## 验证入口

- 编辑模式：`WenXinJie/Validation/Run Core Smoke Tests`。
- Play Mode：`WenXinJie/Validation/Run Prototype Play Mode Checks`，使用隔离临时存档，执行期间会重开原型，不能在需保留的试玩现场运行。
- 资源绑定：`WenXinJie/Setup/Connect Licensed Prototype Assets`，仅对已打开的 Prototype 场景生效。
- MCP：`node Tools/unity-mcp.cjs Unity_RunCommand Tools/InspectUnity.cs`。脚本固定项目并筛选活跃 Unity 实例；域重载期间连接会短暂消失，等待编译完成再调用。

UI 测试须等待渲染后再检查按钮实际中心的射线，不能仅按设计分辨率猜点击位置。按钮事件、合成 Input System 输入与实体鼠标操作须分别记录。真实结果见 `DEVELOPMENT_STATUS.md` 和 `Docs/Validation/`。
