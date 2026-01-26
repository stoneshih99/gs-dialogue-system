using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue.Nodes
{
    [Serializable]
    public class ParticleNode : DialogueNodeBase
    {
        [Header("事件通道")]
        [Tooltip("用於發送粒子請求的事件通道")]
        public ParticleEvent ParticleEvent;

        [Header("粒子設定")]
        [Tooltip("動作類型：播放或停止")]
        public ParticleActionType ActionType;

        [Tooltip("粒子 ID (用於識別和停止特定粒子)")]
        public string ParticleID;

        [Tooltip("要播放的粒子 Prefab (僅 Play 模式需要)")]
        public GameObject ParticlePrefab;

        [Header("變形設定")]
        public Vector3 Position;
        public Vector3 Scale = Vector3.one;

        [Header("流程控制")]
        [Tooltip("是否等待使用者輸入才進入下一個節點")]
        public bool WaitForInput = false;
        public string nextNodeId;

        public override async UniTask Process(DialogueController controller, CancellationToken ct = default)
        {
            if (ParticleEvent != null)
            {
                var request = new ParticleRequest(ActionType, ParticleID, ParticlePrefab, Position, Scale);
                ParticleEvent.Raise(request);
            }
            else
            {
                Debug.LogWarning($"ParticleNode '{nodeId}' 缺少 ParticleEvent 引用。");
            }

            if (WaitForInput)
            {
                await controller.WaitForInputAsync();
            }
            else
            {
                await UniTask.Yield();
            }
        }

        public override string GetNextNodeId() => nextNodeId;
        public override void ClearConnectionsForClipboard() => nextNodeId = null;
        public override void ClearUnityReferencesForClipboard()
        {
            ParticlePrefab = null;
            ParticleEvent = null;
        }
    }
}
