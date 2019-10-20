using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public class UIConnection
    {
        public UINode From;
        public ConversationNode To;
    }

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
        public GUIStyle style;
        public GUIStyle defaultNodeStyle;
        public GUIStyle selectedNodeStyle;

        // Properties
        public ConversationNode Info { get; protected set; }


        // Constructor and creation logic
        public UINode(ConversationNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
        {
            Info = infoNode;
            style = defaultStyle;
            defaultNodeStyle = defaultStyle;
            selectedNodeStyle = selectedStyle;
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
            GUI.Box(rect, title);
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
        public abstract void DrawConnections();
        public abstract void AddConnection(UINode node);
        protected abstract void ValidateConnections();
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

        public ConversationAction ConversationNode { get { return Info as ConversationAction; } }

        private List<UIConnection> connections;

        public UIActionNode(ConversationNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
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

            GUI.Box(rect, ConversationNode.Text);
        }

        public override void DrawConnections()
        {
            ValidateConnections();

            if (connections != null && connections.Count > 0)
            {
                Vector2 start, end;

                for (int i = 0; i < connections.Count; i++)
                {
                    DialogueEditorUtil.GetConnectionDrawInfo(rect, connections[i].To, out start, out end);

                    Vector2 toOption = (start - end).normalized;
                    Vector2 toAction = (end - start).normalized;

                    Handles.DrawBezier(
                        start, end,
                        start + toAction * 50f,
                        end + toOption * 50f,
                        Color.black, null, 5f);
                }
            }
        }

        public override void AddConnection(UINode node)
        {
            if (connections == null)
                connections = new List<UIConnection>();

            UIConnection newConnection = new UIConnection
            {
                From = this,
                To = node.Info
            };
            connections.Add(newConnection);
        }

        protected override void ValidateConnections()
        {
            if (ConversationNode.Options == null || ConversationNode.Options.Count == 0)
                return;

            if (connections == null)
            {
                RecreateConnections();
                return;
            }

            if (connections.Count != ConversationNode.Options.Count)
            {
                RecreateConnections();
                return;
            }

            for (int i = 0; i < ConversationNode.Options.Count; i++)
            {
                if (connections[i].To != ConversationNode.Options[i])
                {
                    RecreateConnections();
                    return;
                }
            }
        }

        private void RecreateConnections()
        {
            if (connections == null)
                connections = new List<UIConnection>();
            connections.Clear();

            if (ConversationNode.Options != null && ConversationNode.Options.Count > 0)
            {
                for (int i = 0; i < ConversationNode.Options.Count; i++)
                {
                    UIConnection c = new UIConnection
                    {
                        From = this,
                        To = ConversationNode.Options[i]
                    };
                    connections.Add(c);
                }
            }
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

        public static int Width { get { return 200; } }
        public static int Height { get { return 50; } }

        public ConversationOption OptionNode { get { return Info as ConversationOption; } }

        private UIConnection connection;

        public UIOptionNode(ConversationNode infoNode, Vector2 pos, GUIStyle defaultStyle, GUIStyle selectedStyle)
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

            GUI.Box(rect, OptionNode.Text);
        }

        public override void DrawConnections()
        {
            ValidateConnections();

            if (connection != null)
            {
                Vector2 start, end;
                DialogueEditorUtil.GetConnectionDrawInfo(rect, connection.To, out start, out end);

                Vector2 toOption = (start - end).normalized;
                Vector2 toAction = (end - start).normalized;

                Handles.DrawBezier(
                    start, end,
                    start + toAction * 50f,
                    end + toOption * 50f,
                    Color.black, null, 5f);
            }
        }

        public override void AddConnection(UINode node)
        {

        }

        protected override void ValidateConnections()
        {
            // No connection required
            if (OptionNode.Action == null)
            {
                if (connection != null)
                    connection = null;
                return;
            }

            // Connection must be created
            if (OptionNode.Action != null && connection == null)
            {
                connection = new UIConnection
                {
                    From = this,
                    To = this.OptionNode.Action
                };
            }
            // Connection exists but is incorrect (links to another node, etc
            else if (connection.To != OptionNode.Action)
            {
                connection.From = this;
                connection.To = OptionNode.Action;
            }
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
            OnUINodeDeleted?.Invoke(this);
        }
    }
}