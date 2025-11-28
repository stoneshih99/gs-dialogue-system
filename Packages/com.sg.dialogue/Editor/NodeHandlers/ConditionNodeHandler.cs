#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ConditionNodeHandler : INodeHandler
    {
        public string MenuName => "Flow/Condition Node";
        public DialogueNodeBase CreateNodeData() => new ConditionNode();
        public string GetPrefix() => "IF";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ConditionNodeElement(node as ConditionNode, onChanged, graphView.GlobalState);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var condNode = nodeData as ConditionNode;
            var condElem = sourceView as ConditionNodeElement;
            connect(condElem.TrueOutputPort, getInputPort(condNode.TrueNextNodeId));
            connect(condElem.FalseOutputPort, getInputPort(condNode.FalseNextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ConditionNodeElement condElem)
            {
                if (portName == "True") return condElem.TrueOutputPort;
                if (portName == "False") return condElem.FalseOutputPort;
            }
            return null;
        }
    }
}
#endif
