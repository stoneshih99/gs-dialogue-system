using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SG.Dialogue.Core.Instructions;
using SG.Dialogue.Enums;
using SG.Dialogue.Events;
using SG.Dialogue.Resource;
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

        [Tooltip("要播放的粒子 Prefab (僅 Play 模式需要)。（若設定了 resourceKey，此欄位將被忽略）")]
        public GameObject ParticlePrefab;
        [Tooltip("粒子 Prefab 的資源 Key（用於透過 DialogueResourceBridge 延遲載入）。")]
        public string ParticlePrefabKey;

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
                // 透過 Bridge 延遲載入粒子 Prefab，或 fallback 到直接引用
                GameObject prefab = ParticlePrefab;
                if (!string.IsNullOrEmpty(ParticlePrefabKey) && DialogueResourceBridge.HasProvider)
                {
                    var loaded = await DialogueResourceBridge.LoadAsync<GameObject>(ParticlePrefabKey);
                    if (loaded != null) prefab = loaded;
                    else Debug.LogWarning($"[ParticleNode] 無法透過 Bridge 載入資源 '{ParticlePrefabKey}'，嘗試使用直接引用。");
                }

                var request = new ParticleRequest(ActionType, ParticleID, prefab, Position, Scale);
                ParticleEvent.Raise(request);
            }
            else
            {
                Debug.LogWarning($"ParticleNode '{nodeId}' 缺少 ParticleEvent 引用。");
            }

            if (WaitForInput)
            {
                await controller.WaitForInputAsync(ct);
            }
            else
            {
                await UniTask.Yield(ct);
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
