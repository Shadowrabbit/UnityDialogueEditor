using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public static class DialogueEditorUtil
    {
        public static bool ContainsAction(List<ConversationNode> nodes, ConversationAction action, out ConversationAction duplicate)
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

        public static bool ContainsOption(List<ConversationNode> nodes, ConversationOption option, out ConversationOption duplicate)
        {
            ConversationOption loopAction;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is ConversationOption)
                {
                    loopAction = nodes[i] as ConversationOption;

                    if (loopAction.Text == option.Text &&
                        loopAction.EditorInfo.xPos == option.EditorInfo.xPos &&
                        loopAction.EditorInfo.yPos == option.EditorInfo.yPos)
                    {
                        duplicate = loopAction;
                        return true;
                    }

                }
            }
            duplicate = null;
            return false;
        }

        public static bool IsPointerNearConnection(List<UINode> uiNodes, Vector2 mousePos, 
            out ConversationNode par, out ConversationNode child)
        {
            par = null;
            child = null;
            UIActionNode action;
            UIOptionNode option;
            Vector2 start, end;           
            float minDistance = float.MaxValue;
            const float MIN_DIST = 6;

            for (int i = 0; i < uiNodes.Count; i++)
            {
                if (uiNodes[i] is UIActionNode)
                {
                    action = uiNodes[i] as UIActionNode;

                    if (action.ConversationNode.Options != null)
                    {
                        for (int j = 0; j < action.ConversationNode.Options.Count; j++)
                        {
                            GetConnectionDrawInfo(action.rect, action.ConversationNode.Options[j], out start, out end);

                            float distance = MinimumDistanceBetweenPointAndLine(start, end, mousePos);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                par = action.ConversationNode;
                                child = action.ConversationNode.Options[j];
                            }
                        }
                    }
                }
                else if (uiNodes[i] is UIOptionNode)
                {
                    option = uiNodes[i] as UIOptionNode;

                    if (option.OptionNode.Action != null)
                    {
                        GetConnectionDrawInfo(option.rect, option.OptionNode.Action, out start, out end);

                        float distance = MinimumDistanceBetweenPointAndLine(start, end, mousePos);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            par = option.OptionNode;
                            child = option.OptionNode.Action;
                        }
                    }
                }
            }

            if (minDistance < MIN_DIST)
            {
                return true;
            }
            else
            {
                par = null;
                child = null;
                return false;
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