#if UNITY_EDITOR
using System;
using SG.Dialogue.Editor.Editor.GraphElements;
using SG.Dialogue.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SG.Dialogue.Editor.Dialogue.Editor
{
    /// <summary>
    /// GraphConnectionHandler 負責處理 DialogueGraphView 中的邊緣連接和斷開邏輯。
    /// </summary>
    public class GraphConnectionHandler
    {
        private DialogueGraphView _graphView;

        public GraphConnectionHandler(DialogueGraphView graphView)
        {
            _graphView = graphView;
        }

        /// <summary>
        /// 處理邊緣的連接。
        /// </summary>
        /// <param name="edge">要連接的邊緣。</param>
        /// <returns>是否成功處理。</returns>
        public bool HandleEdgeConnection(Edge edge)
        {
            if (HandleSubGraphStartEdge(edge))
            {
                _graphView.RecordUndo("Connect Start Node");
                return true;
            }
            else
            {
                var sourceNodeElement = edge.output?.node as DialogueNodeElement;
                var targetNodeElement = edge.input?.node as DialogueNodeElement;
                if (sourceNodeElement != null && targetNodeElement != null)
                {
                    _graphView.RecordUndo("Connect Edge");
                    sourceNodeElement.OnOutputPortConnected(edge.output, targetNodeElement.NodeId);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 處理邊緣的斷開。
        /// </summary>
        /// <param name="edge">要斷開的邊緣。</param>
        /// <returns>是否成功處理。</returns>
        public bool HandleEdgeDisconnection(Edge edge)
        {
            if (HandleSubGraphStartEdge(edge, true))
            {
                _graphView.RecordUndo("Disconnect Start Node");
                return true;
            }
            else
            {
                var sourceNodeElement = edge.output?.node as DialogueNodeElement;
                if (sourceNodeElement != null)
                {
                    _graphView.RecordUndo("Disconnect Edge");
                    sourceNodeElement.OnOutputPortDisconnected(edge.output);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 處理子圖起始節點的邊緣連接/斷開。
        /// </summary>
        private bool HandleSubGraphStartEdge(Edge edge, bool isDeletion = false)
        {
            var container = _graphView.NavigationStack.Peek();
            if (container is SequenceNode seqNode && edge.output?.node == _graphView.SequenceStartNode)
            {
                var targetNodeId = isDeletion ? null : (edge.input?.node as DialogueNodeElement)?.NodeId;
                seqNode.startNodeId = targetNodeId;
                return true;
            }
            if (container is ParallelNode parNode && edge.output?.node == _graphView.ParallelStartNode)
            {
                int portIndex = _graphView.ParallelStartNode.BranchPorts.IndexOf(edge.output);
                if (portIndex != -1)
                {
                    var targetNodeId = isDeletion ? null : (edge.input?.node as DialogueNodeElement)?.NodeId;
                    if (portIndex < parNode.branchStartNodeIds.Count)
                    {
                        parNode.branchStartNodeIds[portIndex] = targetNodeId;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
#endif
