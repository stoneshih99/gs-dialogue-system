#if UNITY_EDITOR
using System;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class PlayAudioNodeHandler : INodeHandler
    {
        public string MenuName => "Events/Play Audio Node";
        public DialogueNodeBase CreateNodeData() => new PlayAudioNode();
        public string GetPrefix() => "AUDIO";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new PlayAudioNodeElement(node as PlayAudioNode, nodeProperty, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var paNode = nodeData as PlayAudioNode;
            var paElem = sourceView as PlayAudioNodeElement;
            connect(paElem.OutputPort, getInputPort(paNode.nextNodeId));
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is PlayAudioNodeElement paElem)
            {
                // PlayAudioNodeElement 只有一個輸出埠，通常命名為 "Next"
                return paElem.OutputPort;
            }
            return null;
        }
    }
}
#endif
