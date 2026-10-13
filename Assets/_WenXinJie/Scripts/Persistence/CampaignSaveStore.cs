using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Persistence
{
    public sealed class CampaignSaveStore
    {
        public const int MaximumHistoryEntries = 400;
        private const int MaximumHistoryEntryLength = 4096;
        private const int MaximumHistoryCharacters = 100000;

        // Keep optional reading metadata in the save transport, outside narrative state.
        [Serializable]
        private sealed class SavedCampaign
        {
            public int version;
            public string campaign;
            public int revision;
            public string node;
            public int randomState;
            public VariableValue[] values;
            public string[] readingHistory;
        }

        public bool Exists(string path)
        {
            return File.Exists(path) || File.Exists(path + ".bak");
        }

        public void Save(string path, StorySession session, string[] readingHistory = null)
        {
            var snapshot = session.Snapshot();
            var document = new SavedCampaign
            {
                version = snapshot.version, campaign = snapshot.campaign, revision = snapshot.revision,
                node = snapshot.node, randomState = snapshot.randomState, values = snapshot.values,
                readingHistory = NormalizeHistory(readingHistory)
            };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(document, true));
            if (File.Exists(path))
            {
                File.Replace(temporary, path, path + ".bak");
            }
            else
            {
                File.Move(temporary, path);
            }
        }

        public StorySession Load(string path, StoryGraph graph, out bool recovered)
        {
            return Load(path, graph, out recovered, out _);
        }

        public StorySession Load(string path, StoryGraph graph, out bool recovered, out string[] readingHistory)
        {
            recovered = false;
            try
            {
                return Read(path, graph, out readingHistory);
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                var backup = Read(path + ".bak", graph, out readingHistory);
                recovered = true;
                return backup;
            }
        }

        private static StorySession Read(string path, StoryGraph graph, out string[] readingHistory)
        {
            var file = new FileInfo(path);
            if (file.Length > 1024 * 1024)
            {
                throw new InvalidDataException("Save is unexpectedly large.");
            }

            var json = File.ReadAllText(path);
            var session = StorySession.Restore(graph, JsonUtility.FromJson<StorySnapshot>(json));
            readingHistory = Array.Empty<string>();
            try { readingHistory = NormalizeHistory(JsonUtility.FromJson<SavedCampaign>(json)?.readingHistory); }
            catch (ArgumentException) { /* Optional display metadata must not reject valid story progress. */ }
            return session;
        }

        private static string[] NormalizeHistory(string[] entries)
        {
            if (entries == null) return Array.Empty<string>();
            var recent = new List<string>();
            var characters = 0;
            for (var index = entries.Length - 1; index >= 0 && recent.Count < MaximumHistoryEntries; index--)
            {
                var entry = entries[index];
                if (string.IsNullOrWhiteSpace(entry) || entry.Length > MaximumHistoryEntryLength) continue;
                if (characters + entry.Length > MaximumHistoryCharacters) break;
                recent.Add(entry);
                characters += entry.Length;
            }

            recent.Reverse();
            return recent.ToArray();
        }
    }
}
