using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public abstract class UINode
    {
        // Events
        public delegate void UINodeSelectedEvent(UINode node, bool selected);
        public static UINodeSelectedEvent OnUINodeSelected;

        // Members
        public Rect rect;
        public string title;
        public bool isDragged;
        public bool isSelected;
        public GUIStyle style;
        public GUIStyle defaultNodeStyle;
        public GUIStyle selectedNodeStyle;

        // Properties
        public NPCNode Info { get; protected set; }


        // Constructor and creation logic
        public UINode(NPCNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
        {
            Info = infoNode;
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


        // Generic methods, called from window
        public void SetPosition(Vector2 newPos)
        {
            Vector2 centeredPos = new Vector2(newPos.x - rect.width / 2, newPos.y - rect.height / 2);
            rect.position = centeredPos;
            Info.uiX = centeredPos.x;
            Info.uiY = centeredPos.y;
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

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                isDragged = true;
                isSelected = true;
                style = selectedNodeStyle;
                DialogueEditorWindow.NodeClickedOnThisUpdate = true;
            }
            else
            {
                isSelected = false;
                style = defaultNodeStyle;
            }
        }

        public bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition) && !isSelected)
                        {
                            OnUINodeSelected?.Invoke(this, true);
                        }

                        GUI.changed = true;
                    }
                    else if (e.button == 1 && rect.Contains(e.mousePosition))
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

        // Abstract methods
        public abstract void OnDraw();
        public abstract void DrawConnection();
        public abstract void DeleteSelf();
        protected abstract void ProcessContextMenu();
    }



    //--------------------------------------
    // Action Node
    //--------------------------------------

    public class UIActionNode : UINode
    {
        public delegate void CreateOptionEvent(UIActionNode node);
        public static CreateOptionEvent OnCreateOption;

        public static int Width { get { return 200; } }
        public static int Height { get { return 100; } }

        public NPCActionNode NodeInfo { get { return Info as NPCActionNode; } }


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

            GUI.Label(rect, "Type: " + NodeInfo.ActionType.ToString());
            rect.y += 25;

            GUI.Box(rect, NodeInfo.ActionValue);
        }

        public override void DrawConnection()
        {
            if (NodeInfo.Options != null)
            {
                Vector2 boxCenter = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

                for (int i = 0; i < NodeInfo.Options.Count; i++)
                {
                    NPCOptionNode option = NodeInfo.Options[i];

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

        public override void DeleteSelf()
        {
            // The parent node no longer points to this as its child
            (NodeInfo.parent as NPCOptionNode).Action = null;

            // This no longer has a parent
            NodeInfo.parent = null;

            // My children no longer point to me as parent
            foreach (var v in NodeInfo.Options)
            {
                v.parent = null;
            }

            // Clear my child nodes
            NodeInfo.Options.Clear();
        }

        protected override void ProcessContextMenu()
        {
            GenericMenu rightClickMenu = new GenericMenu();
            rightClickMenu.AddItem(new GUIContent("Create New Option"), false, CreateNewOption);
            rightClickMenu.AddItem(new GUIContent("Connect to option"), false, ConnectToOption);
            rightClickMenu.AddItem(new GUIContent("Delete this node"), false, DeleteThisNode);
            rightClickMenu.ShowAsContext();
        }

        private void CreateNewOption()
        {
            OnCreateOption?.Invoke(this);
        }

        private void ConnectToOption()
        {

        }

        private void DeleteThisNode()
        {

        }


    }




    //--------------------------------------
    // OptionNode
    //--------------------------------------

    public class UIOptionNode : UINode
    {
        public delegate void CreateActionEvent(UIOptionNode node);
        public static CreateActionEvent OnCreateAction;

        public static int Width { get { return 200; } }
        public static int Height { get { return 50; } }

        public NPCOptionNode NodeInfo { get { return Info as NPCOptionNode; } }


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

            GUI.Box(rect, NodeInfo.Value);
        }

        public override void DrawConnection()
        {
            if (NodeInfo.Action != null && !NodeInfo.Action.IsEffectivelyNull())
            {
                Vector2 boxCenter = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

                NPCActionNode action = NodeInfo.Action;
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

        public override void DeleteSelf()
        {
            // This node is no longer an option of its parent
            (NodeInfo.parent as NPCActionNode).Options.Remove(NodeInfo);

            // This no longer has a parent
            NodeInfo.parent = null;

            // My child no longer points to me as parent
            NodeInfo.Action.parent = null;

            // Clear my child node
            NodeInfo.Action = null;
        }

        protected override void ProcessContextMenu()
        {
            GenericMenu rightClickMenu = new GenericMenu();
            rightClickMenu.AddItem(new GUIContent("Create Action"), false, CreateAction);
            rightClickMenu.AddItem(new GUIContent("Connect to action"), false, ConnectToAction);
            rightClickMenu.AddItem(new GUIContent("Delete this node"), false, DeleteThisNode);
            rightClickMenu.ShowAsContext();
        }

        private void CreateAction()
        {
            OnCreateAction?.Invoke(this);
        }

        private void ConnectToAction()
        {

        }

        private void DeleteThisNode()
        {

        }
    }
}