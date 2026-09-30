"""Build the playable short-story edition from the approved manuscript structure."""

from __future__ import annotations

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Docs" / "INTEGRATED_STORY_DRAFT.md"
DATA = ROOT / "Assets" / "_WenXinJie" / "Data"
REVISION = 15


def sections():
    result = {}
    heading = None
    subheading = None
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        if line.startswith("## "):
            heading = line[3:]
            subheading = None
        elif line.startswith("### "):
            subheading = line[4:]
        elif line.strip() and heading:
            key = subheading or heading
            result.setdefault(key, []).append(line.strip())
    return result


def chunks(paragraph: str, limit: int = 160):
    sentences = re.findall(r"[^。！？]*[。！？]?[”’]?", paragraph)
    sentences = [sentence for sentence in sentences if sentence]
    current = ""
    for sentence in sentences:
        if current and len(current) + len(sentence) > limit:
            yield current
            current = ""
        if len(sentence) > limit:
            if current:
                yield current
                current = ""
            while len(sentence) > limit:
                yield sentence[:limit]
                sentence = sentence[limit:]
        current += sentence
    if current:
        yield current


def variable(name: str):
    return {"id": name, "initial": 0, "minimum": 0, "maximum": 1}


VARIABLES = [variable(name) for name in (
    "story.guessPerson", "story.skill", "story.ash", "story.ache",
    "story.xucheng", "story.village", "story.shen", "story.bell",
    "story.survey", "story.contract", "story.route", "story.public",
)]


def effect(name: str):
    return {"variable": name, "operation": "set", "value": 1}


def node(node_id: str, chapter: str, location: str, background: str, text: str):
    return {
        "id": node_id, "chapter": chapter, "location": location,
        "speaker": "旁白", "text": text, "background": background,
        "portrait": "", "rain": False, "effects": [], "choices": [],
    }


class Builder:
    def __init__(self):
        self.nodes = []
        self.lookup = {}

    def add(self, item):
        if item["id"] in self.lookup:
            raise ValueError(item["id"])
        self.nodes.append(item)
        self.lookup[item["id"]] = item
        return item["id"]

    def prose(self, prefix, paragraphs, chapter, location, background, start=0, stop=None):
        first = last = None
        for index in range(start, len(paragraphs) if stop is None else stop):
            for piece, content in enumerate(chunks(paragraphs[index])):
                node_id = f"story.{prefix}.{index:02d}.{piece}"
                self.add(node(node_id, chapter, location, background, content))
                if last:
                    self.lookup[last]["next"] = node_id
                first = first or node_id
                last = node_id
        return first, last

    def connect(self, last, next_id):
        self.lookup[last]["next"] = next_id

    def choose(self, last, choices):
        self.lookup[last]["choices"] = [
            {"id": item[0], "text": item[1], "target": item[2],
             "effects": [effect(name) for name in item[3:]]}
            for item in choices
        ]


def make_campaign():
    source = sections()
    b = Builder()
    chapters = [source[key] for key in (
        "一、阿澈没有下山", "二、门里有人", "三、村里的另一份账",
        "四、死人留下的阵图", "五、谁决定下一次试验", "六、问心之夜",
    )]
    c1, c2, c3, c4, c5, c6 = chapters

    one_start, one_end = b.prose("one", c1, "卷一 · 没有下山的人", "青云山 · 司籍房", "stele")
    b.lookup[one_start]["id"] = "case.intro"
    del b.lookup[one_start]
    b.lookup["case.intro"] = b.nodes[0]
    one_start = "case.intro"
    # Update the link to the renamed entry if the first paragraph was split.
    for item in b.nodes:
        if item.get("next") == "story.one.00.0":
            item["next"] = one_start
    b.lookup["story.one.08.0"]["effects"] = [effect("story.skill")]
    b.lookup["story.one.09.0"]["effects"] = [effect("story.ash")]
    b.choose(one_end, [
        ("suspect", "沈砚没死，先从他查起", "story.guess.suspect", "story.guessPerson"),
        ("number", "编号可能被人借用，先找阿澈", "story.guess.number"),
    ])
    b.add(node("story.guess.suspect", "卷一 · 没有下山的人", "青云山 · 司籍房", "stele",
               "你把沈砚的名字圈起来。陈闻没有替你下结论，只把阿澈没有盖印的那页推到圈旁：先找到人，再问旧名。"))
    b.add(node("story.guess.number", "卷一 · 没有下山的人", "青云山 · 司籍房", "stele",
               "你在旧编号旁写下“未证身份”。陈闻点头，把二更运车的岔口画给你。"))
    two_start, two_end = b.prose("two", c2, "卷一 · 门里有人", "青云山 · 山腹石门", "mountainHeart")
    b.connect("story.guess.suspect", two_start)
    b.connect("story.guess.number", two_start)
    b.lookup["story.two.05.0"]["effects"] = [effect("story.ache")]
    b.lookup["story.two.12.0"]["effects"] = [effect("story.xucheng")]
    b.connect(two_end, "case.boundary")
    b.add(node("case.boundary", "卷一 · 门里有人", "青云山 · 山腹石门", "mountainHeart",
               "侧门里有人敲了两下，村口同时传来碎符声。顾衡站在门前，等你们回答：开门之后，那股灵气会撞到谁家？"))
    b.choose("case.boundary", [
        ("continue", "去村里核对护符和旧灾记录", "story.three.00.0"),
        ("rescue", "先与林月、家属筹备救援", "story.leave.00.0", "story.route"),
    ])
    three_start, three_end = b.prose("three", c3, "卷二 · 村里的另一份账", "南沟村", "ridge")
    assert three_start == "story.three.00.0"
    b.lookup[three_start]["effects"] = [effect("story.village")]
    four_start, four_end = b.prose("four", c4, "卷三 · 死人的阵图", "废观测台", "stele", stop=13)
    b.connect(three_end, four_start)
    b.lookup["story.four.05.0"]["effects"] = [effect("story.bell")]
    b.lookup["story.four.06.0"]["effects"] = [effect("story.shen")]
    b.lookup["story.four.10.0"]["effects"] = [effect("story.survey")]
    b.choose(four_end, [
        ("joint", "让家属与村民共同守钟，继续小试", "story.four.13.0", "story.contract"),
        ("seize", "趁验阵夺下石台，强行放人", "story.seize.00.0", "story.route"),
        ("seal", "封回旧闸，今夜继续守住旧阵", "story.old.00.0", "story.route"),
    ])
    joint_start, joint_end = b.prose("four", c4, "卷三 · 死人的阵图", "山腹石门", "mountainHeart", start=13)
    assert joint_start == "story.four.13.0"
    five_start, five_end = b.prose("five", c5, "卷四 · 谁有权说停", "青云山 · 议事堂", "stele")
    b.connect(joint_end, five_start)
    six_start, six_end = b.prose("six", c6, "终卷 · 问心之夜", "山腹石门", "mountainHeart")
    b.connect(five_end, six_start)
    b.lookup[six_end]["terminal"] = True

    leave = source["带人离山"]
    leave_start, leave_end = b.prose("leave", leave, "结局 · 带人离山", "青云山道", "ridge")
    assert leave_start == "story.leave.00.0"
    b.lookup[leave_start]["effects"] = [effect("story.xucheng")]
    b.connect(leave_end, "ending.leave")
    b.add(node("ending.leave", "结局 · 带人离山", "山下村路", "ridge",
               "你们带走了证词，也留下尚未救出的人。下一次回山，名单上的每个空格都要有人追问。"))
    b.lookup["ending.leave"]["terminal"] = True

    seize = source["夺下石台"]
    seize_start, seize_end = b.prose("seize", seize, "结局 · 夺下石台", "山腹石台", "mountainHeart")
    assert seize_start == "story.seize.00.0"
    seize_decision = next(item["id"] for item in reversed(b.nodes) if item["id"].startswith("story.seize.00."))
    b.lookup[seize_decision].pop("next")
    b.choose(seize_decision, [
        ("self", "自己坐上石台，事后交出钥匙", "story.seize.01.0"),
        ("force", "让祁元肃承受今夜的阵压", "story.seize.02.0"),
    ])
    seize_self_end = next(item["id"] for item in reversed(b.nodes) if item["id"].startswith("story.seize.01."))
    b.lookup[seize_self_end]["next"] = "ending.seizeSelf"
    b.add(node("ending.seizeSelf", "结局 · 夺下石台", "山腹石台", "mountainHeart",
               "石台钥匙交到了家属与村民手中。你们仍须迁村、重测石柱，才能结束用人挡灾的日子。"))
    b.lookup["ending.seizeSelf"]["terminal"] = True
    b.connect(seize_end, "ending.seizeForce")
    b.add(node("ending.seizeForce", "结局 · 夺下石台", "山腹石台", "mountainHeart",
               "炉门开了，村口也保住了；你替众人决定谁该受伤，得从头争回他们的信任。"))
    b.lookup["ending.seizeForce"]["terminal"] = True

    old = source["守住旧阵"]
    old_start, old_end = b.prose("old", old, "结局 · 守住旧阵", "青云山 · 司籍房", "stele")
    assert old_start == "story.old.00.0"
    old_decision = next(item["id"] for item in reversed(b.nodes) if item["id"].startswith("story.old.00."))
    b.lookup[old_decision].pop("next")
    b.choose(old_decision, [
        ("tell", "把真名册、旧图和伤情交给村民", "story.old.01.0", "story.public"),
        ("hide", "把纸锁回司籍房，继续替宗门誊册", "story.old.02.0"),
    ])
    old_public_end = next(item["id"] for item in reversed(b.nodes) if item["id"].startswith("story.old.01."))
    b.lookup[old_public_end]["next"] = "ending.oldPublic"
    b.add(node("ending.oldPublic", "结局 · 守住旧阵", "南沟村", "ridge",
               "今晚没有改路。村民与家属拿到了真相，开始筹备下一次迁离。"))
    b.lookup["ending.oldPublic"]["terminal"] = True
    b.connect(old_end, "ending.oldHidden")
    b.add(node("ending.oldHidden", "结局 · 守住旧阵", "青云山 · 司籍房", "stele",
               "村庄平安过夜。你亲手写下了下一名弟子的假去向，阿澈始终没等到回答。"))
    b.lookup["ending.oldHidden"]["terminal"] = True

    # Reuse established paintings at actual scene transitions. New locations still need art.
    scenes = {
        "one": [(0, 2, "弟子居所", "quartersNight"),
                (3, 6, "司籍房", "stele"),
                (7, 7, "山腹坡道", "mountainHeart"),
                (8, 8, "弟子居所", "quartersNight"),
                (9, 9, "山腹坡道", "mountainHeart"),
                (10, 13, "考校场与司籍房", "stele")],
        "two": [(0, 2, "医舍", "quartersNight"),
                (3, 8, "山腹炉房", "mountainHeart"),
                (9, 9, "山腹坡道", "ridge"),
                (10, 14, "医舍与村路", "quartersNight"),
                (15, 16, "山腹侧门", "mountainHeart")],
        "three": [(0, 7, "南沟村", "ridge")],
        "four": [(0, 0, "司籍房", "stele"),
                 (1, 9, "废观测台", "stele"),
                 (10, 12, "谷口", "ridge"),
                 (13, 14, "山腹石台", "mountainHeart")],
        "five": [(0, 0, "山腹坡道", "mountainHeart"),
                 (1, 6, "宗门议事堂", "stele"),
                 (7, 7, "谷口村路", "ridge"),
                 (8, 10, "山腹石台", "mountainHeart")],
        "six": [(0, 0, "谷口村路", "ridge"),
                (1, 4, "山腹石台", "mountainHeart"),
                (5, 5, "青云山", "gate"),
                (6, 6, "司籍房", "stele")],
    }
    for item in b.nodes:
        match = re.match(r"story\.(one|two|three|four|five|six)\.(\d+)\.", item["id"])
        if match:
            group, paragraph = match.group(1), int(match.group(2))
            for first, last, place, art in scenes[group]:
                if first <= paragraph <= last:
                    item["location"] = place
                    item["background"] = art
                    break
            if (group == "two" and paragraph in (0, 1, 2, 7, 8, 9, 11, 14, 15)) or \
                    (group == "five" and paragraph in (1, 2, 6, 7)):
                item["portrait"] = "linYue"
    b.lookup["case.intro"]["location"] = "弟子居所"
    b.lookup["case.intro"]["background"] = "quartersNight"
    return b.nodes


def validate(nodes):
    by_id = {item["id"]: item for item in nodes}
    assert len(by_id) == len(nodes)
    for item in nodes:
        exits = int("next" in item) + int(bool(item["choices"])) + int(item.get("terminal", False))
        assert exits == 1, (item["id"], exits)
        if "next" in item:
            assert item["next"] in by_id, item["id"]
        for choice in item["choices"]:
            assert choice["target"] in by_id, (item["id"], choice["target"])
    seen = set()
    pending = ["case.intro"]
    while pending:
        current = pending.pop()
        if current in seen:
            continue
        seen.add(current)
        item = by_id[current]
        if "next" in item:
            pending.append(item["next"])
        pending.extend(choice["target"] for choice in item["choices"])
    assert seen == set(by_id), sorted(set(by_id) - seen)


def write():
    nodes = make_campaign()
    validate(nodes)
    campaign = {"id": "wenxinjie", "revision": REVISION, "start": "case.intro",
                "variables": VARIABLES, "nodes": nodes}
    DATA.joinpath("Campaign.json").write_text(
        json.dumps(campaign, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    boundary = next(index for index, item in enumerate(nodes) if item["id"] == "case.boundary")
    demo_nodes = json.loads(json.dumps(nodes[:boundary + 1], ensure_ascii=False))
    demo_nodes[-1]["choices"] = []
    demo_nodes[-1]["terminal"] = True
    demo_nodes[-1]["text"] += "\n\n【试玩结束】完整版可继承本次调查进度。"
    validate(demo_nodes)
    campaign["nodes"] = demo_nodes
    DATA.joinpath("DemoCampaign.json").write_text(
        json.dumps(campaign, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Full: {len(nodes)} nodes; Demo: {len(demo_nodes)} nodes")


if __name__ == "__main__":
    write()
