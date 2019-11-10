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

        public delegate void UINodeDeletedEvent(UINode node);
        public static UINodeDeletedEvent OnUINodeDeleted;

        // Members
        public Rect rect;
        public string title;
        public bool isDragged;
        public bool isSelected;

        protected GUIStyle currentBoxStyle;

        // Properties
        public ConversationNode Info { get; protected set; }


        // Constructor and creation logic
        public UINode(ConversationNode infoNode, Vector2 pos)
        {
            Info = infoNode;
        }

        protected void CreateRect(Vector2 pos, float wid, float hei)
        {
            rect = new Rect(pos.x, pos.y, wid, hei);
            Info.EditorInfo.xPos = rect.x;
            Info.EditorInfo.yPos = rect.y;
        }


        // Generic methods, called from window
        public void SetPosition(Vector2 newPos)
        {
            Vector2 centeredPos = new Vector2(newPos.x - rect.width / 2, newPos.y - rect.height / 2);
            rect.position = centeredPos;
            Info.EditorInfo.xPos = centeredPos.x;
            Info.EditorInfo.yPos = centeredPos.y;
        }

        public void Drag(Vector2 moveDelta)
        {
            rect.position += moveDelta;
            Info.EditorInfo.xPos = rect.x;
            Info.EditorInfo.yPos = rect.y;
        }

        public void Draw()
        {
            OnDraw();
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                isDragged = true;
                isSelected = true;
                DialogueEditorWindow.NodeClickedOnThisUpdate = true;
            }
            else
            {
                isSelected = false;
            }

            OnSetSelected(selected);
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
        public abstract void DrawConnections();
        protected abstract void ProcessContextMenu();
        protected abstract void OnSetSelected(bool selected);
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

        public ConversationAction ConversationNode { get { return Info as ConversationAction; } }

        protected static GUIStyle defaultNodeStyle;
        protected static GUIStyle selectedNodeStyle;

        public UIActionNode(ConversationNode infoNode, Vector2 pos)
            : base(infoNode, pos)
        {
            if (defaultNodeStyle == null)
            {
                defaultNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(173, 25, 15));
                t2d.Apply();
                defaultNodeStyle.normal.background = t2d;
            }
            if (selectedNodeStyle == null)
            {
                selectedNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(217, 36, 20));
                t2d.Apply();
                selectedNodeStyle.normal.background = t2d;
            }

            currentBoxStyle = defaultNodeStyle;

            CreateRect(pos, Width, Height);
        }

        public override void OnDraw()
        {
            // Box
            GUI.Box(rect, title, currentBoxStyle);

            // Internals 
            Rect internalText = new Rect(base.rect.x, base.rect.y, base.rect.width, 25);

            // Title
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
        
            GUI.Label(internalText, "Action node", titleStyle);
            internalText.y += 25;

            // Properties
            int border = 5;
            internalText.x += border;
            internalText.width -= border * 2;

            GUI.Box(internalText, ConversationNode.Text);
        }

        public override void DrawConnections()
        {
            if (ConversationNode.Options != null && ConversationNode.Options.Count > 0)
            {
                Vector2 start, end;
                for (int i = 0; i < ConversationNode.Options.Count; i++)
                {
                    DialogueEditorUtil.GetConnectionDrawInfo(rect, ConversationNode.Options[i], out start, out end);

                    Vector2 toOption = (start - end).normalized;
                    Vector2 toAction = (end - start).normalized;

                    Handles.DrawBezier(
                        start, end,
                        start + toAction * 50f,
                        end + toOption * 50f,
                        Color.red, null, 5f);
                }
            }

        }

        protected override void OnSetSelected(bool selected)
        {
            if (selected)
                currentBoxStyle = selectedNodeStyle;
            else
                currentBoxStyle = defaultNodeStyle;
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
            OnUINodeDeleted?.Invoke(this);
        }


    }




    //--------------------------------------
    // OptionNode
    //--------------------------------------

    public class UIOptionNode : UINode
    {
        public delegate void CreateActionEvent(UIOptionNode node);
        public static CreateActionEvent OnCreateAction;

        public delegate void ConnectToActionEvent(UIOptionNode node);
        public static ConnectToActionEvent OnConnectToAction;

        public static int Width { get { return 200; } }
        public static int Height { get { return 50; } }

        public ConversationOption OptionNode { get { return Info as ConversationOption; } }

        protected static GUIStyle defaultNodeStyle;
        protected static GUIStyle selectedNodeStyle;

        public UIOptionNode(ConversationNode infoNode, Vector2 pos)
            : base(infoNode, pos)
        {
            if (defaultNodeStyle == null)
            {
                defaultNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(20, 138, 254));
                t2d.Apply();
                defaultNodeStyle.normal.background = t2d;
            }
            if (selectedNodeStyle == null)
            {
                selectedNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(0, 188, 254));
                t2d.Apply();
                selectedNodeStyle.normal.background = t2d;
            }

            currentBoxStyle = defaultNodeStyle;

            CreateRect(pos, Width, Height);
        }

        public override void OnDraw()
        {
            // Box
            GUI.Box(rect, title, currentBoxStyle);

            // Internal rect
            Rect internalRect = new Rect(base.rect.x, base.rect.y, base.rect.width, 25);

            // Title
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;

            GUI.Label(internalRect, "Option node", titleStyle);
            internalRect.y += 25;

            // Properties
            int border = 5;
            internalRect.x += border;
            internalRect.width -= border * 2;

            GUI.Box(internalRect, OptionNode.Text);
        }

        public override void DrawConnections()
        {
            if (OptionNode.Action != null)
            {
                Vector2 start, end;
                DialogueEditorUtil.GetConnectionDrawInfo(rect, OptionNode.Action, out start, out end);

                Vector2 toOption = (start - end).normalized;
                Vector2 toAction = (end - start).normalized;

                Handles.DrawBezier(
                    start, end,
                    start + toAction * 50f,
                    end + toOption * 50f,
                    Color.blue, null, 5f);
            }
        }

        protected override void OnSetSelected(bool selected)
        {
            if (selected)
                currentBoxStyle = selectedNodeStyle;
            else
                currentBoxStyle = defaultNodeStyle;
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
            OnConnectToAction?.Invoke(this);
        }

        private void DeleteThisNode()
        {
            OnUINodeDeleted?.Invoke(this);
        }
    }
}