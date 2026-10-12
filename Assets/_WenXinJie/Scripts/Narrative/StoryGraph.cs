using System;
using System.Collections.Generic;

namespace WenXinJie.Narrative
{
    public sealed class StoryGraph
    {
        private readonly Dictionary<string, StoryNode> _nodes = new Dictionary<string, StoryNode>();
        private readonly Dictionary<string, VariableDefinition> _variables = new Dictionary<string, VariableDefinition>();

        public StoryDefinition Definition { get; }
        public IEnumerable<VariableDefinition> Variables => _variables.Values;
        public IEnumerable<StoryNode> Nodes => _nodes.Values;

        public StoryGraph(StoryDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Require(!string.IsNullOrWhiteSpace(definition.id) && definition.revision > 0, "Invalid campaign identity.");
            Require(definition.variables != null && definition.nodes != null, "Missing campaign arrays.");
            var saveRevisions = new HashSet<int>();
            foreach (var revision in definition.compatibleSaveRevisions ?? Array.Empty<int>())
            {
                Require(revision > 0 && revision < definition.revision && saveRevisions.Add(revision),
                    "Invalid compatible save revision.");
            }

            foreach (VariableDefinition variable in definition.variables)
            {
                Require(variable != null && !string.IsNullOrWhiteSpace(variable.id), "Invalid variable.");
                Require(!_variables.ContainsKey(variable.id), "Duplicate variable: " + variable.id);
                Require(variable.minimum <= variable.initial && variable.initial <= variable.maximum, "Invalid variable bounds.");
                _variables.Add(variable.id, variable);
            }

            foreach (StoryNode node in definition.nodes)
            {
                Require(node != null && !string.IsNullOrWhiteSpace(node.id), "Invalid node.");
                Require(!_nodes.ContainsKey(node.id), "Duplicate node: " + node.id);
                _nodes.Add(node.id, node);
            }

            GetNode(definition.start);
            foreach (StoryNode node in _nodes.Values)
            {
                ValidateNode(node);
            }

            ValidateReachability();
        }

        public StoryNode GetNode(string id)
        {
            Require(id != null && _nodes.ContainsKey(id), "Unknown story node: " + id);
            return _nodes[id];
        }

        public VariableDefinition GetVariable(string id)
        {
            Require(id != null && _variables.ContainsKey(id), "Unknown variable: " + id);
            return _variables[id];
        }

        private void ValidateNode(StoryNode node)
        {
            Require(!string.IsNullOrWhiteSpace(node.text) && !string.IsNullOrWhiteSpace(node.background), "Missing presentation: " + node.id);
            Require(node.effects != null && node.choices != null, "Missing node arrays: " + node.id);
            // JsonUtility may instantiate an empty nested object for an omitted JSON field.
            if (node.observation != null && string.IsNullOrEmpty(node.observation.kind)
                                         && string.IsNullOrEmpty(node.observation.title))
            {
                node.observation = null;
            }
            if (node.observation != null)
            {
                var observation = node.observation;
                Require(observation.kind == "residue" || observation.kind == "flow", "Unknown observation: " + node.id);
                Require(!string.IsNullOrWhiteSpace(observation.title)
                        && !string.IsNullOrWhiteSpace(observation.leftLabel)
                        && !string.IsNullOrWhiteSpace(observation.rightLabel), "Missing observation labels: " + node.id);
                Require(observation.leftStrength >= 0f && observation.leftStrength <= 1f
                        && observation.rightStrength >= 0f && observation.rightStrength <= 1f,
                    "Invalid residue strength: " + node.id);
                Require(observation.kind != "flow" || observation.flow == "pending"
                        || observation.flow == "villageFirst" || observation.flow == "villageWarning"
                        || observation.flow == "closed"
                        || observation.flow == "diverted", "Unknown flow stage: " + node.id);
            }
            foreach (var variant in node.variants ?? Array.Empty<StoryTextVariant>())
            {
                Require(variant != null && !string.IsNullOrWhiteSpace(variant.text)
                                        && variant.conditions != null && variant.conditions.Length > 0,
                    "Invalid text variant: " + node.id);
                foreach (var condition in variant.conditions)
                {
                    Require(condition != null, "Null text variant condition: " + node.id);
                    GetVariable(condition.variable);
                    Require(condition.minimum <= condition.maximum, "Invalid text variant bounds: " + node.id);
                }
            }

            Require(node.choices.Length <= 4, "At most four choices per node: " + node.id);
            int exits = (node.terminal ? 1 : 0) + (!string.IsNullOrEmpty(node.next) ? 1 : 0) + (node.choices.Length > 0 ? 1 : 0);
            Require(exits == 1, "Node must have exactly one exit mode: " + node.id);
            ValidateEffects(node.effects);
            if (!string.IsNullOrEmpty(node.next))
            {
                GetNode(node.next);
            }

            var choiceIds = new HashSet<string>();
            foreach (StoryChoice choice in node.choices)
            {
                Require(choice != null && !string.IsNullOrWhiteSpace(choice.id) && choiceIds.Add(choice.id), "Invalid choice ID: " + node.id);
                Require(!string.IsNullOrWhiteSpace(choice.text), "Missing choice text: " + choice.id);
                Require(choice.conditions != null && choice.effects != null && choice.outcomes != null,
                    "Missing choice arrays: " + choice.id);
                bool hasTarget = !string.IsNullOrEmpty(choice.target);
                Require(hasTarget != (choice.outcomes.Length > 0), "Choice needs one destination mode: " + choice.id);
                if (hasTarget)
                {
                    GetNode(choice.target);
                }

                long totalWeight = 0;
                foreach (StoryOutcome outcome in choice.outcomes)
                {
                    Require(outcome != null && outcome.weight > 0 && outcome.effects != null,
                        "Invalid random outcome: " + choice.id);
                    GetNode(outcome.target);
                    ValidateEffects(outcome.effects);
                    totalWeight += outcome.weight;
                }

                Require(totalWeight <= int.MaxValue, "Random outcome weights overflow: " + choice.id);
                ValidateEffects(choice.effects);
                foreach (StoryCondition condition in choice.conditions)
                {
                    Require(condition != null, "Null condition.");
                    GetVariable(condition.variable);
                    Require(condition.minimum <= condition.maximum, "Invalid condition bounds.");
                }
            }
        }

        private void ValidateEffects(IEnumerable<StoryEffect> effects)
        {
            foreach (StoryEffect effect in effects)
            {
                Require(effect != null, "Null effect.");
                VariableDefinition variable = GetVariable(effect.variable);
                Require(effect.operation == "set" || effect.operation == "add", "Unknown effect operation.");
                if (effect.operation == "set")
                {
                    Require(effect.value >= variable.minimum && effect.value <= variable.maximum, "Set effect outside bounds.");
                }
            }
        }

        private void ValidateReachability()
        {
            var visited = new HashSet<string>();
            var pending = new Stack<string>();
            pending.Push(Definition.start);
            while (pending.Count > 0)
            {
                var id = pending.Pop();
                if (!visited.Add(id))
                {
                    continue;
                }

                var node = _nodes[id];
                if (!string.IsNullOrEmpty(node.next))
                {
                    pending.Push(node.next);
                }

                foreach (var choice in node.choices)
                {
                    if (!string.IsNullOrEmpty(choice.target))
                    {
                        pending.Push(choice.target);
                    }

                    foreach (var outcome in choice.outcomes)
                    {
                        pending.Push(outcome.target);
                    }
                }
            }

            var unreachable = new List<string>(_nodes.Keys);
            unreachable.RemoveAll(visited.Contains);

            Require(unreachable.Count == 0, "Unreachable story nodes: " + string.Join(", ", unreachable));
        }

        internal static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
