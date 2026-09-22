using System;
using UnityEngine;

namespace WenXinJie.Core
{
    public enum DayAction
    {
        Cultivate,
        Investigate,
        VisitSister,
        Work
    }

    public enum NightOutcome
    {
        Unresolved,
        SharedSecret,
        LoneWitness,
        GuardedGate,
        UneasyNight
    }

    [Serializable]
    public sealed class FirstDayState
    {
        public int version = 1;
        public int actionsLeft = 2;
        public int cultivation;
        public int trust;
        public int spiritStones = 1;
        public bool hasEvidence;
        public int visitedMask;
        public bool resolved;
        public NightOutcome outcome;
    }

    // Deterministic prototype rules; no scene or UI dependencies.
    public sealed class FirstDaySession
    {
        private readonly FirstDayState _state;

        public FirstDaySession()
        {
            _state = new FirstDayState();
        }

        private FirstDaySession(FirstDayState restored)
        {
            _state = restored;
        }

        public FirstDayState Snapshot()
        {
            return JsonUtility.FromJson<FirstDayState>(ToJson());
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(_state, true);
        }

        public bool CanChoose(DayAction action, out string reason)
        {
            if (!Enum.IsDefined(typeof(DayAction), action))
            {
                reason = "未知行动。";
                return false;
            }

            if (_state.resolved || _state.actionsLeft == 0)
            {
                reason = "今日行动已结束。";
                return false;
            }

            if ((_state.visitedMask & (1 << (int)action)) != 0)
            {
                reason = "今日已完成此行动。";
                return false;
            }

            if (action == DayAction.Investigate && _state.spiritStones < 1)
            {
                reason = "调查需要一枚灵石打点守阁人。";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryChoose(DayAction action, out string feedback)
        {
            if (!CanChoose(action, out feedback))
            {
                return false;
            }

            switch (action)
            {
                case DayAction.Cultivate:
                    _state.cultivation += 2;
                    feedback = "你稳住了入门剑诀。修为 +2，但远处的争执已散去。";
                    break;
                case DayAction.Investigate:
                    _state.spiritStones--;
                    _state.hasEvidence = true;
                    feedback = "你以一枚灵石换来旧名册：去年登仙的弟子，名字旁都写着‘销籍’。获得证据。";
                    break;
                case DayAction.VisitSister:
                    _state.trust += 2;
                    feedback = "你替林月收拾行囊。她终于承认，调令并非她的本意。信任 +2。";
                    break;
                default:
                    _state.spiritStones += 2;
                    feedback = "你替药庐整理药材，挣得两枚灵石，却错过了白昼的传闻。";
                    break;
            }
            _state.visitedMask |= 1 << (int)action;
            _state.actionsLeft--;
            return true;
        }

        public NightOutcome ResolveNight()
        {
            if (_state.resolved)
            {
                return _state.outcome;
            }

            if (_state.actionsLeft != 0)
            {
                throw new InvalidOperationException("Complete both actions first.");
            }

            _state.outcome = _state.hasEvidence
                ? (_state.trust >= 2 ? NightOutcome.SharedSecret : NightOutcome.LoneWitness)
                : (_state.cultivation >= 2 ? NightOutcome.GuardedGate : NightOutcome.UneasyNight);
            _state.resolved = true;
            return _state.outcome;
        }

        public static FirstDaySession Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Empty save.");
            }

            FirstDayState data;
            try
            {
                data = JsonUtility.FromJson<FirstDayState>(json);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Invalid save JSON.", exception);
            }

            Validate(data);
            return new FirstDaySession(data);
        }

        private static void Validate(FirstDayState s)
        {
            if (s == null || s.version != 1 || s.visitedMask < 0 || s.visitedMask > 15)
            {
                throw new ArgumentException("Unsupported or invalid save.");
            }

            int count = 0;
            for (int index = 0; index < 4; index++)
            {
                if ((s.visitedMask & (1 << index)) != 0)
                {
                    count++;
                }
            }

            bool trained = (s.visitedMask & 1) != 0;
            bool investigated = (s.visitedMask & 2) != 0;
            bool visited = (s.visitedMask & 4) != 0;
            bool worked = (s.visitedMask & 8) != 0;
            if (count > 2 || s.actionsLeft != 2 - count || s.cultivation != (trained ? 2 : 0)
                || s.trust != (visited ? 2 : 0) || s.hasEvidence != investigated
                || s.spiritStones != 1 + (worked ? 2 : 0) - (investigated ? 1 : 0))
            {
                throw new ArgumentException("Inconsistent save state.");
            }

            NightOutcome expected = investigated ? (visited ? NightOutcome.SharedSecret : NightOutcome.LoneWitness)
                : (trained ? NightOutcome.GuardedGate : NightOutcome.UneasyNight);
            if ((s.resolved && (count != 2 || s.outcome != expected))
                || (!s.resolved && s.outcome != NightOutcome.Unresolved))
            {
                throw new ArgumentException("Invalid ending state.");
            }
        }
    }
}
