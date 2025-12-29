#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Dialogue.Editor;
using SG.Dialogue.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using SG.Dialogue.Editor.Editor.GraphElements;

namespace SG.Dialogue.Editor.Editor.NodeHandlers
{
    public class ParticleNodeHandler : INodeHandler
    {
        public string MenuName => "Visual/Particle Effect";

        public DialogueNodeBase CreateNodeData()
        {
            return new ParticleNode();
        }

        public DialogueNodeElement CreateNodeElement(DialogueNodeBase node, DialogueGraphView graphView, SerializedProperty nodeProperty, Action onChanged)
        {
            return new ParticleNodeElement((ParticleNode)node, nodeProperty, onChanged);
        }

        public string GetPrefix()
        {
            return "Particle";
        }

        public void ConnectPorts(DialogueNodeElement sourceView, DialogueNodeBase nodeData, Func<string, Port> getInputPort, Action<Port, Port> connect)
        {
            if (sourceView is ParticleNodeElement particleView && nodeData is ParticleNode particleNode)
            {
                if (!string.IsNullOrEmpty(particleNode.nextNodeId))
                {
                    var targetPort = getInputPort(particleNode.nextNodeId);
                    if (targetPort != null)
                    {
                        connect(particleView.OutputPort, targetPort);
                    }
                }
            }
        }

        public Port GetOutputPort(DialogueNodeElement element, string portName)
        {
            if (element is ParticleNodeElement particleView && portName == "Next")
            {
                return particleView.OutputPort;
            }
            return null;
        }
    }
}
#endif
