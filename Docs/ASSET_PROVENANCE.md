# 资源与授权台账

2026-09-22 已接入下列可随商业游戏分发的素材。正式人物与场景为项目专用生成美术；外部二进制素材仅采用 CC0 音频、CC0 早期背景和 OFL 字体。
未来每项记录：文件路径、来源URL/模型、日期、许可证或服务条款依据、署名要求、人工修改、商用审核状态。
AI生成不自动等于无版权风险；不要模仿具体在世画师或复刻现有游戏素材。
Steam AI披露要求应在提交时核查官方当前政策，尚未完成此项核查。

## 2026-10-12 API人物表情

- 沿用用户授权的API方式与指定模型`gpt-image-2.5-sunburst`，通过imagegen原装CLI的images.edit接口，以五名角色各自的项目自有立绘为编辑目标，制作林月担忧、阿澈不安、顾衡愧疚、沈砚愧疚与许澄坚定差分。
- API返回图、面部蒙版、合成权重、完整提示词和最终源图位于`AssetSources/Art/Generated/Expressions/`；运行资源位于`Assets/_WenXinJie/Art/Generated/Expressions/`。正式版21节点、Demo 10节点接入，剧情revision仍为35；无新增文字、条件、效果或存档变量。
- 五张返回图均为1024×1536。以椭圆面部区域及7像素高斯羽化合成，只采用该区域的生成RGB，复制基础立绘整张Alpha；区域外人物、服饰及透明轮廓保持。`python Tools/prepare-character-expressions.py`可重现，输入、输出哈希及完整请求提示词见`AssetResearch/character-expressions-api.json`。
- 未输入第三方人物素材或指定画师；凭据没有写入项目。后端模型身份、代理商用条款与发行许可复核状态沿用现有记录，尚未独立核实。

## 已接入资源

| 工程文件                                                    | 作者与来源                                                                                                           | 许可与修改                                                                                                                                                         |
|-------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Assets/_WenXinJie/Art/Backgrounds/Landscape_Cool_1080.jpg` | 髡残，Spring Landscape / 春景图，1666，Cleveland Museum of Art，藏品 1966.367；https://clevelandart.org/art/1966.367 | 官方 API 的 `share_license_status` 为 CC0；裁除装裱边缘、裁切为 16:9、缩至 1920×1080、降低饱和度、冷色调及对比度调整。古画衍生画面，不宣称原创画作或真实山门场景。 |
| `Assets/_WenXinJie/Audio/BGM/AsianOriental2_Tozan.ogg`      | Tozan；https://opengameart.org/content/asianoriental2                                                                | 页面标注 CC0，并写明 `cc0 atrib if you wish`。源音频未修改，Unity 内设置音量与淡入淡出；候选场景 BGM，尚需人工听审确认风格和循环接缝。                             |
| `Assets/_WenXinJie/Audio/Ambience/Rain_Window_Ylmir.ogg`    | Ylmir；https://opengameart.org/content/rain-loopable                                                                 | 页面标注 CC0，作者说明为自己在窗边录制并处理的四段可循环雨声。本项目使用 OGG 包中的 `1.ogg`，不修改波形，运行时混音。                                              |
| `Assets/_WenXinJie/Fonts/NotoSerifSC-Regular.ttf`           | Noto Serif SC / Google Fonts；https://fonts.google.com/noto/specimen/Noto+Serif+SC                                   | SIL Open Font License 1.1，允许与商业软件捆绑和再分发；许可证全文随字体保存在 `Assets/_WenXinJie/Fonts/OFL.txt`。未修改字体名称或字形。                            |
| `Assets/_WenXinJie/Fonts/ZhiMangXing-Regular.ttf`           | Zhi Mang Xing / Google Fonts；https://fonts.google.com/specimen/Zhi+Mang+Xing                                        | SIL Open Font License 1.1；用于首页书法题字。许可证全文保存在 `Assets/_WenXinJie/Fonts/ZhiMangXing-OFL.txt`，未修改字体名称或字形。                                |
| `Assets/_WenXinJie/Fonts/ZCOOLXiaoWei-Regular.ttf`          | ZCOOL XiaoWei / Google Fonts；https://fonts.google.com/specimen/ZCOOL+XiaoWei                                        | SIL Open Font License 1.1；用于首页副标题与菜单。许可证全文保存在 `Assets/_WenXinJie/Fonts/ZCOOLXiaoWei-OFL.txt`，未修改字体名称或字形。                           |

## 原创生成美术

2026-09-22 使用 Codex 内置 OpenAI 图像生成工具制作以下项目专用素材：

| 工程文件                                                    | 创作说明                                                 | 后处理与风险边界                                                                     |
|-------------------------------------------------------------|----------------------------------------------------------|--------------------------------------------------------------------------------------|
| `AssetSources/Art/Generated/CloudGate_Premium.png`          | 原创雨晨仙门：白石长阶、云海、青绿群山、玉色与暖金装饰。 | 制作母图保留在 `AssetSources`，项目脚本裁切为 1920×1080；不作为 Unity 运行资源导入。 |
| `AssetSources/Art/Generated/PineRidge_Premium.png`          | 原创松风岭：断碑、避雨亭、远山宗门灯火。                 | 制作母图保留在 `AssetSources`，项目脚本输出蓝调与深夜两个版本。                      |
| `AssetSources/Art/Generated/SteleCorridor_Premium.png`      | 原创旧碑廊：雨后石碑长廊、封存经匣与云海远山。           | 母图保留在 `AssetSources`，项目脚本裁切为 1920×1080；用于名册调查路线。              |
| `AssetSources/Art/Generated/DiscipleQuartersNight_Premium.png` | 原创弟子居所夜景：搜查后的房间、巡夜灯与湿润山廊。    | 母图保留在 `AssetSources`，项目脚本裁切为 1920×1080；用于夜间搜屋剧情。              |
| `AssetSources/Art/Generated/MountainHeartGate_Premium.png`  | 原创山腹石门：无缝巨门、炉火远影与手印凝痕。             | 母图保留在 `AssetSources`，项目脚本裁切为 1920×1080；用于共鸣路线。                  |
| `AssetSources/Art/Generated/LinYue_Premium.png`             | 原创林月立绘的去背景前源图。                             | 仅作为生成过程与透明化编辑的审计源，不被 Unity 导入或正式媒体目录引用。              |
| `Assets/_WenXinJie/Art/Generated/LinYue_Premium_Cutout.png` | 原创林月设定：年轻调查者、白青黛汉服、玉牌、收拢纸伞。   | 第二次生成只做去背景，保留角色设计并输出透明 Alpha；没有指定或输入现有游戏角色。     |

用户提出《一念逍遥》作为品质参考时，仅提取“清透水墨、仙雾层次、青绿与暖金、年轻化人物、精致修仙 UI”等抽象方向；未输入其截图，未复刻角色、图标、品牌、具体构图或界面。生成记录、摘要与 SHA-256 保存在
`Docs/AssetResearch/generated-assets.json`，衍生处理由 `python Tools/prepare-premium-art.py` 重现。

生成输出不等于绝对唯一，也不能自动排除所有相似性风险。发行前仍需人工反向搜图、角色相似性复核和平台 AI 内容披露核查。当前生成素材不附带“禁止商用”第三方素材许可；项目也没有通过修改来规避任何原素材限制。

核实日期：2026-09-24。均未购买；CC0 1.0 https://creativecommons.org/publicdomain/zero/1.0/ 。馆方开放政策 https://www.clevelandart.org/open-access
明确允许无偿下载、修改及再使用指定开放图像，且无需另外申请许可。馆方名称不代表其为游戏背书。

下载的第三方源文件保留于 `AssetSources/`，不随 Unity 构建自动打包；最终导入资源在 `Assets/_WenXinJie/`。抓取页面与对应 URL 保留于 `Docs/AssetResearch/`。首批第三方资源哈希见
`imported-assets.json`，生成美术哈希见 `generated-assets.json`。可运行 `python Tools/prepare-assets.py` 重现早期背景和雨声选择；`python Tools/prepare-premium-art.py` 重现正式背景裁切、夜景调色和
UI 纹理。

虽然 CC0 无强制署名要求，游戏仍提供鸣谢面板列出作者和来源。修改不是取得授权的手段；许可依据是原作者/馆方的明确声明，不保证所有第三方权利不存在争议。

## 2026-10-09 API 医舍背景

- 用户授权由内置出图改用 API。调用本机已配置代理的 Image API，使用其列出的模型标识 `gpt-image-2.5-sunburst`；后端真实模型身份尚未独立核实。凭据只用于本次进程，没有保存到工程。
- 原图：`AssetSources/Art/Generated/MedicineHouse_Premium_v1.png`；完整提示词：同目录 `MedicineHouse_Premium_v1.prompt.txt`。风格参考是项目既有 `DiscipleQuartersNight_Premium.png`，未输入第三方游戏素材。
- 运行资源：`Assets/_WenXinJie/Art/Backgrounds/MedicineHouse_Premium_1080.jpg`。居中裁切并以 Lanczos 缩放为 1920×1080，JPEG 质量 94；正式版与 Demo 各 10 个医舍节点使用。
- 请求参数、返回尺寸与源图／衍生图 SHA-256：`Docs/AssetResearch/medicine-house-api.json`。来源记录已保存；代理服务商商用条款与发行许可复核仍待完成，不能仅凭生成成功认定许可已核验。

## 2026-10-09 API 主线背景

- 经 imagegen 原装 CLI 通过当前代理 Image API 调用 `gpt-image-2.5-sunburst`，生成南沟村、议事堂与炉房。以项目自有山门、医舍、山腹石门源图为风格参考，未输入第三方游戏图；后端模型身份与代理许可核实状态沿用医舍记录。
- 源图与完整提示词：`AssetSources/Art/Generated/` 下的 `NangouVillage_Premium_v1`、`CouncilHall_Premium_v1`、`FurnaceChamber_Premium_v1`（`.png` 与 `.prompt.txt`）。运行图为 `Assets/_WenXinJie/Art/Backgrounds/` 下同名 `*_Premium_1080.jpg`。
- 三张源图均返回 1672×941，居中裁切／缩放到 1920×1080。村口另以亮度 0.40、饱和度 0.40、32% 青黛色混合制作 `NangouVillage_Premium_Night_1080.jpg`；是日景调色衍生，不宣称独立生成。
- 正式版使用村景 21 节点（其中夜景 4）、议事堂 25、炉房 17；Demo 使用炉房 7。逐图参考来源、完整参数及哈希见 `Docs/AssetResearch/mainline-backgrounds-api.json`，裁切和夜景处理可由 `Tools/prepare-premium-art.py` 重现。

## 2026-10-09 API 角色与谷口

- 沿用已授权的 imagegen 原装 CLI、代理 Image API 与模型标识 `gpt-image-2.5-sunburst`。四人以项目自有林月透明立绘为风格参考；谷口以南沟村为参考，改道后以谷口原图为参考。凭据不写入工程。
- 源图与完整提示词位于 `AssetSources/Art/Generated/`：`GuHeng_Premium_v1`、`AChe_Premium_v1`、`XuCheng_Premium_v1`、`ShenYan_Premium_v1`、`DiversionValley_Premium_v1`、`DiversionValley_Aftermath_v1`（`.png` 与 `.prompt.txt`）。
- 四人返回 1024×1536，已自带透明通道，逐字节复制为 `Assets/_WenXinJie/Art/Generated/*_Premium_Cutout.png`，不再抠色或裁切。正式版接入 27 节点，Demo 阿澈同步 3 节点。
- 谷口两张源图返回 1672×941，裁切／缩放至 1920×1080，运行资源在 `Assets/_WenXinJie/Art/Backgrounds/`。谷口夜景由日景调色衍生，参数与村口夜景一致；改道后的药田损毁图为单独生成。三种状态接入正式版 19 节点。
- 参数、源图／运行图路径、完整提示词及哈希见 `Docs/AssetResearch/characters-valley-api.json`。加工可由 `Tools/prepare-premium-art.py` 重现；后端模型身份与代理许可核实状态沿用前述 API 记录。

## 2026-10-09 API 调查地点背景

- 沿用已授权 Image API 与 imagegen 原装 CLI，模型标识 `gpt-image-2.5-sunburst`；只以项目自有南沟村、医舍、议事堂、司籍房、炉房与山门源图作为风格参考。模型与代理许可核实状态沿用前述 API 记录。
- 八张源图与完整提示词位于 `AssetSources/Art/Generated/`：`CraftsmanHome_Premium_v1`、`VillagerHome_Premium_v1`、`XuFamilyHome_Premium_v1`、`OldBellTower_Premium_v1`、`RegistryOffice_Premium_v1`、`Storehouse_Premium_v1`、`TransferDesk_Premium_v1`、`ExaminationCourt_Premium_v1`（`.png` 与 `.prompt.txt`）。分别对应工匠家、村民家、许家、旧钟楼、司籍房、仓房、交接台、考校台。
- 返回尺寸均为 1672×941；居中裁切并以 Lanczos 缩放为 1920×1080，JPEG 质量 94。运行资源位于 `Assets/_WenXinJie/Art/Backgrounds/`，同名前缀加 `_Premium_1080.jpg`。正式版 38 节点、Demo 19 节点接入；没有改变证据条件、变量或分支效果。
- 提示词、参考、请求参数、源图与运行图哈希及节点清单见 `Docs/AssetResearch/investigation-backgrounds-api.json`。加工已纳入 `Tools/prepare-premium-art.py`；凭据不写入工程。

## 未采用与待办

- Liyan / elerya：https://opengameart.org/content/liyan ，页面标注 CC-BY 3.0。仅研究，未下载或接入。
- 五名角色表情差分和额外 UI 音效仍待制作。既有内置生成美术与新增 API 美术分别留档。
- 当前只接入 CC0 与 OFL 资源。禁止把仅限非商业、禁止改编、来源不明、游戏拆包或无法保留授权证据的资源导入 `Assets/_WenXinJie`。
