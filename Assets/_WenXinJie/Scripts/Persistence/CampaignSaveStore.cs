using System;
using System.IO;
using UnityEngine;
using WenXinJie.Narrative;

namespace WenXinJie.Persistence
{
    public sealed class CampaignSaveStore
    {
        public void Save(string path, StorySession session)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
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
                StorySession backup = Read(path + ".bak", graph);
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

            StorySnapshot snapshot = JsonUtility.FromJson<StorySnapshot>(File.ReadAllText(path));
            return StorySession.Restore(graph, snapshot);
        }
    }
}
