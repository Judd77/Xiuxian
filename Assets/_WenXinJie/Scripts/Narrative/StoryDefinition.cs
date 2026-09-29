using System;

namespace WenXinJie.Narrative
{
    // JSON transport types. Runtime rules live in StoryGraph and StorySession.
    [Serializable]
    public sealed class StoryDefinition
    {
        public string id;
        public int revision;
        public string start;
        public VariableDefinition[] variables;
        public StoryNode[] nodes;
    }

    [Serializable]
    public sealed class VariableDefinition
    {
        public string id;
        public int initial;
        public int minimum;
        public int maximum;
    }

    [Serializable]
    public sealed class StoryNode
    {
        public string id;
        public string chapter;
        public string location;
        public string speaker;
        public string text;
        public string background;
        public string portrait;
        public bool rain;
        public string next;
        public bool terminal;
        public StoryEffect[] effects = Array.Empty<StoryEffect>();
        public StoryChoice[] choices = Array.Empty<StoryChoice>();
    }

    [Serializable]
    public sealed class StoryChoice
    {
        public string id;
        public string text;
        public string target;
        public string lockedReason;
        public bool hidden;
        public StoryCondition[] conditions = Array.Empty<StoryCondition>();
        public StoryEffect[] effects = Array.Empty<StoryEffect>();
        public StoryOutcome[] outcomes = Array.Empty<StoryOutcome>();
    }

    [Serializable]
    public sealed class StoryOutcome
    {
        public string target;
        public int weight;
        public StoryEffect[] effects = Array.Empty<StoryEffect>();
    }

    [Serializable]
    public sealed class StoryCondition
    {
        public string variable;
        public int minimum;
        public int maximum;
    }

    [Serializable]
    public sealed class StoryEffect
    {
        public string variable;
        public string operation;
        public int value;
    }
}
