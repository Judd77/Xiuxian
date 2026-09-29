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
                node.text,
                BuildStatus(session, journal),
                node.terminal ? (node.id.StartsWith("ending.") ? "本周目终" : "未完待续") : "继续",
                node.terminal,
                _assets.GetBackground(node.background),
                _assets.GetPortrait(node.portrait),
                journal,
                node.choices.Where(choice => !choice.hidden)
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
                    session.GetValue("case.shenAccount") > 0)
            });
        }

        private static bool IsNewStory(string id)
        {
            return id.StartsWith("case.") || id.StartsWith("route.") || id.StartsWith("rift.")
                   || id.StartsWith("ending.");
        }
    }
}
