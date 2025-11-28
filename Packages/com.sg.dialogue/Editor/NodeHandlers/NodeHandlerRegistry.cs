#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SG.Dialogue.Nodes;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// 一個靜態註冊表，用於在編輯器啟動時發現並儲存所有的 INodeHandler 實現。
    /// </summary>
    public static class NodeHandlerRegistry
    {
        public static readonly Dictionary<Type, INodeHandler> Handlers = new Dictionary<Type, INodeHandler>();

        static NodeHandlerRegistry()
        {
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(INodeHandler).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var type in handlerTypes)
            {
                var handler = (INodeHandler)Activator.CreateInstance(type);
                // 假設 Handler 的命名規則是 "NodeTypeName" + "Handler"
                string nodeTypeName = type.Name.Replace("Handler", "");
                var nodeType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .FirstOrDefault(t => t.Name == nodeTypeName && typeof(DialogueNodeBase).IsAssignableFrom(t));
                
                if (nodeType != null)
                {
                    Handlers[nodeType] = handler;
                }
            }
        }
    }
}
#endif
