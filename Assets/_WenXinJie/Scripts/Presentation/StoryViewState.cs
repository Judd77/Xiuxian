using System;
using UnityEngine;

namespace WenXinJie.Presentation
{
    public sealed class StoryViewState
    {
        public string Chapter;
        public string Location;
        public string Speaker;
        public string Text;
        public string Status;
        public string NextLabel;
        public bool Terminal;
        public Texture2D Background;
        public Texture2D Portrait;
        public ChoiceViewState[] Choices = Array.Empty<ChoiceViewState>();
    }

    public sealed class ChoiceViewState
    {
        public string Id;
        public string Label;
        public string LockedReason;
        public bool Available;
    }
}
