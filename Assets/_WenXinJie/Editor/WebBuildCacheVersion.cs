using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WenXinJie.Editor
{
    // Changing COS object headers leaves its content ETag unchanged. Version the resource
    // URLs so browsers do not reuse a compressed response cached before those headers existed.
    public sealed class WebBuildCacheVersion : IPostprocessBuildWithReport
    {
        private static readonly Regex AssetUrl = new(
            @"(?<prefix>\bbuildUrl\s*\+\s*"")(?<url>/[^""]+)(?<suffix>"")");

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                PrepareOutput(report.summary.outputPath);
            }
        }

        public static void PrepareOutput(string directory)
        {
            var root = Path.GetFullPath(directory);
            var htmlPath = Path.Combine(root, "index.html");
            var buildRoot = Path.GetFullPath(Path.Combine(root, "Build")) + Path.DirectorySeparatorChar;
            var original = File.ReadAllText(htmlPath);
            var count = 0;
            var updated = AssetUrl.Replace(original, match =>
            {
                var parts = match.Groups["url"].Value.Split(new[] { '?' }, 2);
                var file = Path.GetFullPath(Path.Combine(buildRoot, parts[0].Substring(1)));
                if (!file.StartsWith(buildRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BuildFailedException("Web resource is outside the build directory.");
                }

                using var stream = File.OpenRead(file);
                using var hasher = SHA256.Create();
                var version = BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "")
                    .Substring(0, 16).ToLowerInvariant();
                var query = parts.Length == 2
                    ? parts[1].Split('&').Where(item => !item.StartsWith("wxv=", StringComparison.Ordinal))
                        .Where(item => !string.IsNullOrEmpty(item)).ToList()
                    : new System.Collections.Generic.List<string>();
                query.Add("wxv=" + version);
                count++;
                return match.Groups["prefix"].Value + parts[0] + "?" + string.Join("&", query)
                       + match.Groups["suffix"].Value;
            });

            if (count == 0)
            {
                throw new BuildFailedException("No Unity Web resource URLs were found in index.html.");
            }

            if (updated != original)
            {
                File.WriteAllText(htmlPath, updated, new UTF8Encoding(false));
            }
            Debug.Log($"Web upload prepared: {count} resource URLs versioned in {htmlPath}");
        }
    }
}
