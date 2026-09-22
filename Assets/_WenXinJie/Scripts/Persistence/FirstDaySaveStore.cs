using System;
using System.IO;
using UnityEngine;
using WenXinJie.Core;

namespace WenXinJie.Persistence
{
    [Serializable]
    public sealed class FirstDaySave
    {
        // Keep these names stable for existing version-1 saves.
        public int version = 1;
        public string session;
        public int line;
        public string message;
    }

    public sealed class FirstDaySaveStore
    {
        public void Save(string path, FirstDaySave data)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Save path must include a directory.", nameof(path));
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, path + ".bak");
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }

        public FirstDaySave Load(string path, int introLength, out bool recoveredBackup)
        {
            try
            {
                FirstDaySave data = ReadValidated(path, introLength);
                recoveredBackup = false;
                return data;
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                FirstDaySave data = ReadValidated(path + ".bak", introLength);
                recoveredBackup = true;
                return data;
            }
        }

        private static FirstDaySave ReadValidated(string path, int introLength)
        {
            FirstDaySave data = JsonUtility.FromJson<FirstDaySave>(File.ReadAllText(path));
            if (data == null || data.version != 1 || data.line < 0 || data.line > introLength)
            {
                throw new ArgumentException("Unsupported or invalid save.");
            }

            FirstDaySession restored = FirstDaySession.Restore(data.session);
            if (data.line < introLength && restored.Snapshot().actionsLeft != 2)
            {
                throw new ArgumentException("Inconsistent story progress.");
            }

            return data;
        }

        private static bool IsRecoverable(Exception exception)
        {
            return exception is IOException || exception is UnauthorizedAccessException
                || exception is ArgumentException;
        }
    }
}
