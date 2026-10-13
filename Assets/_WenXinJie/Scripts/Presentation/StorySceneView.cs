using System.Collections;
using TMPro;
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
        private readonly RawImage _previousPortrait;
        private readonly CanvasGroup _chapterCard;
        private readonly TMP_Text _chapterTitle;
        private Coroutine _backgroundTransition;
        private Coroutine _portraitTransition;
        private Coroutine _chapterTransition;
        private string _chapter;

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
            _previousPortrait = ui.Picture(gameplay, "PreviousPortrait", null, 0f, 0f, 1f, 1f);
            _previousPortrait.transform.SetSiblingIndex(1);
            _previousPortrait.color = Color.clear;
            _previousPortrait.gameObject.SetActive(false);

            var chapter = ui.Frame(gameplay, "ChapterArrival", 1000f, 164f, 824f, 50f);
            _chapterCard = chapter.gameObject.AddComponent<CanvasGroup>();
            _chapterCard.blocksRaycasts = false;
            _chapterCard.interactable = false;
            _chapterTitle = ui.Label(chapter.transform, "ChapterArrivalTitle", string.Empty,
                28f, 4f, 768f, 42f, 28);
            _chapterTitle.color = new(0.96f, 0.86f, 0.60f);
            _chapterTitle.alignment = TextAlignmentOptions.Center;
            _chapterCard.gameObject.SetActive(false);
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

        public void ShowPortrait(PortraitAssetEntry portrait, bool immediate = false)
        {
            var texture = portrait?.Texture;
            if (!immediate && _portrait.texture == texture && _portrait.gameObject.activeSelf == (texture != null))
            {
                return;
            }

            StopTransition(ref _portraitTransition);
            // Keep the dominant visible portrait when a player advances during a dissolve.
            var source = _previousPortrait.gameObject.activeSelf
                         && _previousPortrait.color.a > _portraitGroup.alpha
                ? _previousPortrait : _portrait;
            var previousAlpha = source == _previousPortrait ? source.color.a : _portraitGroup.alpha;
            _previousPortrait.texture = source.texture;
            _previousPortrait.rectTransform.anchoredPosition = source.rectTransform.anchoredPosition;
            _previousPortrait.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
            _previousPortrait.color = new(1f, 1f, 1f, previousAlpha);
            _previousPortrait.gameObject.SetActive(source.texture != null);
            _portrait.texture = texture;
            if (texture == null)
            {
                _portrait.gameObject.SetActive(false);
            }
            else
            {
                var height = portrait.Width * texture.height / texture.width;
                var x = 1920f - portrait.Right - portrait.Width;
                _portrait.rectTransform.anchoredPosition = new(x, -portrait.Top);
                _portrait.rectTransform.sizeDelta = new(portrait.Width, height);
                _portrait.uvRect = new(0f, 0f, 1f, 1f);
                _portrait.gameObject.SetActive(true);
            }
            if (immediate)
            {
                _portraitGroup.alpha = 1f;
                _previousPortrait.gameObject.SetActive(false);
                _previousPortrait.texture = null;
                return;
            }
            _portraitGroup.alpha = 0f;
            _portraitTransition = _coroutineRunner.StartCoroutine(FadePortrait(previousAlpha));
        }

        public void ShowChapter(string chapter, bool ending)
        {
            if (_chapter == chapter) return;
            _chapter = chapter;
            HideChapter();
            if (ending) return;
            _chapterTitle.text = chapter;
            _chapterCard.alpha = 0f;
            _chapterCard.gameObject.SetActive(true);
            _chapterTransition = _coroutineRunner.StartCoroutine(FadeChapter());
        }

        public void HideChapter()
        {
            StopTransition(ref _chapterTransition);
            _chapterCard.gameObject.SetActive(false);
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

        private IEnumerator FadePortrait(float previousAlpha)
        {
            var elapsed = 0f;
            while (elapsed < PortraitFadeDuration)
            {
                var progress = Mathf.SmoothStep(0f, 1f, elapsed / PortraitFadeDuration);
                _portraitGroup.alpha = progress;
                _previousPortrait.color = new(1f, 1f, 1f, previousAlpha * (1f - progress));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _portraitGroup.alpha = 1f;
            _previousPortrait.gameObject.SetActive(false);
            _previousPortrait.texture = null;
            _portraitTransition = null;
        }

        private IEnumerator FadeChapter()
        {
            var elapsed = 0f;
            while (elapsed < 1.8f)
            {
                _chapterCard.alpha = elapsed < 0.25f
                    ? Mathf.SmoothStep(0f, 1f, elapsed / 0.25f)
                    : Mathf.SmoothStep(0f, 1f, (1.8f - elapsed) / 0.4f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _chapterCard.gameObject.SetActive(false);
            _chapterTransition = null;
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
