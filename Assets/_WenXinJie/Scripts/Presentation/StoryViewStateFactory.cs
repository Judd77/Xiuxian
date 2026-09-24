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
            return new(
                node.chapter,
                node.location,
                node.speaker,
                node.text,
                BuildStatus(session),
                node.terminal ? "未完待续" : "继续  ▷",
                node.terminal,
                _assets.GetBackground(node.background),
                _assets.GetPortrait(node.portrait),
                BuildJournal(session),
                node.choices.Select(choice => CreateChoice(session, choice)).ToArray());
        }

        private static ChoiceViewState CreateChoice(StorySession session, StoryChoice choice)
        {
            return new(
                choice.id,
                choice.text,
                choice.lockedReason,
                session.IsAvailable(choice));
        }

        private static string BuildStatus(StorySession session)
        {
            var clues = session.GetValue("clue.register") + session.GetValue("clue.jade");
            var attention = session.GetValue("risk.attention") > 0 ? "已惊动" : "未惊动";
            return "第 " + session.GetValue("day") + " 日    /    余暇 " + session.GetValue("actions")
                   + "    /    修为 " + session.GetValue("cultivation")
                   + "    /    林月信任 " + session.GetValue("trust.linYue")
                   + "    /    线索 " + clues + "/2    /    宗门 " + attention;
        }

        private static ClueJournalState BuildJournal(StorySession session)
        {
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
                    session.GetValue("mark.resonance") > 0)
            });
        }
    }
}