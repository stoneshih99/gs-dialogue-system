using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace SG.Dialogue.Presentation
{
    // /// <summary>
    // /// DialogueCharacterDatabase 是一個 ScriptableObject，用於集中管理角色的預設設定，
    // /// 例如預設的 Spine 設定、預設的立繪圖片等。
    // /// DialogueController 可以在運行時根據 TextNode.speakerName 查詢此資料庫，以獲取對應角色的設定。
    // /// </summary>
    // [CreateAssetMenu(fileName = "DialogueCharacterDatabase", menuName = "SG/Dialogue/Character Database", order = 10)]
    // public class DialogueCharacterDatabase : ScriptableObject
    // {
    //     /// <summary>
    //     /// CharacterEntry 定義了單個角色的預設設定。
    //     /// </summary>
    //     [Serializable]
    //     public class CharacterEntry
    //     {
    //         [Tooltip("角色名稱，需與 TextNode.speakerName 對應（大小寫可視為相同）。")]
    //         public string name;
    //
    //         [Header("Portrait Defaults")]
    //         [Tooltip("預設的角色立繪 Sprite（可為空）。")]
    //         public Sprite defaultSprite;
    //
    //         [Tooltip("預設的 Spine 立繪設定（包含 Skeleton、動畫名稱等）。")]
    //         public SpinePortraitConfig defaultSpineConfig;
    //
    //         [Tooltip("僅指定 Skeleton 時使用的預設 SkeletonDataAsset（可覆蓋 defaultSpineConfig.skeletonData）。")]
    //         public SkeletonDataAsset defaultSkeleton;
    //     }
    //
    //     [Tooltip("角色預設設定的列表（可依角色名稱查詢）。")]
    //     public List<CharacterEntry> characters = new List<CharacterEntry>();
    //
    //     /// <summary>
    //     /// 根據角色名稱尋找角色的預設設定。此比較不區分大小寫。
    //     /// </summary>
    //     /// <param name="speakerName">要尋找的角色名稱。</param>
    //     /// <returns>對應的 CharacterEntry，如果找不到則為 null。</returns>
    //     public CharacterEntry FindByName(string speakerName)
    //     {
    //         if (string.IsNullOrEmpty(speakerName) || characters == null || characters.Count == 0)
    //         {
    //             return null;
    //         }
    //
    //         for (int i = 0; i < characters.Count; i++)
    //         {
    //             var entry = characters[i];
    //             if (entry == null || string.IsNullOrEmpty(entry.name))
    //             {
    //                 continue;
    //             }
    //
    //             if (string.Equals(entry.name, speakerName, StringComparison.OrdinalIgnoreCase))
    //             {
    //                 return entry;
    //             }
    //         }
    //
    //         return null;
    //     }
    // }
}
