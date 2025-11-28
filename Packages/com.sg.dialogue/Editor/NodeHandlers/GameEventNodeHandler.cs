#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class GameEventNodeHandler : INodeHandler
    {
        public string MenuName => "Events/Trigger Game Event Node";
        public DialogueNodeBase CreateNodeData() => new GameEventNode();
        public string GetPrefix() => "EVENT";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new GameEventNodeElement(node as GameEventNode, nodeProperty, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var geNode = nodeData as GameEventNode;
            var geElem = sourceView as GameEventNodeElement;
            connect(geElem.OutputPort, getInputPort(geNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is GameEventNodeElement geElem)
            {
                // GameEventNodeElement 只有一個輸出埠，通常命名為 "Next"
                return geElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
