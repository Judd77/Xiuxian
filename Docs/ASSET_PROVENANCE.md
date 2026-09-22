# 资源与授权台账

2026-09-22 已接入下列可随商业游戏分发的素材。正式人物与场景为项目专用生成美术；外部二进制素材仅采用 CC0 音频、CC0 早期背景和 OFL 字体。
未来每项记录：文件路径、来源URL/模型、日期、许可证或服务条款依据、署名要求、人工修改、商用审核状态。
AI生成不自动等于无版权风险；不要模仿具体在世画师或复刻现有游戏素材。
Steam AI披露要求应在提交时核查官方当前政策，尚未完成此项核查。

## 已接入资源

| 工程文件                                                    | 作者与来源                                                                                                           | 许可与修改                                                                                                                                                         |
|-------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Assets/_WenXinJie/Art/Backgrounds/Landscape_Cool_1080.jpg` | 髡残，Spring Landscape / 春景图，1666，Cleveland Museum of Art，藏品 1966.367；https://clevelandart.org/art/1966.367 | 官方 API 的 `share_license_status` 为 CC0；裁除装裱边缘、裁切为 16:9、缩至 1920×1080、降低饱和度、冷色调及对比度调整。古画衍生画面，不宣称原创画作或真实山门场景。 |
| `Assets/_WenXinJie/Audio/BGM/AsianOriental2_Tozan.ogg`      | Tozan；https://opengameart.org/content/asianoriental2                                                                | 页面标注 CC0，并写明 `cc0 atrib if you wish`。源音频未修改，Unity 内设置音量与淡入淡出；候选场景 BGM，尚需人工听审确认风格和循环接缝。                             |
| `Assets/_WenXinJie/Audio/Ambience/Rain_Window_Ylmir.ogg`    | Ylmir；https://opengameart.org/content/rain-loopable                                                                 | 页面标注 CC0，作者说明为自己在窗边录制并处理的四段可循环雨声。本项目使用 OGG 包中的 `1.ogg`，不修改波形，运行时混音。                                              |
| `Assets/_WenXinJie/Fonts/NotoSerifSC-Regular.ttf`           | Noto Serif SC / Google Fonts；https://fonts.google.com/noto/specimen/Noto+Serif+SC                                   | SIL Open Font License 1.1，允许与商业软件捆绑和再分发；许可证全文随字体保存在 `Assets/_WenXinJie/Fonts/OFL.txt`。未修改字体名称或字形。                            |

## 原创生成美术

2026-09-22 使用 Codex 内置 OpenAI 图像生成工具制作以下项目专用素材：

| 工程文件                                                    | 创作说明                                                 | 后处理与风险边界                                                                 |
|-------------------------------------------------------------|----------------------------------------------------------|----------------------------------------------------------------------------------|
| `Assets/_WenXinJie/Art/Generated/CloudGate_Premium.png`     | 原创雨晨仙门：白石长阶、云海、青绿群山、玉色与暖金装饰。 | 项目脚本裁切为 1920×1080。提示词明确禁止现有游戏人物、标志、UI、符号和复刻构图。 |
| `Assets/_WenXinJie/Art/Generated/PineRidge_Premium.png`     | 原创松风岭：断碑、避雨亭、远山宗门灯火。                 | 项目脚本输出蓝调与深夜两个版本；未使用外部图片作输入。                           |
| `Assets/_WenXinJie/Art/Generated/LinYue_Premium.png`        | 原创林月立绘的去背景前源图。                             | 仅作为生成过程与透明化编辑的审计源，不被正式媒体目录引用。                       |
| `Assets/_WenXinJie/Art/Generated/LinYue_Premium_Cutout.png` | 原创林月设定：年轻调查者、白青黛汉服、玉牌、收拢纸伞。   | 第二次生成只做去背景，保留角色设计并输出透明 Alpha；没有指定或输入现有游戏角色。 |

用户提出《一念逍遥》作为品质参考时，仅提取“清透水墨、仙雾层次、青绿与暖金、年轻化人物、精致修仙 UI”等抽象方向；未输入其截图，未复刻角色、图标、品牌、具体构图或界面。生成记录、摘要与 SHA-256 保存在
`Docs/AssetResearch/generated-assets.json`，衍生处理由 `python Tools/prepare-premium-art.py` 重现。

生成输出不等于绝对唯一，也不能自动排除所有相似性风险。发行前仍需人工反向搜图、角色相似性复核和平台 AI 内容披露核查。当前生成素材不附带“禁止商用”第三方素材许可；项目也没有通过修改来规避任何原素材限制。

核实日期：2026-09-22。均未购买；CC0 1.0 https://creativecommons.org/publicdomain/zero/1.0/ 。馆方开放政策 https://www.clevelandart.org/open-access
明确允许无偿下载、修改及再使用指定开放图像，且无需另外申请许可。馆方名称不代表其为游戏背书。

下载的第三方源文件保留于 `AssetSources/`，不随 Unity 构建自动打包；最终导入资源在 `Assets/_WenXinJie/`。抓取页面与对应 URL 保留于 `Docs/AssetResearch/`。首批第三方资源哈希见
`imported-assets.json`，生成美术哈希见 `generated-assets.json`。可运行 `python Tools/prepare-assets.py` 重现早期背景和雨声选择；`python Tools/prepare-premium-art.py` 重现正式背景裁切、夜景调色和
UI 纹理。

虽然 CC0 无强制署名要求，游戏仍提供鸣谢面板列出作者和来源。修改不是取得授权的手段；许可依据是原作者/馆方的明确声明，不保证所有第三方权利不存在争议。

## 未采用与待办

- Liyan / elerya：https://opengameart.org/content/liyan ，页面标注 CC-BY 3.0。仅研究，未下载或接入。
- 林月表情差分、另外两名主要角色、六处完整场景和额外 UI 音效仍待制作。Unity 自带生成服务模型列表为空，本轮正式图像由 Codex 内置生成工具完成并留档。
- 当前只接入 CC0 与 OFL 资源。禁止把仅限非商业、禁止改编、来源不明、游戏拆包或无法保留授权证据的资源导入 `Assets/_WenXinJie`。
