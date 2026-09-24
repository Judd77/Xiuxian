using System;
using System.Collections.Generic;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Presentation
{
    [CreateAssetMenu(menuName = "WenXinJie/Story Media Catalog")]
    public sealed class StoryMediaCatalog : ScriptableObject
    {
        [SerializeField] private Font _font;
        [SerializeField] private Font _titleFont;
        [SerializeField] private Font _menuFont;
        [SerializeField] private Texture2D _panel;
        [SerializeField] private Texture2D _choice;
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _rain;
        [SerializeField] private MediaImage[] _backgrounds;
        [SerializeField] private PortraitMedia[] _portraits;

        public Font Font => _font;
        public Font TitleFont => _titleFont;
        public Font MenuFont => _menuFont;
        public Texture2D Panel => _panel;
        public Texture2D Choice => _choice;
        public AudioClip Music => _music;
        public AudioClip Rain => _rain;

        public Texture2D Background(string id)
        {
            return Find(_backgrounds, id);
        }

        public PortraitMedia Portrait(string id)
        {
            return string.IsNullOrEmpty(id) ? null : FindPortrait(id);
        }

        public void Validate(StoryGraph graph)
        {
            if (_font == null || _titleFont == null || _menuFont == null || _panel == null || _choice == null
                || _music == null || _rain == null)
            {
                throw new InvalidOperationException("The licensed media catalog is incomplete.");
            }

            ValidateImages(_backgrounds);
            ValidatePortraits();
            foreach (StoryNode node in graph.Nodes)
            {
                Background(node.background);
                Portrait(node.portrait);
            }
        }

        private static void ValidateImages(IEnumerable<MediaImage> images)
        {
            if (images == null)
            {
                throw new InvalidOperationException("Missing media entries.");
            }

            var ids = new HashSet<string>();
            foreach (MediaImage image in images)
            {
                if (image == null || string.IsNullOrWhiteSpace(image.id) || !ids.Add(image.id) || image.texture == null)
                {
                    throw new InvalidOperationException("Duplicate or missing media entry.");
                }
            }
        }

        private static Texture2D Find(IEnumerable<MediaImage> images, string id)
        {
            foreach (MediaImage image in images)
            {
                if (image.id == id && image.texture != null)
                {
                    return image.texture;
                }
            }

            throw new InvalidOperationException("Unregistered story image: " + id);
        }

        private PortraitMedia FindPortrait(string id)
        {
            foreach (PortraitMedia portrait in _portraits)
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
            if (_portraits == null)
            {
                throw new InvalidOperationException("Missing portrait entries.");
            }

            var ids = new HashSet<string>();
            foreach (PortraitMedia portrait in _portraits)
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
    public sealed class MediaImage
    {
        public string id;
        public Texture2D texture;
    }

    [Serializable]
    public sealed class PortraitMedia
    {
        [SerializeField] private string _id;
        [SerializeField] private Texture2D _texture;
        [SerializeField] private float _width = 1050f;
        [SerializeField] private float _top = 140f;
        [SerializeField] private float _right = 30f;

        public string Id => _id;
        public Texture2D Texture => _texture;
        public float Width => _width;
        public float Top => _top;
        public float Right => _right;

        public bool HasValidFraming => _width > 0f && _top >= 0f && _right >= 0f;
    }
}
