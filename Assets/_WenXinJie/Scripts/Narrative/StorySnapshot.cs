using System;

namespace WenXinJie.Narrative
{
    [Serializable]
    public sealed class StorySnapshot
    {
        public int version = 3;
        public string campaign;
        public int revision;
        public string node;
        public int randomState;
        public VariableValue[] values;
    }

    [Serializable]
    public sealed class VariableValue
    {
        public string id;
        public int value;
    }
}