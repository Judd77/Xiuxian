using System;
using System.IO;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Persistence
{
    public sealed class CampaignSaveStore
    {
        public bool Exists(string path)
        {
            return File.Exists(path) || File.Exists(path + ".bak");
        }

        public void Save(string path, StorySession session)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(session.Snapshot(), true));
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
            recovered = false;
            try
            {
                return Read(path, graph);
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                var backup = Read(path + ".bak", graph);
                recovered = true;
                return backup;
            }
        }

        private static StorySession Read(string path, StoryGraph graph)
        {
            var file = new FileInfo(path);
            if (file.Length > 1024 * 1024)
            {
                throw new InvalidDataException("Save is unexpectedly large.");
            }

            var snapshot = JsonUtility.FromJson<StorySnapshot>(File.ReadAllText(path));
            return StorySession.Restore(graph, snapshot);
        }
    }
}