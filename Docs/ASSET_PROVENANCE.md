# 资源与授权台账
2026-09-22 已接入下列首批 CC0 素材。它们是切片资源，不代表正式立绘、雨夜山门背景及全套音频已经完成。
未来每项记录：文件路径、来源URL/模型、日期、许可证或服务条款依据、署名要求、人工修改、商用审核状态。
AI生成不自动等于无版权风险；不要模仿具体在世画师或复刻现有游戏素材。
Steam AI披露要求应在提交时核查官方当前政策，尚未完成此项核查。

## 已接入资源

| 工程文件 | 作者与来源 | 许可与修改 |
| --- | --- | --- |
| `Assets/_WenXinJie/Art/Backgrounds/Landscape_Cool_1080.jpg` | 髡残，Spring Landscape / 春景图，1666，Cleveland Museum of Art，藏品 1966.367；https://clevelandart.org/art/1966.367 | 官方 API 的 `share_license_status` 为 CC0；裁除装裱边缘、裁切为 16:9、缩至 1920×1080、降低饱和度、冷色调及对比度调整。古画衍生画面，不宣称原创画作或真实山门场景。 |
| `Assets/_WenXinJie/Audio/BGM/AsianOriental2_Tozan.ogg` | Tozan；https://opengameart.org/content/asianoriental2 | 页面标注 CC0，并写明 `cc0 atrib if you wish`。源音频未修改，Unity 内设置音量与淡入淡出；候选场景 BGM，尚需人工听审确认风格和循环接缝。 |
| `Assets/_WenXinJie/Audio/Ambience/Rain_Window_Ylmir.ogg` | Ylmir；https://opengameart.org/content/rain-loopable | 页面标注 CC0，作者说明为自己在窗边录制并处理的四段可循环雨声。本项目使用 OGG 包中的 `1.ogg`，不修改波形，运行时混音。 |

核实日期：2026-09-22。均未购买；CC0 1.0 https://creativecommons.org/publicdomain/zero/1.0/ 。馆方开放政策 https://www.clevelandart.org/open-access 明确允许无偿下载、修改及再使用指定开放图像，且无需另外申请许可。馆方名称不代表其为游戏背书。

源文件保留于 `AssetSources/`，不随 Unity 构建自动打包；最终导入资源在 `Assets/_WenXinJie/`。抓取页面与对应 URL 保留于 `Docs/AssetResearch/`，导入文件 SHA-256 见 `imported-assets.json`。可运行 `python Tools/prepare-assets.py` 重现裁切调色及雨声选择。

虽然 CC0 无强制署名要求，游戏仍提供鸣谢面板列出作者和来源。修改不是取得授权的手段；许可依据是原作者/馆方的明确声明，不保证所有第三方权利不存在争议。

## 未采用与待办

- Liyan / elerya：https://opengameart.org/content/liyan ，页面标注 CC-BY 3.0。仅研究，未下载或接入。
- Noto Serif CJK：尝试读取官方仓库许可证，但网络请求失败；未下载字体，不宣称已完成字体授权接入。现仍为本地动态系统字体，不可作为可发行字体方案。
- 原创山门背景、林月透明立绘、额外 UI 音效：尚未生成。Unity MCP 工具已连接；`Unity_AssetGeneration_GetModels` 查询约等待三分钟后返回 `Models: []`，当前没有可选生成模型，未调用付费生成。规格见 `ART_DIRECTION.md`。
