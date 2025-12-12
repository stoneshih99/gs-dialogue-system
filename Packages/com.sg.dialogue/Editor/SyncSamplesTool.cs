using UnityEditor;
using UnityEngine;
using System.IO;

namespace SG.Dialogue.Editor
{
    public static class SyncSamplesTool
    {
        // --- 請根據您的 package.json 進行設定 ---

        // package.json 中的 "name"
        private const string PackageName = "com.sg.dialogue";
        
        // package.json 中的 "displayName" (目前未使用，但保留參考)
        private const string PackageDisplayName = "SG Dialogue System";

        // 在 Assets/Samples/ 底下的範例資料夾名稱
        private const string ImportedSampleFolderName = "SG Dialogue System";

        // package.json -> samples -> "path" 的最後一部分 (資料夾名稱)
        // 例如 "Samples~/Dialogue"，這裡就填 "Dialogue"
        private const string SampleFolderNameInPackage = "Dialogue";


        [MenuItem("Tools/SG Dialogue/Sync Samples Back to Package")]
        public static void SyncSamplesBackToPackage()
        {
            // 來源路徑: Assets/Samples/[ImportedSampleFolderName]
            string sourcePath = Path.Combine(Application.dataPath, "Samples", ImportedSampleFolderName);

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"來源資料夾不存在，無法同步！\n請確認 ImportedSampleFolderName 的設定是否正確。\n預期路徑：{sourcePath}");
                return;
            }

            // 目標路徑: Packages/[PackageName]/Samples~/[SampleFolderNameInPackage]
            string destinationPath = Path.Combine("Packages", PackageName, "Samples~", SampleFolderNameInPackage);

            // 1. 確保目標路徑是乾淨的。如果舊的範例資料夾存在，就刪除它。
            if (Directory.Exists(destinationPath))
            {
                FileUtil.DeleteFileOrDirectory(destinationPath);
                // Also delete the meta file for the directory itself
                FileUtil.DeleteFileOrDirectory(destinationPath + ".meta");
            }
            
            // 2. 使用 Unity 的 API 複製整個資料夾。
            // 這會正確處理 .meta 檔案，保留所有資產的 GUID 與參照關係。
            FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);

            // 3. 刷新資產資料庫，讓 Unity 編輯器知道檔案已變更
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>範例已成功同步回套件！</color>\n來源：{sourcePath}\n目標：{destinationPath}");
        }
    }
}
