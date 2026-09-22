using System;
using UnityEngine;

namespace WenXinJie.Core
{
    [Serializable]
    public sealed class FirstDayEnding
    {
        public string id;
        public string text;
    }

    [Serializable]
    public sealed class FirstDayStory
    {
        public string[] lines;
        public FirstDayEnding[] endings;

        public static FirstDayStory Parse(string json)
        {
            FirstDayStory story = JsonUtility.FromJson<FirstDayStory>(json);
            if (story == null || story.lines == null || story.lines.Length == 0)
            {
                throw new ArgumentException("Story requires opening lines.");
            }

            foreach (string line in story.lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    throw new ArgumentException("Story contains an empty opening line.");
                }
            }

            if (story.endings == null || story.endings.Length != 4)
            {
                throw new ArgumentException("Story requires all four first-day outcomes.");
            }

            foreach (NightOutcome outcome in Enum.GetValues(typeof(NightOutcome)))
            {
                if (outcome != NightOutcome.Unresolved)
                {
                    story.GetEnding(outcome);
                }
            }

            return story;
        }

        public string GetEnding(NightOutcome outcome)
        {
            foreach (FirstDayEnding ending in endings)
            {
                if (ending != null && ending.id == outcome.ToString() && !string.IsNullOrWhiteSpace(ending.text))
                {
                    return ending.text;
                }
            }

            throw new ArgumentException("Missing ending: " + outcome);
        }
    }
}
