using System;

namespace WenXinJie.Narrative
{
    [Serializable]
    public sealed class StorySnapshot
    {
        public int version = 2;
        public string campaign;
        public int revision;
        public string node;
        public VariableValue[] values;
    }

    [Serializable]
    public sealed class VariableValue
    {
        public string id;
        public int value;
    }
}