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
        
        // package.json 中的 "displayName"
        private const string PackageDisplayName = "SG Dialogue System";

        // // package.json 中的 "version"
        // private const string PackageVersion = "1.0.0";
        //
        // // --- 請根據您的 samples 陣列進行設定 ---
        //
        // // package.json -> samples -> "displayName"
        // private const string SampleDisplayName = "1. Basic Dialogue Example";

        // package.json -> samples -> "path" 的最後一部分 (資料夾名稱)
        // 例如 "Samples~/Dialogue"，這裡就填 "Dialogue"
        private const string SampleFolderNameInPackage = "Dialogue";


        [MenuItem("Tools/SG Dialogue/Sync Samples Back to Package")]
        public static void SyncSamplesBackToPackage()
        {
            // 來源路徑: Assets/Samples/[PackageDisplayName]/[Version]/[SampleDisplayName]
            // string sourcePath = Path.Combine(Application.dataPath, "Samples", PackageDisplayName, PackageVersion, SampleDisplayName);
            
            string sourcePath = Path.Combine(Application.dataPath, "Samples", PackageDisplayName);

            // 目標路徑: Packages/[PackageName]/Samples~/[SampleFolderNameInPackage]
            string destinationPath = Path.Combine("Packages", PackageName, "Samples~", SampleFolderNameInPackage);

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"來源資料夾不存在，無法同步！\n請先從 Package Manager 匯入範例，並確認路徑設定是否正確。\n預期路徑：{sourcePath}");
                return;
            }

            // 1. 確保目標路徑是乾淨的。如果舊的範例資料夾存在，就刪除它。
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            
            // 2. 建立目標資料夾
            Directory.CreateDirectory(destinationPath);

            // 3. 使用 Unity 的 API 複製整個資料夾的內容
            // FileUtil.CopyFileOrDirectory 複製的是資料夾本身，我們需要的是複製其內容
            // 所以我們手動遍歷並複製
            CopyDirectoryContents(sourcePath, destinationPath);

            // 4. 刷新資產資料庫，讓 Unity 編輯器知道檔案已變更
            AssetDatabase.Refresh();

            Debug.Log($"<color=lime>範例已成功同步回套件！</color>\n來源：{sourcePath}\n目標：{destinationPath}");
        }

        private static void CopyDirectoryContents(string sourceDir, string destDir)
        {
            // 複製所有檔案
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                // 排除 .meta 檔案，讓 Unity 自己重新生成
                if (file.EndsWith(".meta")) continue;

                string destFile = file.Replace(sourceDir, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }
        }
    }
}
