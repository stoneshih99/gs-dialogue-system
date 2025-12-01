#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Editor.Editor.NodeHandlers;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;


namespace SG.Dialogue.Editor.Dialogue.Editor
{
    public class ChoiceNodeHandler : INodeHandler
    {
        public string MenuName => "Content/Choice";
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

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ChoiceNodeElement choiceElem)
            {
                if (portName.StartsWith("Choice "))
                {
                    if (int.TryParse(portName.Replace("Choice ", ""), out int index))
                    {
                        return choiceElem.GetChoicePort(index - 1);
                    }
                }
            }
            return null;
        }
    }
}
#endif
