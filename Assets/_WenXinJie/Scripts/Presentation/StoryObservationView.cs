using TMPro;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    internal sealed class StoryObservationView
    {
        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private readonly TMP_Text _caption;
        private readonly TMP_Text _leftLabel;
        private readonly TMP_Text _rightLabel;
        private readonly TMP_Text _leftNote;
        private readonly TMP_Text _rightNote;
        private readonly ObservationInk _ink;

        public StoryObservationView(Transform parent, StoryUiFactory ui)
        {
            var panel = ui.Panel(parent, "ObservationPanel", 874f, 220f, 950f, 478f);
            panel.raycastTarget = false;
            _root = panel.gameObject;
            _title = ui.Label(panel.transform, "ObservationTitle", "", 34f, 20f, 882f, 48f, 32);
            _title.color = new(0.95f, 0.82f, 0.54f);
            _caption = ui.Label(panel.transform, "ObservationCaption", "", 34f, 76f, 882f, 66f, 23);
            _caption.color = new(0.83f, 0.90f, 0.85f);
            _ink = StoryUiFactory.Box(panel.transform, "ObservationInk", 40f, 146f, 870f, 226f)
                .gameObject.AddComponent<ObservationInk>();
            _ink.raycastTarget = false;
            _leftLabel = ui.Label(panel.transform, "LeftSample", "", 94f, 158f, 290f, 44f, 25);
            _rightLabel = ui.Label(panel.transform, "RightSample", "", 576f, 158f, 290f, 44f, 25);
            _leftLabel.alignment = _rightLabel.alignment = TextAlignmentOptions.Center;
            _leftNote = ui.Label(panel.transform, "LeftObservation", "", 46f, 392f, 404f, 70f, 22);
            _rightNote = ui.Label(panel.transform, "RightObservation", "", 510f, 392f, 404f, 70f, 22);
            _leftNote.alignment = _rightNote.alignment = TextAlignmentOptions.Center;
            _root.SetActive(false);
        }

        public void Render(StoryObservation observation)
        {
            _root.SetActive(observation != null);
            if (observation == null)
            {
                return;
            }
            _title.text = observation.title;
            _caption.text = observation.caption;
            _leftLabel.text = observation.leftLabel;
            _rightLabel.text = observation.rightLabel;
            _leftNote.text = observation.leftNote;
            _rightNote.text = observation.rightNote;
            var labelColor = observation.kind == "residue"
                ? new Color(0.13f, 0.23f, 0.22f) : new Color(0.95f, 0.91f, 0.75f);
            _leftLabel.color = _rightLabel.color = labelColor;
            _ink.Present(observation);
        }
    }
}
