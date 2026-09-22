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
                Require(!string.IsNullOrWhiteSpace(choice.text) && choice.conditions != null && choice.effects != null, "Invalid choice: " + choice.id);
                GetNode(choice.target);
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

        internal static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
