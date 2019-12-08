using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public static class DialogueEditorUtil
    {
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

        public static Color Colour(float r, float g, float b, float a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        public static Texture2D MakeTexture(int width, int height, Color col)
        {
            Texture2D t2d = new Texture2D(width, height);
            for (int x = 0; x < width - 1; x++)
            {
                for (int y = 0; y < height - 1; y++)
                {
                    if (y > height - 20)
                    {
                        t2d.SetPixel(x, y, col);
                    }
                    else
                    {
                        t2d.SetPixel(x, y, Color.black);
                    }                  
                }
            }
            t2d.Apply();
            return t2d;
        }
    }
}