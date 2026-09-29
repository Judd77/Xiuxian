using System;
using System.Collections.Generic;
using System.Linq;

namespace WenXinJie.Narrative
{
    public sealed class StorySession
    {
        private readonly StoryGraph _graph;
        private readonly RunRandom _random;
        private Dictionary<string, int> _values;

        public StorySession(StoryGraph graph, int? seed = null)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _random = new(seed ?? Guid.NewGuid().GetHashCode());
            _values = graph.Variables.ToDictionary(variable => variable.id, variable => variable.initial);
            Enter(graph.Definition.start, Array.Empty<StoryEffect>());
        }

        private StorySession(StoryGraph graph, Dictionary<string, int> values, StoryNode node, int randomState)
        {
            _graph = graph;
            _random = new(randomState);
            _values = values;
            Current = node;
        }

        public StoryNode Current { get; private set; }

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
            var choice = Current.choices.FirstOrDefault(item => item.id == choiceId);
            if (choice == null || !IsAvailable(choice))
            {
                return false;
            }

            var target = choice.target;
            IEnumerable<StoryEffect> effects = choice.effects;
            if (choice.outcomes.Length > 0)
            {
                var totalWeight = choice.outcomes.Sum(outcome => outcome.weight);
                var roll = _random.Next(totalWeight);
                foreach (var outcome in choice.outcomes)
                {
                    roll -= outcome.weight;
                    if (roll >= 0)
                    {
                        continue;
                    }

                    target = outcome.target;
                    effects = choice.effects.Concat(outcome.effects);
                    break;
                }
            }

            Enter(target, effects);
            return true;
        }

        public StorySnapshot Snapshot()
        {
            return new()
            {
                campaign = _graph.Definition.id,
                revision = _graph.Definition.revision,
                node = Current.id,
                randomState = _random.State,
                values = _values.OrderBy(pair => pair.Key).Select(pair => new VariableValue
                {
                    id = pair.Key,
                    value = pair.Value
                }).ToArray()
            };
        }

        public static StorySession Restore(StoryGraph graph, StorySnapshot snapshot)
        {
            StoryGraph.Require(snapshot != null && snapshot.version == 3, "Unsupported save schema.");
            StoryGraph.Require(snapshot.campaign == graph.Definition.id && snapshot.revision == graph.Definition.revision, "Save needs a content migration.");
            StoryGraph.Require(snapshot.values != null && snapshot.randomState != 0, "Missing saved state.");
            var values = new Dictionary<string, int>();
            foreach (var value in snapshot.values)
            {
                StoryGraph.Require(value != null, "Null saved variable.");
                var definition = graph.GetVariable(value.id);
                StoryGraph.Require(!values.ContainsKey(value.id), "Duplicate saved variable.");
                StoryGraph.Require(value.value >= definition.minimum && value.value <= definition.maximum, "Saved variable outside bounds.");
                values.Add(value.id, value.value);
            }

            StoryGraph.Require(values.Count == graph.Variables.Count(), "Incomplete save.");
            // Restoring the cursor must never replay entry effects.
            return new(graph, values, graph.GetNode(snapshot.node), snapshot.randomState);
        }

        private void Enter(string nodeId, IEnumerable<StoryEffect> choiceEffects)
        {
            var next = _graph.GetNode(nodeId);
            var values = new Dictionary<string, int>(_values);
            foreach (var effect in choiceEffects.Concat(next.effects))
            {
                var variable = _graph.GetVariable(effect.variable);
                var value = effect.operation == "set" ? effect.value : (long)values[effect.variable] + effect.value;
                values[effect.variable] = (int)Math.Max(variable.minimum, Math.Min(variable.maximum, value));
            }

            _values = values;
            Current = next;
        }
    }
}