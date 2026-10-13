using System;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    internal sealed class StoryViewState
    {
        public StoryViewState(string chapter, string location, string speaker, string text, string status,
            string nextLabel, bool terminal, Texture2D background, PortraitAssetEntry portrait,
            ClueJournalState journal, ChoiceViewState[] choices, StoryObservation observation,
            EndingViewState ending = null)
        {
            Chapter = chapter;
            Location = location;
            Speaker = speaker;
            Text = text;
            Status = status;
            NextLabel = nextLabel;
            Terminal = terminal;
            Background = background;
            Portrait = portrait;
            Journal = journal;
            Choices = choices ?? Array.Empty<ChoiceViewState>();
            Observation = observation;
            Ending = ending;
        }

        public string Chapter { get; }
        public string Location { get; }
        public string Speaker { get; }
        public string Text { get; }
        public string Status { get; }
        public string NextLabel { get; }
        public bool Terminal { get; }
        public Texture2D Background { get; }
        public PortraitAssetEntry Portrait { get; }
        public ClueJournalState Journal { get; }
        public ChoiceViewState[] Choices { get; }
        public StoryObservation Observation { get; }
        public EndingViewState Ending { get; }
    }

    internal sealed class EndingViewState
    {
        public EndingViewState(string title, string[] consequences)
        {
            Title = title;
            Consequences = consequences;
        }

        public string Title { get; }
        public string[] Consequences { get; }
    }

    internal sealed class ClueJournalState
    {
        public ClueJournalState(ClueEntryViewState[] entries = null)
        {
            Entries = entries ?? Array.Empty<ClueEntryViewState>();
        }

        public ClueEntryViewState[] Entries { get; }
    }

    internal sealed class ClueEntryViewState
    {
        public ClueEntryViewState(string title, string category, string description, bool unlocked)
        {
            Title = title;
            Category = category;
            Description = description;
            Unlocked = unlocked;
        }

        public string Title { get; }
        public string Category { get; }
        public string Description { get; }
        public bool Unlocked { get; }
    }

    internal sealed class ChoiceViewState
    {
        public ChoiceViewState(string id, string label, string lockedReason, bool available)
        {
            Id = id;
            Label = label;
            LockedReason = lockedReason;
            Available = available;
        }

        public string Id { get; }
        public string Label { get; }
        public string LockedReason { get; }
        public bool Available { get; }
    }
}
