using System;
using UnityEditor;
using UnityEngine;
using WenXinJie.Presentation;
using WenXinJie.UI;
using Object = UnityEngine.Object;

namespace WenXinJie.Editor
{
    public static class CampaignPlayModeChecks
    {
        public static string LastReport { get; private set; } = "Not run";

        [MenuItem("WenXinJie/Validation/Run Campaign Play Mode Checks")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode first.");
            var application = Object.FindAnyObjectByType<StoryApplication>();
            if (application == null)
                throw new InvalidOperationException("StoryApplication is missing.");
            var reader = application.GetComponent<DialogueReader>();
            if (reader == null)
                throw new InvalidOperationException("DialogueReader is missing.");

            application.StartNewGame();
            if (application.Session.Current.id != "case.intro")
                throw new InvalidOperationException("New story did not start at the missing disciple.");
            var step = 0;
            while (application.Session.Current.id != "case.boundary" && step++ < 100)
            {
                reader.Complete();
                if (application.Session.Current.choices.Length > 0)
                    application.SelectChoice(application.Session.Current.choices[0].id);
                else
                    application.AdvanceStory();
            }
            if (application.Session.Current.id != "case.boundary")
                throw new InvalidOperationException("Opening did not reach its Demo boundary.");
            if (application.Session.GetValue("story.ache") != 1)
                throw new InvalidOperationException("Rescue evidence was not recorded.");
            LastReport = "PASS: new story opening rendered to Demo boundary.";
            Debug.Log(LastReport);
        }
    }
}
