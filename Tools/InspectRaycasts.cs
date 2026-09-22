using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Canvas.ForceUpdateCanvases();
        result.Log("Screen: " + Screen.width + "x" + Screen.height + "; event system: " + EventSystem.current);
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            result.Log(button.name + "; center=" + point + "; hits=" + hits.Count + "; first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
        }
    }
}
