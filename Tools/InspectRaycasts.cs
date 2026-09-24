using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Canvas.ForceUpdateCanvases();
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            throw new System.InvalidOperationException("No EventSystem is active.");
        }

        result.Log("Screen: " + Screen.width + "x" + Screen.height + "; event system: " + eventSystem);
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var rect = (RectTransform)button.transform;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = point }, hits);
            result.Log(
                button.name + "; center=" + point + "; hits=" + hits.Count
                + "; first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
        }
    }
}
