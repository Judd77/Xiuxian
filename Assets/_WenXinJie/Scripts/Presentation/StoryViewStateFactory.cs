using System.Linq;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    internal sealed class StoryViewStateFactory
    {
        private readonly StoryAssetCatalog _assets;

        public StoryViewStateFactory(StoryAssetCatalog assets)
        {
            _assets = assets;
        }

        public StoryViewState Create(StorySession session)
        {
            var node = session.Current;
            var journal = BuildJournal(session);
            return new(
                node.chapter,
                node.location,
                node.speaker,
                session.CurrentText,
                BuildStatus(session, journal),
                node.terminal ? (node.id.StartsWith("ending.") ? "本周目终" : "未完待续") : "继续",
                node.terminal,
                _assets.GetBackground(node.background),
                _assets.GetPortrait(node.portrait),
                journal,
                node.choices.Where(choice => !choice.hidden || session.IsAvailable(choice))
                    .Select(choice => CreateChoice(session, choice)).ToArray());
        }

        private static ChoiceViewState CreateChoice(StorySession session, StoryChoice choice)
        {
            return new(
                choice.id,
                choice.text,
                choice.lockedReason,
                session.IsAvailable(choice));
        }

        private static string BuildStatus(StorySession session, ClueJournalState journal)
        {
            var clues = journal.Entries.Count(entry => entry.Unlocked);
            if (session.Revision >= 16)
            {
                var skill = session.GetValue("story.skill") > 0 ? "可用" : "未习得";
                return "问心劫    /    已核线索 " + clues + "/" + journal.Entries.Length
                       + "    /    辨气 " + skill;
            }
            if (session.Revision >= 15)
            {
                var skill = session.GetValue("story.skill") > 0 ? "已习得" : "未习得";
                return "问心劫    /    线索 " + clues + "/" + journal.Entries.Length
                       + "    /    辨气 " + skill;
            }
            if (IsNewStory(session.Current.id))
            {
                var skill = session.GetValue("case.learned") > 0 ? "已习得" : "未习得";
                var record = session.GetValue("case.sealLost") > 0 ? "原回执被封" : "可核对";
                return "旧号疑云    /    线索 " + clues + "/" + journal.Entries.Length
                       + "    /    辨气 " + skill + "    /    炉房记录 " + record;
            }

            var attention = session.GetValue("risk.attention") > 0 ? "已惊动" : "未惊动";
            return "第 " + session.GetValue("day") + " 日    /    余暇 " + session.GetValue("actions")
                   + "    /    修为 " + session.GetValue("cultivation")
                   + "    /    林月信任 " + session.GetValue("trust.linYue")
                   + "    /    线索 " + clues + "/" + journal.Entries.Length + "    /    宗门 " + attention;
        }

        private static ClueJournalState BuildJournal(StorySession session)
        {
            if (session.Revision >= 16)
            {
                return BuildFormalJournal(session);
            }
            if (session.Revision >= 15)
            {
                return BuildRewrittenJournal(session);
            }
            if (IsNewStory(session.Current.id))
            {
                return BuildCaseJournal(session);
            }

            return new(new[]
            {
                new ClueEntryViewState(
                    "外门名册抄页",
                    "物证 · 旧碑廊",
                    "残页上的名字被朱笔圈过，部分人名又遭刻意刮除。页脚的归档记号指向药炉，而非外门执事堂。",
                    session.GetValue("clue.register") > 0),
                new ClueEntryViewState(
                    "青玉扣",
                    "物证 · 松风岭",
                    "玉扣内侧刻着“沈砚”二字，边缘残留暗褐血痕。它与名册中被抹去的姓名能够相互印证。",
                    session.GetValue("clue.jade") > 0),
                new ClueEntryViewState(
                    "山腹共鸣",
                    "异象 · 山腹石门",
                    "运转引气诀时，山腹深处会传来与经脉同频的回响。那并非自然灵脉，更像许多人的呼吸被困在石壁之后。",
                    session.GetValue("mark.resonance") > 0),
                new ClueEntryViewState(
                    "药炉账册",
                    "物证 · 药炉",
                    "领药量与失踪弟子的名字一一对应。炉房以弟子灵气维系山脉裂隙，所谓考校其实在挑选祭品。",
                    session.GetValue("clue.ledger") > 0),
                new ClueEntryViewState(
                    "阿澈的证词",
                    "人证 · 弟子居所",
                    "阿澈亲眼见过炉中仍有活人。他提醒：骤然停炉会让山脉裂隙失控，救人必须先找到替代阵法。",
                    session.GetValue("clue.witness") > 0),
                new ClueEntryViewState(
                    "替阵测绘",
                    "阵图 · 山腹石门",
                    "旧灵脉能够分担炉中阵压，但不能独自承受。测得的转接点为逐批撤人提供了可能，完整原图仍需另行保全。",
                    session.GetValue("clue.array") > 0)
            });
        }

        private static ClueJournalState BuildRewrittenJournal(StorySession session)
        {
            return new(new[]
            {
                new ClueEntryViewState("阿澈的去向", "文书 · 山门",
                    "销名册称阿澈离山，山门却没有他验帖的印。两处记录互相矛盾，仍需找到本人。", true),
                new ClueEntryViewState("辨气的边界", "术法 · 入门书",
                    "物件上的余息能分新旧；不能直接读出人名。先用已知新旧的符做对照。",
                    session.GetValue("story.skill") > 0),
                new ClueEntryViewState("运车灰样", "术证 · 山腹坡道",
                    "与仓房同批灰比较，运车灰上有新近的修行者余息；来源可能是守卫，不能直接认作阿澈。",
                    session.GetValue("story.ash") > 0),
                new ClueEntryViewState("阿澈的证词", "人证 · 山腹",
                    "阿澈被带去坐榻，隔墙有人两下敲门。安神药使他无法确认对方姓名。",
                    session.GetValue("story.ache") > 0),
                new ClueEntryViewState("许澄的半枚玉扣", "互证 · 家属",
                    "阿澈带出的半扣与许澄姐姐保存的另一半吻合；隔墙人的身份仍待亲见。",
                    session.GetValue("story.xucheng") > 0),
                new ClueEntryViewState("南沟旧灾原簿", "文书 · 村口",
                    "南沟曾遇险，东坡并非同刻受冲；顾衡将两次损符写作全村同时遇险。",
                    session.GetValue("story.village") > 0),
                new ClueEntryViewState("初试停铃", "阵证 · 观测台",
                    "初试本被涂去求停记录；旧阵接线与陶叔证词表明铃曾响，沈砚却没有立刻停。",
                    session.GetValue("story.bell") > 0),
                new ClueEntryViewState("谷口复测", "术证 · 山谷",
                    "旧图第三根石柱已经移位。小试会先冲村口，不能照图直接开闸。",
                    session.GetValue("story.survey") > 0),
                new ClueEntryViewState("共同叫停约定", "行动 · 议事堂",
                    "炉门、村口分别守钟，家属与村民掌握叫停权；谁受伤、谁搬迁必须记名。",
                    session.GetValue("story.contract") > 0)
            });
        }

        private static ClueJournalState BuildFormalJournal(StorySession session)
        {
            return new(new[]
            {
                new ClueEntryViewState("离山手续矛盾", "文书对照 · 山门",
                    "销名册写阿澈卯初离山；同刻的门阵朱纹为空。与真实离山者的完整朱纹比较，确认少了验帖。仍可能走偏道，去向未明。",
                    session.GetValue("case.record") > 0),
                new ClueEntryViewState("登仙帖背的旧名", "实物 · 司籍房",
                    "帖背残留一个“砚”字，十年前的山灾册记沈砚失踪。旧帖重新使用不违规；残字不能证明沈砚仍活着。",
                    session.GetValue("case.post") > 0),
                new ClueEntryViewState("运药车与纸角", "目击 · 山腹坡道",
                    "车沿有昨夜包药的折角，车次已记下。纸可能来自医舍丢弃物；没有亲见车内的人。",
                    session.GetValue("case.cart") > 0),
                new ClueEntryViewState("辨气的边界", "术法 · 入门书",
                    "以已知新旧的两张废符校准。辨气能比较近期余息，无法辨认施术者姓名。",
                    session.GetValue("story.skill") > 0),
                new ClueEntryViewState("双灰对照", "术证 · 仓房",
                    "同批未运灰无新息，运车灰有今日余息。车接近过施术的人；守卫也可能留下余息，不能认作阿澈。",
                    session.GetValue("case.ashCorrect") > 0),
                new ClueEntryViewState("考校副册", "文书 · 考校台",
                    "正册已写阿澈离山，副册仍等他应试。归档时辰已抄下，可与山门记录核对。",
                    session.GetValue("case.deputy") > 0),
                new ClueEntryViewState("炉房外榻的流向", "亲见与术证 · 山腹",
                    "石柱先承力，隔壁榻随后加重。解开一人会转移冲击；阿澈获救当夜，村口护符裂了一张。",
                    session.GetValue("story.ache") > 0),
                new ClueEntryViewState("两张榻号", "现场抄录 · 炉房",
                    "救阿澈前抄下他与隔壁病人的榻号。床位还需与伤签核名，不能直接当作姓名。",
                    session.GetValue("case.bedNumber") > 0),
                new ClueEntryViewState("阿澈的证词", "当事人说法 · 医舍",
                    "床底鞋是他留下的求救痕迹；隔墙有人两下敲门。安神药影响了他对门漆和姓名的记忆。",
                    session.GetValue("story.ache") > 0),
                new ClueEntryViewState("许澄的半枚玉扣", "实物互证 · 许家",
                    "阿澈带出的半扣与许家姐姐保管的另一半吻合。扣可能被别人携带，仍需让门里的人自行回应身份。",
                    session.GetValue("story.xucheng") > 0),
                new ClueEntryViewState("伤签与车次", "文书对照 · 医舍",
                    "乙七的用药在二更后变动；交接单显示里门并非整夜不开。床号仍不是姓名，须由见过病人的人核对。",
                    session.GetValue("l.medical") > 0 && session.GetValue("l.shift") > 0),
                new ClueEntryViewState("听脉的边界", "术法 · 医舍",
                    "用阿澈与空房作对照，薄墙后可听活人的呼吸；雨声、厚石和阵气会干扰，听脉不能报出姓名。",
                    session.GetValue("l.hear") > 0),
                new ClueEntryViewState("许澄亲自回应", "人证 · 山腹里门",
                    "姐姐以乳名称呼，门里的人用姐妹私下的称呼回答；听脉确认有活人，姐姐确认说话者身份。",
                    session.GetValue("l.familyAsked") > 0),
                new ClueEntryViewState("第二次救援名单", "行动记录 · 山道",
                    session.GetValue("l.danqi") > 0
                        ? "许澄、外榻少年与段七获救；阿澈旧伤复发，陈闻替车次担责，村口又裂一符。姓名仍需逐一核实。"
                        : "许澄与外榻少年获救；段七最后见于里榻，去向待查。未见到不能写成已死。",
                    session.GetValue("l.names") > 0),
                new ClueEntryViewState("许澄的完整证词", "本人署名 · 临时医舍",
                    "她同意第一夜坐榻，第二夜要求离开被拒，去向栏还提前写作离山。她要在堂上自己作证。",
                    session.GetValue("l.testimony") > 0),
                new ClueEntryViewState("家属共同定行动", "行动约定 · 医舍",
                    "林月写炉门和退路，姐姐记停手条件，许澄确认铃放在伸手可及处；三人可逐项核查。",
                    session.GetValue("l.protocol") > 0),
                new ClueEntryViewState("南沟旧灾", "文书 · 山下",
                    "南沟先报符裂，东坡近半个时辰后换符；旧符损坏方式也不同。十年前的余息已散，辨气不能补报当夜详情。",
                    session.GetValue("g.oldCharm") > 0 && session.GetValue("g.bellBook") > 0),
                new ClueEntryViewState("顾衡的改记", "本人更正 · 村议",
                    "顾衡当众承认把两处灾情合写同刻，也把南沟死者写轻；更正写在旧呈报旁，原字未擦。",
                    session.GetValue("g.corrected") > 0),
                new ClueEntryViewState("撤离的实际脚程", "现场实走 · 谷口",
                    "背老妇过第一坡已用完旧簿所写的半炷香；按慢者先行、药箱与孩子分开过狭路重排。",
                    session.GetValue("g.walked") > 0),
                new ClueEntryViewState("移位的第三柱", "局部术证 · 谷口",
                    "洪水后第三柱不在旧图原位；小试显示村口先亮、炉门后热。只量出局部先后，不能预言下一次灵潮强度。",
                    session.GetValue("g.thirdPillar") > 0),
                new ClueEntryViewState("村民握停钟", "行动约定 · 村议",
                    "工匠由村民选出守钟，家属守炉门铃，陶叔关支路；任何一端喊停，两端撤力。不是整村同意永远开谷口。",
                    session.GetValue("g.hammer") > 0),
                new ClueEntryViewState("分开的村民答复", "本人决定 · 村议",
                    "愿试走、愿借屋、仍反对开谷口的户分栏记录。木匠要求公开冬药账，不能写成全村一致同意改道。",
                    session.GetValue("g.villageRecord") > 0),
                new ClueEntryViewState("两版阵图", "原图对照 · 观测台",
                    "沈砚签署的初案已有供人坐的榻；祁元肃后来加高承压并使值守常例化。两人的责任须分开查。",
                    session.GetValue("s.firstBed") > 0 && session.GetValue("s.raisedLimit") > 0),
                new ClueEntryViewState("初试停铃", "阵证 · 观测台",
                    "初试本的“有人求停”被涂，破铃接不上原图支路，完铃接得上；受控小试可叫停。仍需周望本人讲当年的决定。",
                    session.GetValue("s.bellDiagram") > 0 && session.GetValue("s.smallFlow") > 0),
                new ClueEntryViewState("周望求停", "本人证词 · 周望屋前",
                    "周望明确要原簿写“周望求停，沈砚未停”。沈砚当面补记，周望收下纸，没有说原谅。",
                    session.GetValue("s.zhouWords") > 0),
                new ClueEntryViewState("有住户的旧谷道", "阵图与现场 · 谷口",
                    "泄流道经过三户与药田，开闸第一下仍需人守台。图须标住户、撤路与实测柱位，不能只画灵力最顺的线。",
                    session.GetValue("s.homes") > 0),
                new ClueEntryViewState("分开的石台权力", "行动约定 · 观测台",
                    "沈砚读图，陶叔执钥，家属守炉门；村口停钟待村民自己选人。会读图与有权叫停是两件事。",
                    session.GetValue("s.sharedKey") > 0),
                new ClueEntryViewState("谷口复测", "术证 · 山谷",
                    "旧图第三根石柱已经移位。未复测前，不能照图直接开闸。",
                    session.GetValue("story.survey") > 0),
                new ClueEntryViewState("共同叫停约定", "行动 · 议事堂",
                    "炉门与村口都须有人守钟；家属和村民能叫停，伤亡与迁离按真名记录。",
                    session.GetValue("story.contract") > 0),
                new ClueEntryViewState("三处加快的记录", "时辰互证 · 山腹与村口",
                    "炉符耗量、石柱闷响与无雨裂符按时辰先后加快。至迟明夜须准备，无法预言灵潮准确一刻。",
                    session.GetValue("h.deadline") > 0),
                new ClueEntryViewState("分开的三份责任", "公开对质 · 议事堂",
                    "沈砚初案有人榻且未按周望求停；祁元肃加高承压并改去向；顾衡合写旧灾并写轻死者。不得以一人受罚抹去其余责任。",
                    session.GetValue("h.shen") > 0 && session.GetValue("h.qi") > 0
                    && session.GetValue("h.gu") > 0),
                new ClueEntryViewState("可叫停的小试", "共同实测 · 谷口",
                    "沈砚读图、陶叔执钥、家属与村人握钟。村口先亮，炉门后热；这次小试仅量局部先后，不保证下一股同样轻。",
                    session.GetValue("h.test") > 0),
                new ClueEntryViewState("三户分别同意暂迁", "本人决定 · 谷口",
                    "老妇先过狭岔，父女带药与平车，木匠借屋并要求公开冬药账。谁也没有答应永久舍弃家与药田。",
                    session.GetValue("h.house1") > 0 && session.GetValue("h.house2") > 0
                    && session.GetValue("h.house3") > 0),
                new ClueEntryViewState("第三声后的执行", "行动 · 山腹石台",
                    "许澄够得到停铃，陶叔能接支路；按新测石柱辨流，第三声后两端撤力。药田受损，三户须暂迁，救出者仍需养伤。",
                    session.GetValue("e.bellPlaced") > 0 && session.GetValue("e.flowMeasured") > 0),
                new ClueEntryViewState("夺台后的轮换真账", "本人伤情 · 山腹石台",
                    "主角自愿坐台也受了长期伤，不能替下一人答应。停铃与伤情公开，钥匙交家属、村民与出炉弟子共同保管。",
                    session.GetValue("e.seizeRecord") > 0),
                new ClueEntryViewState("拆掉单令符", "行动 · 山腹石台",
                    "强按祁元肃保住这一夜，也由主角剥夺他的拒绝权。台上只听一人的符已拆，家属仍未因此原谅主角。",
                    session.GetValue("e.forceSymbol") > 0),
                new ClueEntryViewState("公开的守阵真账", "三方留存 · 司籍房",
                    "今晚仍守旧阵；家属持真名册，村民持旧图与未测处，医舍留今日伤情。下一次迁路可据此追问。",
                    session.GetValue("e.publicCopies") > 0)
            });
        }

        private static ClueJournalState BuildCaseJournal(StorySession session)
        {
            var recordsSeen = session.GetValue("case.guess") > 0;
            return new(new[]
            {
                new ClueEntryViewState("登仙帖刮名", "物证 · 司籍房",
                    "帖子背面的旧名被刮去，残笔可辨“沈砚”。刮名本身不说明他如今身在何处。", true),
                new ClueEntryViewState("死亡注销抄页", "文书 · 司籍房",
                    "十年前的注销抄页记沈砚“亡于阵故”。原册锁在执事房，司籍弟子只能查抄页。", true),
                new ClueEntryViewState("当旬领灰回执", "文书 · 炉房",
                    "本旬回执沿用沈砚的旧编号领灰，盖有今日炉印。编号不能单独证明领用人的身份。",
                    recordsSeen),
                new ClueEntryViewState("辨气入门", "术法 · 司籍房入门册",
                    "以新旧朱印练习后，可辨气息残留的新旧与流向；不能辨认姓名或人数。",
                    session.GetValue("case.learned") > 0),
                new ClueEntryViewState("炉灰内侧活息", "术证 · 交接台",
                    "回执灰中有今日从炉门内侧带出的活人气息，旧账误抄无法解释；运灰者与息位中人仍需另证。",
                    session.GetValue("case.verified") > 0),
                new ClueEntryViewState("旧号与身份", "推断 · 司籍房",
                    "旧号今天被使用，却不能据编号直接认定持帖者、转运者与炉中人为同一人。",
                    session.GetValue("case.conclusion") > 0),
                new ClueEntryViewState("运灰车时辰", "物证 · 运灰小径",
                    "车辙、轮值与登记时辰不一致。它能确认有人在换班时转运，单凭车辙不能认出帘后的人。",
                    session.GetValue("case.cartTrace") > 0),
                new ClueEntryViewState("阿澈的证词", "人证 · 外门居所",
                    "阿澈见过炉中活人，但因药与阵压混淆姓名。须用随身物和轮值时辰核对。",
                    session.GetValue("case.ache") > 0),
                new ClueEntryViewState("炉房交接抄页", "文书 · 炉房",
                    "旧编号仍在本旬使用，交接时辰可与运灰痕迹核对；抄页没有承压者亲笔签名。",
                    session.GetValue("case.ledgerCopy") > 0),
                new ClueEntryViewState("活人身份线索", "互证 · 家属与轮值",
                    "家属物件与换班记录指向一名活人。旧号、持帖者与炉中人不能直接画等号。",
                    session.GetValue("case.identityLead") > 0),
                new ClueEntryViewState("副脉回流实测", "术证 · 观测台",
                    "短时分流测出回流点，骤停会伤及村口；沈砚底稿缺一处测点，不能照图直接开炉。",
                    session.GetValue("case.arrayMeasured") > 0),
                new ClueEntryViewState("村落撤离准备", "行动 · 山下村落",
                    "护符只能抵一轮小幅回流。村民需知情并先撤出低处住户，古道上的慢行者需要更多时间。",
                    session.GetValue("case.villageReady") > 0),
                new ClueEntryViewState("公开见证记录", "记录 · 旧碑廊",
                    "已核实的姓名、测量与未知处须分开公布。家属与村民可以据此监督后续阵法行动。",
                    session.GetValue("case.publicRecord") > 0),
                new ClueEntryViewState("沈砚的初案签名", "物证 · 旧阵底稿",
                    "第一版阵图已有由沈砚签署的人息位。宗门后来提高承压上限，并不能抹去最初设计者的责任。",
                    session.GetValue("case.shenAccount") > 0),
                new ClueEntryViewState("帖背刮痕拓片", "物证 · 登仙帖",
                    "旧批帖纸被刮名后重新盖印。拓片和原纸可与旧图库纸纹核对，单凭它不能认定沈砚仍活着。",
                    session.GetValue("case.postPreserved") > 0),
                new ClueEntryViewState("交接人值守时辰", "记录 · 司籍房",
                    "三处空白去向都落在陈闻值守时段。可私下向他查轮值，但他未必知道炉房真相。",
                    session.GetValue("case.clerkLead") > 0),
                new ClueEntryViewState("双灰对照样本", "术证 · 炉房交接台",
                    "同批未入炉灰无活息，回执灰有今日从内侧带出的活息。排除灰料本身，仍不能辨认运灰者与息位中人的身份。",
                    session.GetValue("case.controlSample") > 0),
                new ClueEntryViewState("药碗残药", "物证 · 外门居所",
                    "安神散在二更后才进了阿澈的碗。他可能记错先后与姓名；药物不能解释服药前留下的玉扣。",
                    session.GetValue("case.sedative") > 0),
                new ClueEntryViewState("家属参与侧门查验", "行动 · 炉房侧门",
                    "许澄的姐姐知晓开门可能冲击山下护符，亲自见证炉内回声，同意在有人稳阵时再用钥匙救人。",
                    session.GetValue("case.familyConsent") > 0),
                new ClueEntryViewState("护符领料簿", "文书 · 护符工坊",
                    "领灰增加后村口护符耗损减缓，但雨损护符与停炉无关。须剔除雨损，按现存护符余量重算撤离顺序。",
                    session.GetValue("case.charmLedger") > 0),
                new ClueEntryViewState("双侧回流时差", "术证 · 废副脉",
                    "半息试验中村口试纸先黑，炉侧迟一息才热。两处风险有先后，仍需当事人知情并设叫停信号。",
                    session.GetValue("case.returnMeasured") > 0),
                new ClueEntryViewState("家属核名", "人证 · 山腹观测台",
                    "许澄的家属以配扣和轮值确认姓名，并获知救援风险。旧号仍不能代替活人的姓名与同意。",
                    session.GetValue("case.familyNotified") > 0),
                new ClueEntryViewState("村民暂迁同意", "行动 · 山下村口",
                    "低处住户看过护符余量与山路时程，自行同意暂迁，并保留叫停试验的权利。",
                    session.GetValue("case.villageConsent") > 0),
                new ClueEntryViewState("三方叫停约定", "记录 · 山腹观测台",
                    "家属报炉侧呼吸，工匠报村口护符，沈砚按两侧读数调副脉；任何一方喊停，主角立即收息。",
                    session.GetValue("case.jointProtocol") > 0),
                new ClueEntryViewState("侧门符尾", "术证 · 炉房侧门",
                    "半张遮息符在门缝先向炉内卷，再被村口副脉推回。它能帮助复测回流先后，不能隔阵辨认人名。",
                    session.GetValue("case.sideDoorMeasured") > 0),
                new ClueEntryViewState("工匠查炉签记", "见证 · 炉房外廊",
                    "村落工匠亲见炉门封条与今日活息，或签封双灰对照样本。他能证明查验范围，不能据旧号认出承压者。",
                    session.GetValue("case.villageWitness") > 0),
                new ClueEntryViewState("封炉改签底页", "文书 · 旧碑廊",
                    "封门后有人用执事专印改写旧号签收栏。它可与旧阵两版图分开核对责任，不能单独认出今日炉中人。",
                    session.GetValue("case.archiveWitness") > 0),
                new ClueEntryViewState("旧灾守钟原簿", "文书 · 山下村落",
                    "原簿记亥初南沟三户先报符裂，子初东坡另有换符。它证明先后，尚须以留存护符排除雨损。",
                    session.GetValue("case.bellRecords") > 0),
                new ClueEntryViewState("旧灾护符对照", "术证 · 护符工坊",
                    "以同批未用符校准并交换位置复测：南沟符有山腹回流，东坡符只有雨损。可修正撤离顺序，不能推算今日灵潮强度。",
                    session.GetValue("case.bellVerified") > 0),
                new ClueEntryViewState("村民掌钟约定", "行动 · 山下村口",
                    "南沟三户看过原簿、复测符与炉中活人风险，愿先试走缓坡；是否暂迁与第三声叫停钟仍由他们决定。",
                    session.GetValue("case.bellShared") > 0)
            });
        }

        private static bool IsNewStory(string id)
        {
            return id.StartsWith("case.") || id.StartsWith("route.") || id.StartsWith("rift.")
                   || id.StartsWith("ending.");
        }
    }
}
