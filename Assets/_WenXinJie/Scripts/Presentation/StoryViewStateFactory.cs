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
                    .Select(choice => CreateChoice(session, choice)).ToArray(), node.observation,
                BuildEnding(session));
        }

        private static EndingViewState BuildEnding(StorySession session)
        {
            if (!session.Current.terminal) return null;
            if (session.Current.id == "ending.newRoad")
            {
                var record = session.GetValue("case.post") > 0
                    ? "旧帖与更名页留证。" : "旧号领单与更名页留证。";
                var thirdSegment = session.GetValue("e.thirdWatch") == 1
                    ? "第三段封口待修；" : session.GetValue("e.thirdWatch") == 2
                        ? "第三段轮班守符待修；" : string.Empty;
                return new("改道", new[]
                {
                    "许澄与段七离开炉榻，转入医舍；你耗尽灵力，修为退损，需要休养。",
                    BuildVillageOutcome(session),
                    session.GetValue("e.thirdWatch") > 0
                        ? thirdSegment + record + "出炉的人各有去向抄页。"
                        : session.GetValue("case.post") > 0
                            ? "旧帖、刮痕拓片与更名页留作对照；出炉的人各自拿到了去向抄页。"
                            : "原帖没有保住。旧号领单与更名页留下对照，出炉的人各自拿到了去向抄页。"
                });
            }
            if (session.Current.id != "ending.oldPublic") return null;

            if (session.GetValue("e.publicCopies") > 0)
            {
                return new("公开暂守", new[]
                {
                    "今夜暂守旧阵。许澄仍在炉内，轮换歇息；没有将尚未救出的人写成离山。",
                    "家属、村人和医舍各留真账，去向、伤情与损耗分开记录。",
                    "三日后先补料、复查柱位，再由当事人决定是否试阵。开门仍未完成。"
                });
            }

            return new("公开暂守", new[]
            {
                "今夜暂守旧阵，许澄仍未出炉。顾衡公开更正的旧呈报，家属留下待核去向页。",
                session.GetValue("g.holdWarned") > 0
                    ? "先撤三户，孙子没有添伤；药车让路、重画村符多耗的符纸记在村账上。"
                    : "先补村符，三户迟撤；落瓦划伤了孙子的手臂，医舍接应者为他包扎。",
                session.GetValue("g.holdPatrol") == 1
                    ? "守村墙挡住复热；炉门多耗一张应急符。三日后补料复测，再问本人。"
                    : session.GetValue("g.holdPatrol") == 2
                        ? "回炉门帮她歇下；村口空仓檐木烤裂。三日后补料复测，再问本人。"
                        : "三日后先看伤、补料和复测，再问当事人。暂守留下的风险仍须处理。"
            });
        }

        private static string BuildVillageOutcome(StorySession session)
        {
            var housing = session.GetValue("h.lodging") == 1
                ? "三户住修过的旧屋；补瓦耗符待清。"
                : session.GetValue("h.lodging") == 2
                    ? "三户共宿，南屋待修。"
                    : "三户暂住高处，田屋受损。";
            var notice = session.GetValue("h.notice") == 1
                ? "预药单转公账。"
                : session.GetValue("h.notice") == 2
                    ? "晚备耗符入账。"
                    : string.Empty;
            return housing + notice + "冬药修阵另欠。";
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
                return "问心劫    /    已收录线索 " + clues + "/" + journal.Entries.Length
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

        private static string AcheTestimonyClue(StorySession session)
        {
            var id = session.Current.id;
            var eveningKnown = session.Revision >= 69
                && (id == "c12.evening" || id == "c12.fear" || id == "c12.company"
                    || id == "c12.traceStart" || id == "c12.traceFeel" || id == "c12.traceRest"
                    || id == "c12.shoeStart" || id == "c12.shoeWork" || id == "c12.shoeRest"
                    || id == "c12.sleep");
            var wallKnown = !id.StartsWith("c12.") || id == "c12.name"
                || id == "c12.challenge" || id == "c12.unknown" || eveningKnown;
            if (session.Revision < 48)
            {
                return wallKnown
                    ? "床底鞋是他留下的求救痕迹；隔墙有人两下敲门。安神药影响了他对门漆和姓名的记忆。隔墙身份仍须另证。"
                    : "阿澈说前夜已经坐过炉榻，发烧后暂送回住处，天未亮又被带走。床底的鞋是他自己留下的求救痕迹。隔墙病人的事还没问到。";
            }
            var record = session.GetValue("case.acheTestimony") switch
            {
                1 => "阿澈本人已核过医舍证词：领药不等于同意继续留榻；背灯人的姓名他没看清。",
                2 => "医舍暂记阿澈领药后仍被留榻，页首注明本人未核；不能当作他签过的证词。",
                _ => "阿澈说前夜已坐过炉榻，发烧后回住处，天未亮又被带走；床底鞋是他留下的求救痕迹。"
            };
            if (session.Revision >= 66 && session.GetValue("case.rescueMethod") >= 2)
            {
                record += "救援时你的灵力也进入过旧带；近期余息不能单独归给阿澈，须另看救援见闻与本人证词。";
            }
            return wallKnown
                ? record + "隔墙有人两下敲门；安神药影响了他对门漆和姓名的记忆，身份仍须另证。"
                : record + "隔墙病人的事还没问到。";
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
                    "同批未运灰几乎无近期余息，车灰有新近余息。术法没有给出确切日期或人名；出入验牌的守卫也可能留下余息，仍须查交接与目击。",
                    session.GetValue("case.ashCorrect") > 0),
                new ClueEntryViewState("考校副册", "文书 · 考校台",
                    session.GetValue("case.deputy") >= 2
                        ? "正册写阿澈离山，晨间副册仍等他应试；考校台在点名后才收到销名签。值日抄手核签时辰，实际去向仍须验山门印和本人。"
                        : "正册已写阿澈离山，副册仍等他应试。归档时辰已抄下，可与山门记录核对。",
                    session.GetValue("case.deputy") > 0),
                new ClueEntryViewState("炉房外榻的流向", "亲见与术证 · 山腹",
                    "石柱先承力，隔壁榻随后加重。解开一人会转移冲击；阿澈获救当夜，村口护符裂了一张。",
                    session.GetValue("story.ache") > 0),
                new ClueEntryViewState("两张榻号", "现场抄录 · 炉房",
                    "救阿澈前抄下他与隔壁病人的榻号。床位还需与伤签核名，不能直接当作姓名。",
                    session.GetValue("case.bedNumber") > 0),
                new ClueEntryViewState("阿澈的证词", "当事人说法 · 医舍",
                    AcheTestimonyClue(session),
                    session.GetValue("story.ache") > 0 && session.Current.id != "c11.cost"),
                new ClueEntryViewState("许澄的半枚玉扣", "实物互证 · 许家",
                    "阿澈带出的半扣与许家姐姐保管的另一半吻合。扣可能被别人携带，仍需让门里的人自行回应身份。",
                    session.GetValue("story.xucheng") > 0),
                new ClueEntryViewState("南沟旧灾", "文书 · 山下",
                    session.GetValue("g.bellBook") >= 2
                        ? "南沟先报符裂，东坡近半个时辰后换符；旧符坏法也不同。原簿记钟楼三响，却没有炉门回签。今日试传村口有回签、炉门没有；辨气不能据今日反推十年前收钟与否。"
                        : "南沟先报符裂，东坡近半个时辰后换符；旧符损坏方式也不同。十年前的余息已散，辨气不能补报当夜详情。",
                    session.GetValue("g.oldCharm") > 0 && session.GetValue("g.bellBook") > 0),
                new ClueEntryViewState("顾衡的改记", "本人更正 · 村议",
                    "顾衡当众承认把两处灾情合写同刻，也把南沟死者写轻；更正写在旧呈报旁，原字未擦。",
                    session.GetValue("g.corrected") > 0),
                new ClueEntryViewState("撤离的实际脚程", "现场实走 · 谷口",
                    "背老妇过第一坡已用完旧簿所写的半炷香；按慢者先行、药箱与孩子分开过狭路重排。",
                    session.GetValue("g.walked") > 0),
                new ClueEntryViewState("移位的第三柱", "局部术证 · 谷口",
                    "旧石础与现柱脚错开，今日余息经过现柱脚；小试村口先亮、炉门后热。只量出局部先后，不能预言下一次灵潮强度。",
                    session.GetValue("g.thirdPillar") > 0),
                new ClueEntryViewState("村民握停钟", "行动约定 · 村议",
                    "工匠由村民选出守钟，家属守炉门铃，陶叔关支路；任何一端喊停，两端撤力。不是整村同意永远开谷口。",
                    session.GetValue("g.hammer") > 0),
                new ClueEntryViewState("分开的村民答复", "本人决定 · 村议",
                    "愿试走、愿借屋、仍反对开谷口的户分栏记录。木匠要求公开冬药账，不能写成全村一致同意改道。",
                    session.GetValue("g.villageRecord") > 0),
                new ClueEntryViewState("谷口复测", "术证 · 山谷",
                    ValleySurveyRecord(session),
                    session.GetValue("story.survey") > 0),
                new ClueEntryViewState("共同叫停约定", "行动 · 议事堂",
                    "炉门与村口都须有人守钟；家属和村民能叫停，伤亡与迁离按真名记录。",
                    session.GetValue("story.contract") > 0),
                new ClueEntryViewState("三处加快的记录", "时辰互证 · 山腹与村口",
                    "炉符耗量、石柱闷响与无雨裂符按时辰先后加快。至迟明夜须准备，无法预言灵潮准确一刻。",
                    session.GetValue("h.deadline") > 0),
                new ClueEntryViewState("分开的三份责任", "公开对质 · 议事堂",
                    "出处：周望初试本、祁元肃签令与离山册、顾衡旧呈报。\n已核：沈砚闻铃未停；祁元肃加压并改去向；顾衡合写灾情、写轻死者。\n三人的责任分别记，不由一人代认。",
                    session.GetValue("h.shen") > 0 && session.GetValue("h.qi") > 0
                    && session.GetValue("h.gu") > 0),
                new ClueEntryViewState("可叫停的小试", "共同实测 · 谷口与炉门",
                    JointTrialRecord(session),
                    session.GetValue("h.test") > 0),
                new ClueEntryViewState("三户分别同意暂迁", "本人决定 · 谷口",
                    "老妇要求由孙子背行、先过狭岔；父女带药，平车留好卧伤者的位置；木匠借高处旧屋，宗门签认冬药与木料欠账。三户已答应暂迁，实际撤离仍待放行；无人答应永久舍弃家与药田。",
                    session.GetValue("h.house1") > 0 && session.GetValue("h.house2") > 0
                    && session.GetValue("h.house3") > 0),
                new ClueEntryViewState("开闸与停铃", "行动记录 · 山腹石台",
                    RescueRecord(session),
                    session.GetValue("e.bellPlaced") > 0),
                new ClueEntryViewState("公开的守阵真账", "三方留存 · 司籍房",
                    PublicHoldRecord(session),
                    session.GetValue("e.publicCopies") > 0)
            });
        }

        private static string ValleySurveyRecord(StorySession session)
        {
            var cost = session.GetValue("g.extraCharm") == 0
                ? "本次村符未裂。"
                : session.GetValue("g.repairOwed") > 0
                    ? "本次裂了一张村符，临时压住裂口，仍欠正式修补。"
                    : "本次裂过的村符已补牢，所用符料仍记在村账。";
            return "出处：现柱脚、旧石础与两处感应符。\n所测：第三柱已移位，村口先亮，炉门后热。" + cost
                + "\n未明：下一股灵潮多强；炉内伤情与正式开闸条件仍须另核。";
        }

        private static string JointTrialRecord(StorySession session)
        {
            // E03 can also set extraCharm; only name the wrist injury while still in H05/H06.
            var beforeOpening = session.Current.id.StartsWith("h05.") || session.Current.id.StartsWith("h06.");
            var cost = session.GetValue("e.extraCharm") == 0
                ? "按钟收息，村符焦而未断，许澄能活动手腕。"
                : beforeOpening
                    ? "停钟后迟撤，村符裂了一张，许澄腕上添伤；重新试前须看伤、再问本人。"
                    : "已用过额外符料，伤情须按炉门记录的时辰核对；后来的损耗不能混记成早先小试造成。";
            return "出处：村口、炉门与石台共同记录。\n所测：村口先亮，炉门后热；试验支路已关，谷底仍未通。"
                + "\n损耗：" + cost + "\n未明：下一股灵潮是否同样轻。";
        }

        private static string RescueRecord(StorySession session)
        {
            var node = session.Current.id;
            if (node == "e04.stop" || node == "e04.breathAfter")
            {
                return "已执行：第三声后收息，陶叔关炉榻小闸并回钟；引流闸仍开，余流沿前两段泄入深谷。\n当前：林月已确认腕符卸压，两人还未出炉，待解带、扶上平车。";
            }

            if (node == "e04.outside" || node == "e04.valley" || node == "e04.familyMessage" || node.StartsWith("e05.")
                || node == "ending.newRoad")
            {
                var exhaustion = node.StartsWith("e05.") || node == "ending.newRoad"
                    ? "主角修为退落"
                    : "主角灵力耗尽";
                return "已执行：第三声收息、隔开炉榻，林月解下腕符带，将许澄与段七接出炉房。\n代价：" + exhaustion
                    + "，获救者仍需养伤，三户暂迁，药田受损。\n未完成：谷底新路仍须修缮，下一轮灵潮还要有人维护。";
            }

            if (node == "e03.old" || node == "e03.reconnect")
            {
                var care = node == "e03.old"
                    ? "村符裂了一张，炉中添轻伤；补护、看伤和重新问本人仍待完成。"
                    : "裂符已补、伤情已查看，两人答应继续；引流闸仍关，等待玩家确认后重开。";
                return "已发生：左槽接错，主角收息，陶叔关回引流闸；炉榻仍接在旧阵上，两人未获救。\n当前：" + care;
            }

            if (session.GetValue("e.flowMeasured") > 0)
            {
                return "已发生：新测支路正在引流，药田已受冲，两人能使用停铃。\n约定：第三声必停，开口求停也须传到石台。\n尚未执行：第三声后的收息与出炉，两人仍在炉内。";
            }

            if (node.StartsWith("e03."))
            {
                return "已发生：谷底引流闸已开，药田开始受冲；炉榻尚未隔开。两人能用停铃，石台能回钟。\n待做：循现柱脚辨流，将来气牵向深谷；人还没有离榻。\n约定：第三声必停，任何人求停也立即传令。";
            }

            return "已准备：许澄与段七亲手试铃，能听见陶叔的回钟，并答应本次行动。\n约定：开闸后重新计铃，第三声必停，任何人求停也要立即传令。\n尚未发生：正式开闸与救人。";
        }

        private static string PublicHoldRecord(StorySession session)
        {
            var node = session.Current.id;
            var afterPause = node == "h05.holdPause" || node == "h05.holdSchedule"
                || node == "h05.holdVillage" || node == "ending.oldPublic";
            var progress = afterPause
                ? "许澄已获一轮歇息、下榻换药，仍未离开炉房。"
                : "记录已交付，许澄仍在炉房。本榻轮换与换药尚待执行。";
            if (node == "h05.holdSchedule" || node == "h05.holdVillage" || node == "ending.oldPublic")
            {
                progress += "三日后先看伤、补料、复测并重新问本人，不能保证当天救出。";
            }
            else
            {
                progress += "下次何时试阵，还须看伤情、符料和复测后再定。";
            }

            return "已交付：家属持去向，村民持撤路与损耗时辰，医舍持伤情；未核姓名仍留空。\n当前：" + progress;
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
