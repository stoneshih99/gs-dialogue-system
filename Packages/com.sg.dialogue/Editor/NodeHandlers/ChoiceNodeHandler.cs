#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ChoiceNodeHandler : INodeHandler
    {
        public string MenuName => "Flow/Choice Node";
        public DialogueNodeBase CreateNodeData() => new ChoiceNode { choices = new List<DialogueChoice> { new DialogueChoice() } };
        public string GetPrefix() => "CHOICE";

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ChoiceNodeElement(node as ChoiceNode, onChanged);
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            var choiceNode = nodeData as ChoiceNode;
            var choiceElem = sourceView as ChoiceNodeElement;
            for (int i = 0; i < choiceNode.choices.Count; i++)
            {
                connect(choiceElem.GetChoicePort(i), getInputPort(choiceNode.choices[i].nextNodeId));
            }
        }

        /// <summary>
        /// 根據埠名稱獲取節點的輸出埠。
        /// </summary>
        /// <param name="element">節點的視覺元素。</param>
        /// <param name="portName">埠的名稱。</param>
        /// <returns>對應的輸出埠，如果找不到則為 null。</returns>
        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ChoiceNodeElement choiceElem)
            {
                // ChoiceNodeElement 有多個輸出埠，根據 portName 判斷
                // portName 的格式通常是 "Choice X"
                if (portName.StartsWith("Choice "))
                {
                    if (int.TryParse(portName.Replace("Choice ", ""), out int index))
                    {
                        // 由於 portName 是從 1 開始的，而 GetChoicePort 是從 0 開始的索引
                        return choiceElem.GetChoicePort(index - 1);
                    }
                }
            }
            return null;
        }
    }
}
#endif
