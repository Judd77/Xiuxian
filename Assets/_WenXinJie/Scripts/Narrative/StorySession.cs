using System;
using System.Collections.Generic;
using System.Linq;

namespace WenXinJie.Narrative
{
    public sealed class StorySession
    {
        private readonly StoryGraph _graph;
        private Dictionary<string, int> _values;

        public StoryNode Current { get; private set; }

        public StorySession(StoryGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _values = graph.Variables.ToDictionary(variable => variable.id, variable => variable.initial);
            Enter(graph.Definition.start, Array.Empty<StoryEffect>());
        }

        private StorySession(StoryGraph graph, Dictionary<string, int> values, StoryNode node)
        {
            _graph = graph;
            _values = values;
            Current = node;
        }

        public int GetValue(string id)
        {
            return _values[id];
        }

        public bool IsAvailable(StoryChoice choice)
        {
            return Current.choices.Contains(choice) && choice.conditions.All(condition =>
                _values[condition.variable] >= condition.minimum && _values[condition.variable] <= condition.maximum);
        }

        public bool Advance()
        {
            if (string.IsNullOrEmpty(Current.next))
            {
                return false;
            }

            Enter(Current.next, Array.Empty<StoryEffect>());
            return true;
        }

        public bool Choose(string choiceId)
        {
            StoryChoice choice = Current.choices.FirstOrDefault(item => item.id == choiceId);
            if (choice == null || !IsAvailable(choice))
            {
                return false;
            }

            Enter(choice.target, choice.effects);
            return true;
        }

        public StorySnapshot Snapshot()
        {
            return new StorySnapshot
            {
                campaign = _graph.Definition.id,
                revision = _graph.Definition.revision,
                node = Current.id,
                values = _values.OrderBy(pair => pair.Key).Select(pair => new VariableValue
                {
                    id = pair.Key,
                    value = pair.Value
                }).ToArray()
            };
        }

        public static StorySession Restore(StoryGraph graph, StorySnapshot snapshot)
        {
            StoryGraph.Require(snapshot != null && snapshot.version == 2, "Unsupported save schema.");
            StoryGraph.Require(snapshot.campaign == graph.Definition.id && snapshot.revision == graph.Definition.revision, "Save needs a content migration.");
            StoryGraph.Require(snapshot.values != null, "Missing saved state.");
            var values = new Dictionary<string, int>();
            foreach (VariableValue value in snapshot.values)
            {
                StoryGraph.Require(value != null, "Null saved variable.");
                VariableDefinition definition = graph.GetVariable(value.id);
                StoryGraph.Require(!values.ContainsKey(value.id), "Duplicate saved variable.");
                StoryGraph.Require(value.value >= definition.minimum && value.value <= definition.maximum, "Saved variable outside bounds.");
                values.Add(value.id, value.value);
            }

            StoryGraph.Require(values.Count == graph.Variables.Count(), "Incomplete save.");
            // Restoring the cursor must never replay entry effects.
            return new StorySession(graph, values, graph.GetNode(snapshot.node));
        }

        private void Enter(string nodeId, IEnumerable<StoryEffect> choiceEffects)
        {
            StoryNode next = _graph.GetNode(nodeId);
            var values = new Dictionary<string, int>(_values);
            foreach (StoryEffect effect in choiceEffects.Concat(next.effects))
            {
                VariableDefinition variable = _graph.GetVariable(effect.variable);
                long value = effect.operation == "set" ? effect.value : (long)values[effect.variable] + effect.value;
                values[effect.variable] = (int)Math.Max(variable.minimum, Math.Min(variable.maximum, value));
            }

            _values = values;
            Current = next;
        }
    }
}
