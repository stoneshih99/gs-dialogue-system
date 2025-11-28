#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class FlashEffectNodeHandler : INodeHandler
    {
        public string MenuName => "Effects/Screen/Flash Effect Node";
        public DialogueNodeBase CreateNodeData() => new FlashEffectNode();
        public string GetPrefix() => "FLASH";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new FlashEffectNodeElement(node as FlashEffectNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var flashNode = nodeData as FlashEffectNode;
            var flashElem = sourceView as FlashEffectNodeElement;
            connect(flashElem.OutputPort, getInputPort(flashNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is FlashEffectNodeElement flashElem)
            {
                // FlashEffectNodeElement 只有一個輸出埠，通常命名為 "Next"
                return flashElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
