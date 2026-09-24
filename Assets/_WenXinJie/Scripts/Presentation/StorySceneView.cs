using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WenXinJie.Presentation
{
    internal sealed class StorySceneView
    {
        private const float BackgroundFadeDuration = 0.45f;
        private const float PortraitFadeDuration = 0.3f;
        private readonly RawImage _background;

        private readonly MonoBehaviour _coroutineRunner;
        private readonly RawImage _portrait;
        private readonly CanvasGroup _portraitGroup;
        private readonly RawImage _previousBackground;
        private Coroutine _backgroundTransition;
        private Coroutine _portraitTransition;

        public StorySceneView(Transform canvas, Transform gameplay, StoryUiFactory ui, MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
            _background = CreateBackdrop(canvas, ui, "Background");
            _previousBackground = CreateBackdrop(canvas, ui, "PreviousBackground");
            _background.transform.SetSiblingIndex(0);
            _previousBackground.transform.SetSiblingIndex(1);
            _previousBackground.color = Color.clear;

            _portrait = ui.Picture(gameplay, "Portrait", null, 0f, 0f, 1f, 1f);
            _portraitGroup = _portrait.gameObject.AddComponent<CanvasGroup>();
            _portrait.transform.SetAsFirstSibling();
            _portrait.gameObject.SetActive(false);
        }

        public void ShowBackground(Texture2D texture)
        {
            if (_background.texture == texture)
            {
                return;
            }

            StopTransition(ref _backgroundTransition);
            _previousBackground.texture = _background.texture;
            _background.texture = texture;
            _backgroundTransition = _coroutineRunner.StartCoroutine(FadeBackground());
        }

        public void ShowPortrait(PortraitAssetEntry portrait)
        {
            var texture = portrait?.Texture;
            if (_portrait.texture == texture && _portrait.gameObject.activeSelf == (texture != null))
            {
                return;
            }

            StopTransition(ref _portraitTransition);
            _portrait.texture = texture;
            if (texture == null)
            {
                _portrait.gameObject.SetActive(false);
                return;
            }

            var height = portrait.Width * texture.height / texture.width;
            var x = 1920f - portrait.Right - portrait.Width;
            _portrait.rectTransform.anchoredPosition = new(x, -portrait.Top);
            _portrait.rectTransform.sizeDelta = new(portrait.Width, height);
            _portrait.uvRect = new(0f, 0f, 1f, 1f);
            _portrait.gameObject.SetActive(true);
            _portraitGroup.alpha = 0f;
            _portraitTransition = _coroutineRunner.StartCoroutine(FadePortrait());
        }

        private static RawImage CreateBackdrop(Transform parent, StoryUiFactory ui, string objectName)
        {
            var image = ui.Picture(parent, objectName, null, 0f, 0f, 1920f, 1080f);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot =
                new(0.5f, 0.5f);
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return image;
        }

        private IEnumerator FadeBackground()
        {
            var elapsed = _previousBackground.texture == null ? BackgroundFadeDuration : 0f;
            while (elapsed < BackgroundFadeDuration)
            {
                var alpha = 1f - elapsed / BackgroundFadeDuration;
                _previousBackground.color = new(1f, 1f, 1f, alpha);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _previousBackground.color = Color.clear;
            _backgroundTransition = null;
        }

        private IEnumerator FadePortrait()
        {
            var elapsed = 0f;
            while (elapsed < PortraitFadeDuration)
            {
                _portraitGroup.alpha = elapsed / PortraitFadeDuration;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _portraitGroup.alpha = 1f;
            _portraitTransition = null;
        }

        private void StopTransition(ref Coroutine transition)
        {
            if (transition == null)
            {
                return;
            }

            _coroutineRunner.StopCoroutine(transition);
            transition = null;
        }
    }
}