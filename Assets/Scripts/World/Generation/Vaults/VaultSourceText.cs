using System.IO;
using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    /// <summary>
    /// Resolves .vault file text from a catalog <see cref="TextAsset"/> or path under Assets/.
    /// </summary>
    public static class VaultSourceText
    {
        public static bool TryRead(DungeonVaultCatalogEntry entry, out string text, out string error)
        {
            text = null;
            error = null;

            if (entry?.sourceFile != null)
            {
                text = entry.sourceFile.text;
                if (!string.IsNullOrEmpty(text))
                    return true;

                error = $"TextAsset '{entry.sourceFile.name}' is empty.";
                return false;
            }

            if (!string.IsNullOrEmpty(entry?.sourceAssetPath))
            {
                if (!TryReadFileAtAssetPath(entry.sourceAssetPath, out text, out error))
                    return false;

                return true;
            }

            error = "No sourceFile or sourceAssetPath on catalog entry.";
            return false;
        }

        /// <summary>Accepts paths relative to Assets/ (Data/...) or legacy Assets/Data/... forms.</summary>
        public static bool TryReadFileAtAssetPath(string assetPath, out string text, out string error)
        {
            text = null;
            error = null;

            if (string.IsNullOrEmpty(assetPath))
            {
                error = "Asset path is empty.";
                return false;
            }

            string relative = assetPath.Replace('\\', '/').Trim();
            if (relative.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring("Assets/".Length);

            string fullPath = Path.Combine(Application.dataPath, relative);
            if (!File.Exists(fullPath))
            {
                error = $"Vault file not found at '{fullPath}'.";
                return false;
            }

            text = File.ReadAllText(fullPath);
            return true;
        }
    }
}
