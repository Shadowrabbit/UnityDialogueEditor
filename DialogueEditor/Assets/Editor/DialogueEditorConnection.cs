using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public enum ConnectionPointType { In, Out }

    public delegate void ClickConnectionPointEvent(ConnectionPoint p);
    public delegate void ClickRemoveConnection(Connection c);

    public class ConnectionPoint
    {
        public Rect rect;

        public ConnectionPointType type;

        public UINode node;

        public GUIStyle style;
      
        public ClickConnectionPointEvent OnClickConnectionPoint;

        public ConnectionPoint(UINode node, ConnectionPointType type, GUIStyle style, ClickConnectionPointEvent clickConnectionPoint)
        {
            this.node = node;
            this.type = type;
            this.style = style;
            this.OnClickConnectionPoint = clickConnectionPoint;
            rect = new Rect(0, 0, 10f, 20f);
        }

        public void Draw()
        {
            rect.y = node.rect.y + (node.rect.height * 0.5f) - rect.height * 0.5f;

            switch (type)
            {
                case ConnectionPointType.In:
                    rect.x = node.rect.x - rect.width + 8f;
                    break;

                case ConnectionPointType.Out:
                    rect.x = node.rect.x + node.rect.width - 8f;
                    break;
            }

            if (GUI.Button(rect, "", style))
            {
                OnClickConnectionPoint?.Invoke(this);
            }
        }
    }

    public class Connection
    {
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;

        public ClickRemoveConnection OnClickConnectionPoint;

        public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint, ClickRemoveConnection onRemoveConnection)
        {
            this.inPoint = inPoint;
            this.outPoint = outPoint;
            this.OnClickConnectionPoint = onRemoveConnection;
        }

        public void Draw()
        {
            Handles.DrawBezier(
                inPoint.rect.center,                        /* start pos */
                outPoint.rect.center,                       /* end pos */
                inPoint.rect.center + Vector2.left * 50f,   /* start tangent */
                outPoint.rect.center - Vector2.left * 50f,  /* end tangent */
                Color.white,                                /* color */
                null,                                       /* tex */
                2f                                          /* width */
            );

            if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleCap))
            {
                OnClickConnectionPoint?.Invoke(this);
            }
        }
    }
}