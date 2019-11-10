using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public static class DialogueEditorUtil
    {
        public static List<ConversationNode> GetAllNodes(ConversationAction root)
        {
            List<ConversationNode> nodes = new List<ConversationNode>();
            nodes.Add(root);

            AddChildrenToList(root, ref nodes);

            return nodes;
        }

        private static void AddChildrenToList(ConversationNode node, ref List<ConversationNode> nodes)
        {
            if (node is ConversationAction)
            {
                ConversationAction action = node as ConversationAction;
                if (action.Options != null)
                {
                    for (int i = 0; i < action.Options.Count; i++)
                    {
                        nodes.Add(action.Options[i]);
                        AddChildrenToList(action.Options[i], ref nodes);
                    }
                }
            }
            else
            {
                ConversationOption option = node as ConversationOption;
                if (option.Action != null)
                {
                    // Two options can be pointing to the same action. 
                    // Ensure that the list of nodes does not already contain this action
                    ConversationAction existingAction;
                    if (!Contains(nodes, option.Action, out existingAction)) 
                    {
                        
                        nodes.Add(option.Action);
                        AddChildrenToList(option.Action, ref nodes);
                    }
                    // If it does, ensure the option points to the correct action
                    else
                    {
                        option.Action = existingAction;
                    }
                }
            }
        }

        public static bool Contains(List<ConversationNode> nodes, ConversationAction action, out ConversationAction duplicate)
        {
            ConversationAction loopAction;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is ConversationAction)
                {
                    loopAction = nodes[i] as ConversationAction;

                    if (loopAction.Text == action.Text &&
                        loopAction.EditorInfo.xPos == action.EditorInfo.xPos &&
                        loopAction.EditorInfo.yPos == action.EditorInfo.yPos)
                    {
                        duplicate = loopAction;
                        return true;
                    }
                        
                }
            }
            duplicate = null;
            return false;
        }

        public static bool IsPointerNearConnection(List<UINode> uiNodes)
        {
            return false;

            for (int i = 0; i < uiNodes.Count; i++)
            {
                if (uiNodes[i] is UIActionNode)
                {

                }
                else if (uiNodes[i] is UIOptionNode)
                {

                }
            }
        }

        // Translated into UnityC# from C++ 
        // Original Source: https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
        private static float MinimumDistanceBetweenPointAndLine(Vector2 v, Vector2 w, Vector2 p)
        {
            float lsqu = (v - w).sqrMagnitude;
            if (lsqu < 0.01f)
                return (p - v).magnitude;

            float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - v, w - v) / lsqu));
            Vector2 projection = v + t * (w - v);
            return (p - projection).magnitude;
        }

        public static void GetConnectionDrawInfo(Rect originRect, 
            ConversationNode connectionTarget, out Vector2 start, out Vector2 end)
        {
            float offset = 12;

            Vector2 origin = new Vector2(originRect.x + originRect.width / 2, originRect.y + originRect.height / 2);
            Vector2 target;

            if (connectionTarget is ConversationAction)
            {
                target = new Vector2(
                    connectionTarget.EditorInfo.xPos + UIActionNode.Width / 2,
                    connectionTarget.EditorInfo.yPos + UIActionNode.Height / 2);

                origin.x -= offset;
                target.x -= offset;
            }
            else
            {
                target = new Vector2(
                    connectionTarget.EditorInfo.xPos + UIOptionNode.Width / 2,
                    connectionTarget.EditorInfo.yPos + UIOptionNode.Height / 2);

                origin.x += offset;
                target.x += offset;
            }

            start = origin;
            end = target;
        }

        public static Color Colour(float r, float g, float b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1);
        }

        public static void RemoveChildFromNode(ConversationNode parent, ConversationNode child)
        {

        }
    }
}