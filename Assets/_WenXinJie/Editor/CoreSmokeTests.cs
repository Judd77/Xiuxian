using System;
using UnityEditor;
using UnityEngine;
using WenXinJie.Core;

namespace WenXinJie.Editor
{
    public static class CoreSmokeTests
    {
        [MenuItem("WenXinJie/Validation/Run Core Smoke Tests")]
        public static void Run()
        {
            int cases = 0;
            foreach (DayAction first in Enum.GetValues(typeof(DayAction)))
            {
                foreach (DayAction second in Enum.GetValues(typeof(DayAction)))
                {
                    if (first == second)
                    {
                        continue;
                    }

                    VerifyRoute(first, second);
                    cases++;
                }
            }

            VerifyInvalidInputs();
            Debug.Log("WenXinJie core smoke tests PASS: " + cases
                + " ordered routes; reload, duplicate rewards, ending idempotence, invalid inputs.");
        }

        private static void VerifyRoute(DayAction first, DayAction second)
        {
            var session = new FirstDaySession();
            Require(session.TryChoose(first, out _), "First action rejected");
            string checkpoint = session.ToJson();
            Require(!session.TryChoose(first, out _), "Duplicate action accepted");
            Require(session.ToJson() == checkpoint, "Rejected action mutated state");

            session = FirstDaySession.Restore(checkpoint);
            Require(!session.TryChoose(first, out _), "Reload grants repeated rewards");
            Require(session.TryChoose(second, out _), "Second action rejected");
            Require(!session.TryChoose(DayAction.Work, out _), "Third action accepted");

            NightOutcome expected = ExpectedOutcome(first, second);
            Require(session.ResolveNight() == expected, "Wrong ending");
            string finished = session.ToJson();
            Require(session.ResolveNight() == expected && session.ToJson() == finished, "Ending not idempotent");
            Require(FirstDaySession.Restore(finished).ToJson() == finished, "Save roundtrip mismatch");

            FirstDayState copy = session.Snapshot();
            copy.trust = 999;
            Require(session.ToJson() == finished, "Snapshot exposed mutable state");
        }

        private static NightOutcome ExpectedOutcome(DayAction first, DayAction second)
        {
            if (first == DayAction.Investigate || second == DayAction.Investigate)
            {
                return first == DayAction.VisitSister || second == DayAction.VisitSister
                    ? NightOutcome.SharedSecret
                    : NightOutcome.LoneWitness;
            }

            return first == DayAction.Cultivate || second == DayAction.Cultivate
                ? NightOutcome.GuardedGate
                : NightOutcome.UneasyNight;
        }

        private static void VerifyInvalidInputs()
        {
            var fresh = new FirstDaySession();
            Require(!fresh.TryChoose((DayAction)99, out _), "Invalid action accepted");
            bool prematureRejected = false;

            try
            {
                fresh.ResolveNight();
            }
            catch (InvalidOperationException)
            {
                prematureRejected = true;
            }

            Require(prematureRejected, "Premature ending accepted");
            string[] invalidSaves =
            {
                "", "not json", "null", "{\"version\":99}", "{\"version\":1,\"actionsLeft\":7}"
            };

            foreach (string save in invalidSaves)
            {
                bool rejected = false;
                try
                {
                    FirstDaySession.Restore(save);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                Require(rejected, "Invalid save accepted: " + save);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("WenXinJie test failed: " + message);
            }
        }
    }
}
