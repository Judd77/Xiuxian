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
        [SerializeField] private Texture2D _panel;
        [SerializeField] private Texture2D _choice;
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _rain;
        [SerializeField] private MediaImage[] _backgrounds;
        [SerializeField] private MediaImage[] _portraits;

        public Font Font => _font;
        public Texture2D Panel => _panel;
        public Texture2D Choice => _choice;
        public AudioClip Music => _music;
        public AudioClip Rain => _rain;

        public Texture2D Background(string id)
        {
            return Find(_backgrounds, id);
        }

        public Texture2D Portrait(string id)
        {
            return string.IsNullOrEmpty(id) ? null : Find(_portraits, id);
        }

        public void Validate(StoryGraph graph)
        {
            if (_font == null || _panel == null || _choice == null || _music == null || _rain == null)
            {
                throw new InvalidOperationException("The licensed media catalog is incomplete.");
            }

            ValidateImages(_backgrounds);
            ValidateImages(_portraits);
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
    }

    [Serializable]
    public sealed class MediaImage
    {
        public string id;
        public Texture2D texture;
    }
}
