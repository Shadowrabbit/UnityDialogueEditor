using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public abstract class UINode
    {
        public NPCNode Info { get { return info; } }
        protected NPCNode info;

        public Rect rect;
        public string title;
        public bool isDragged;
        public bool isSelected;

        public GUIStyle style;
        public GUIStyle defaultNodeStyle;
        public GUIStyle selectedNodeStyle;

        public delegate void NodeRemovedEvent(UINode node);
        public static NodeRemovedEvent OnNodeRemoved;

        public delegate void OptionAddedEvent(UIActionNode node);
        public static OptionAddedEvent OnOptionAdded;

        public static UINode SelectedNode { get; set; }

        public UINode(NPCNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
        {
            info = infoNode;
            style = defaultStyle;
            defaultNodeStyle = defaultStyle;
            selectedNodeStyle = selectedStyle;
        }

        protected void CreateRect(Vector2 pos, float wid, float hei)
        {
            rect = new Rect(pos.x, pos.y, wid, hei);
            Info.uiX = rect.x;
            Info.uiY = rect.y;
        }

        public void Drag(Vector2 moveDelta)
        {
            rect.position += moveDelta;
            Info.uiX = rect.x;
            Info.uiY = rect.y;
        }

        public void Draw()
        {
            GUI.Box(rect, title, style);
            OnDraw();
        }

        public abstract void OnDraw();
        public abstract void DrawConnection();

        public bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            isDragged = true;
                            isSelected = true;
                            SelectedNode = this;
                            style = selectedNodeStyle;
                            DialogueEditorWindow.NodeClickedOnThisUpdate = true;
                        }
                        else
                        {
                            isSelected = false;
                            style = defaultNodeStyle;
                        }

                        GUI.changed = true;
                    }

                    else if (e.button == 1 && rect.Contains(e.mousePosition) && this is UIActionNode)
                    {
                        ProcessContextMenu();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    isDragged = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isDragged)
                    {
                        Drag(e.delta);
                        e.Use();
                    }
                    return true;
            }

            return false;
        }

        private void ProcessContextMenu()
        {
            GenericMenu rightClickMenu = new GenericMenu();
            rightClickMenu.AddItem(new GUIContent("Delete node"), false, OnClickRemoveNode);

            if (this is UIActionNode)
            {
                rightClickMenu.AddItem(new GUIContent("Add new option"), false, OnClickAddOption);
            }
            
            rightClickMenu.ShowAsContext();
        }

        private void OnClickRemoveNode()
        {
            OnNodeRemoved?.Invoke(this);
        }

        private void OnClickAddOption()
        {
            OnOptionAdded?.Invoke(this as UIActionNode);
        }
    }

    public class UIActionNode : UINode
    {
        public NPCActionNode DeInfo { get { return info as NPCActionNode; } }

        public static int Width { get { return 200; } }
        public static int Height { get { return 100; } }

        public UIActionNode(NPCNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
            : base(infoNode, pos, defaultStyle, selectedStyle)
        {
            CreateRect(pos, Width, Height);
        }

        public override void OnDraw()
        {
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            // Title
            Rect rect = new Rect(base.rect.x, base.rect.y, base.rect.width, 25);
            GUI.Label(rect, "Action node", titleStyle);
            rect.y += 25;

            // Properties
            int border = 5;
            rect.x += border;
            rect.width -= border * 2;

            GUI.Label(rect, "Type: " + DeInfo.ActionType.ToString());
            rect.y += 25;

            GUI.Box(rect, DeInfo.ActionValue);
        }

        public override void DrawConnection()
        {
            Vector2 boxCenter = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

            if (DeInfo.Options != null)
            {
                for (int i = 0; i < DeInfo.Options.Count; i++)
                {
                    NPCOptionNode option = DeInfo.Options[i];

                    Vector2 optionCenter = new Vector2(
                        option.uiX + UIOptionNode.Width / 2,
                        option.uiY + UIOptionNode.Height / 2);

                    Vector2 toOption = (boxCenter - optionCenter).normalized;
                    Vector2 toAction = (optionCenter - boxCenter).normalized;

                    Handles.DrawBezier(boxCenter, optionCenter,
                        boxCenter + toAction * 50f, optionCenter + toOption * 50f,
                        Color.black, null, 5f);
                }
            }
        }
    }

    public class UIOptionNode : UINode
    {
        public NPCOptionNode DeInfo { get { return info as NPCOptionNode; } }

        public static int Width { get { return 200; } }
        public static int Height { get { return 50; } }

        public UIOptionNode(NPCNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
            : base(infoNode, pos, defaultStyle, selectedStyle)
        {
            CreateRect(pos, Width, Height);
        }

        public override void OnDraw()
        {
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            // Title
            Rect rect = new Rect(base.rect.x, base.rect.y, base.rect.width, 25);
            GUI.Label(rect, "Option node", titleStyle);
            rect.y += 25;

            // Properties
            int border = 5;
            rect.x += border;
            rect.width -= border * 2;

            GUI.Box(rect, DeInfo.Value);
        }

        public override void DrawConnection()
        {
            Vector2 boxCenter = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

            NPCActionNode action = DeInfo.Action;
            Vector2 actionCenter = new Vector2(
                action.uiX + UIActionNode.Width / 2,
                action.uiY + UIActionNode.Height / 2);

            Vector2 toOption = (boxCenter - actionCenter).normalized;
            Vector2 toAction = (actionCenter - boxCenter).normalized;

            Handles.DrawBezier(boxCenter, actionCenter,
                boxCenter + toAction * 50f, actionCenter + toOption * 50f,
                Color.black, null, 5f);
        }
    }
}