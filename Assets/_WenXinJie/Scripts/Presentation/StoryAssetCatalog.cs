using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    [CreateAssetMenu(menuName = "WenXinJie/Story Asset Catalog")]
    public sealed class StoryAssetCatalog : ScriptableObject
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset menuFont;
        [SerializeField] private Texture2D panel;
        [SerializeField] private Texture2D choice;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip rain;
        [SerializeField] private BackgroundAssetEntry[] backgrounds;
        [SerializeField] private PortraitAssetEntry[] portraits;

        public TMP_FontAsset Font => font;
        public TMP_FontAsset TitleFont => titleFont;
        public TMP_FontAsset MenuFont => menuFont;
        public Texture2D PanelTexture => panel;
        public Texture2D ChoiceFrameTexture => choice;
        public AudioClip MusicClip => music;
        public AudioClip RainClip => rain;

        public Texture2D GetBackground(string id)
        {
            return FindBackground(id);
        }

        public PortraitAssetEntry GetPortrait(string id)
        {
            return string.IsNullOrEmpty(id) ? null : FindPortrait(id);
        }

        public void Validate(StoryGraph graph)
        {
            if (font == null || titleFont == null || menuFont == null || panel == null || choice == null
                || music == null || rain == null)
            {
                throw new InvalidOperationException("The licensed story asset catalog is incomplete.");
            }

            ValidateImages(backgrounds);
            ValidatePortraits();
            foreach (var node in graph.Nodes)
            {
                GetBackground(node.background);
                GetPortrait(node.portrait);
            }
        }

        private static void ValidateImages(IEnumerable<BackgroundAssetEntry> entries)
        {
            if (entries == null)
            {
                throw new InvalidOperationException("Missing story asset entries.");
            }

            var ids = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id)
                    || entry.Texture == null)
                {
                    throw new InvalidOperationException("Duplicate or missing story asset entry.");
                }
            }
        }

        private Texture2D FindBackground(string id)
        {
            foreach (var entry in backgrounds)
            {
                if (entry.Id == id && entry.Texture != null)
                {
                    return entry.Texture;
                }
            }

            throw new InvalidOperationException("Unregistered story image: " + id);
        }

        private PortraitAssetEntry FindPortrait(string id)
        {
            foreach (var portrait in portraits)
            {
                if (portrait.Id == id && portrait.Texture != null)
                {
                    return portrait;
                }
            }

            throw new InvalidOperationException("Unregistered story portrait: " + id);
        }

        private void ValidatePortraits()
        {
            if (portraits == null)
            {
                throw new InvalidOperationException("Missing portrait entries.");
            }

            var ids = new HashSet<string>();
            foreach (var portrait in portraits)
            {
                if (portrait == null || string.IsNullOrWhiteSpace(portrait.Id) || !ids.Add(portrait.Id)
                    || portrait.Texture == null || !portrait.HasValidFraming)
                {
                    throw new InvalidOperationException("Duplicate, missing, or invalid portrait entry.");
                }
            }
        }
    }

    [Serializable]
    public sealed class BackgroundAssetEntry
    {
        [SerializeField] private string id;
        [SerializeField] private Texture2D texture;

        public string Id => id;
        public Texture2D Texture => texture;
    }

    [Serializable]
    public sealed class PortraitAssetEntry
    {
        [SerializeField] private string id;
        [SerializeField] private Texture2D texture;
        [SerializeField] private float width = 1050f;
        [SerializeField] private float top = 140f;
        [SerializeField] private float right = 30f;

        public string Id => id;
        public Texture2D Texture => texture;
        public float Width => width;
        public float Top => top;
        public float Right => right;

        public bool HasValidFraming => width > 0f && top >= 0f && right >= 0f;
    }
}