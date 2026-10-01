using System.IO;
using UnityEditor;

namespace GodTower.Editor
{
    public static class AssetFolders
    {
        /// <summary>Creates every missing folder of an <c>Assets/...</c> path through the AssetDatabase.</summary>
        public static void Ensure(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                Ensure(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
