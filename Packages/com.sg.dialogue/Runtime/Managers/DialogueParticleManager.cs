using System.Collections.Generic;
using SG.Dialogue.Events;
using UnityEngine;

namespace SG.Dialogue.Managers
{
    /// <summary>
    /// 管理對話系統中的粒子特效。
    /// 它使用獨立的 Camera 將 2D 粒子渲染到 RenderTexture 上，以便在 UI 中顯示。
    /// </summary>
    public class DialogueParticleManager : MonoBehaviour
    {
        [Header("事件監聽")]
        [Tooltip("要監聽的粒子事件通道")]
        [SerializeField] private ParticleEvent particleEvent;

        [Header("渲染設定")]
        [Tooltip("專門用於渲染粒子的攝影機")]
        [SerializeField] private Camera particleCamera;
        
        [Tooltip("粒子生成的根節點（建議位於攝影機前方）")]
        [SerializeField] private Transform particleRoot;

        [Tooltip("粒子所在的 Layer，必須與 Camera 的 Culling Mask 一致")]
        [SerializeField] private LayerMask particleLayer;

        // 用於追蹤運行中的粒子實例 (Key: ParticleID, Value: GameObject)
        private readonly Dictionary<string, GameObject> _activeParticles = new Dictionary<string, GameObject>();

        private void OnEnable()
        {
            if (particleEvent != null) particleEvent.RegisterListener(OnParticleRequest);
        }

        private void OnDisable()
        {
            if (particleEvent != null) particleEvent.UnregisterListener(OnParticleRequest);
        }

        private void OnParticleRequest(ParticleRequest request)
        {
            if (request.ActionType == Enums.ParticleActionType.Play)
            {
                PlayParticle(request.ParticleID, request.Prefab, request.Position, request.Scale);
            }
            else
            {
                StopParticle(request.ParticleID);
            }
        }

        private void PlayParticle(string id, GameObject prefab, Vector3 position, Vector3 scale)
        {
            // 如果已有同名粒子，先停止舊的
            StopParticle(id);

            if (prefab == null) return;

            // 生成粒子
            GameObject instance = Instantiate(prefab, particleRoot);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
            
            // 設定 Layer
            int layerIndex = 0;
            int layerMask = particleLayer.value;
            while (layerMask > 0)
            {
                if ((layerMask & 1) == 1) break;
                layerMask >>= 1;
                layerIndex++;
            }
            
            SetLayerRecursively(instance, layerIndex);

            _activeParticles[id] = instance;
        }

        private void StopParticle(string id)
        {
            if (_activeParticles.TryGetValue(id, out GameObject instance))
            {
                if (instance != null) Destroy(instance);
                _activeParticles.Remove(id);
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
