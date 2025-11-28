using System;
using System.Collections;
using System.Collections.Generic;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    /// <summary>
    /// CameraControlNode 是一個攝影機控制節點，專門用於在對話流程中控制鏡頭的行為。
    /// 它支援攝影機震動、縮放、平移和聚焦於特定目標，以增強演出的動感和表現力。
    /// </summary>
    [Serializable]
    public class CameraControlNode : DialogueNodeBase
    {
        [Header("動作設定")]
        [Tooltip("此節點要執行的攝影機動作類型。")]
        public CameraActionType ActionType = CameraActionType.Shake;

        [Tooltip("攝影機動作的持續時間（秒）。")]
        public float Duration = 0.5f;

        [Header("震動設定")]
        [Tooltip("攝影機震動的強度。")]
        public float ShakeIntensity = 1f;
        [Tooltip("攝影機震動的頻率（每秒震動次數）。")]
        public int ShakeVibrato = 10;

        [Header("縮放設定")]
        [Tooltip("目標的 Orthographic Size（正交攝影機的視野大小）。數值越小，鏡頭越近。")]
        public float TargetZoom = 4f;

        [Header("平移設定")]
        [Tooltip("攝影機要平移到的世界空間目標位置。")]
        public Vector2 PanTargetPosition;

        [Header("聚焦設定")]
        [Tooltip("攝影機要聚焦的目標 Transform。需要從場景中指定。")]
        public Transform FocusTarget;

        [Header("流程控制")]
        [Tooltip("此節點執行完畢後，要前往的下一個節點 ID。")]
        public string nextNodeId;

        /// <summary>
        /// 處理攝影機控制節點的核心邏輯。
        /// 它會呼叫攝影機控制器來執行指定的攝影機動作，並等待其完成。
        /// </summary>
        /// <param name="controller">對話總控制器。</param>
        /// <returns>一個協程迭代器，用於等待攝影機動作完成。</returns>
        public override IEnumerator Process(DialogueController controller)
        {
            // 檢查 DialogueController 上是否有指定的攝影機控制器
            if (controller.CameraController != null)
            {
                // 呼叫攝影機控制器來執行這個動作，並等待其完成
                yield return controller.CameraController.Execute(this);
            }
            else
            {
                Debug.LogWarning("攝影機控制節點需要一個 DialogueCameraController 指派在 DialogueController 上才能運作。");
            }
        }

        /// <summary>
        /// 覆寫基底類別的方法，返回此攝影機控制節點的下一個節點 ID。
        /// </summary>
        /// <returns>下一個節點的 ID。</returns>
        public override string GetNextNodeId()
        {
            return nextNodeId;
        }
    }
}
