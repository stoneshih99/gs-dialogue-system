using UnityEditor;
using UnityEngine;
using System.IO;

namespace SG.Dialogue.Editor
{
    public static class SyncSamplesTool
    {
        // 這是您套件的名稱，用於找到正確的路徑
        private const string PackageName = "com.sg.dialogue";
        
        // 這是您在 Assets 中範例資料夾的名稱，通常是套件的 displayName
        private const string SamplesFolderNameInAssets = "SG Dialogue System";

        [MenuItem("Tools/SG Dialogue/Sync Samples Back to Package")]
        public static void SyncSamples()
        {
            // 來源路徑：Assets/Samples/SG Dialogue System/
            string sourcePath = Path.Combine(Application.dataPath, "Samples", SamplesFolderNameInAssets);

            // 目標路徑：Packages/com.sg.dialogue/Samples~
            string destinationPath = Path.Combine("Packages", PackageName, "Samples~");

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"來源資料夾不存在，無法同步！請先從 Package Manager 匯入範例。\n路徑：{sourcePath}");
                return;
            }

            // 1. 確保目標路徑是乾淨的。如果舊的 Samples~ 資料夾存在，就刪除它。
            // 這樣可以確保在來源處被刪除的檔案也會同步消失。
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            
            // 2. 使用 Unity 的 API 複製整個資料夾。
            // 這個 API 會自動處理 .meta 檔案，並且會自己建立目標資料夾。
            FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);

            // 3. 刷新資產資料庫，讓 Unity 編輯器知道檔案已變更
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>範例已成功同步回套件！</color>\n來源：{sourcePath}\n目標：{destinationPath}");
        }
    }
}
