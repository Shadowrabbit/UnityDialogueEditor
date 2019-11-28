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

        // Consts
        protected const int TEXT_BORDER = 5;
        protected const int TITLE_HEIGHT = 25;
        protected const int TEXT_BOX_HEIGHT = 35;

        // Members
        public Rect rect;
        public bool isDragged;
        public bool isSelected;
        protected string title;
        protected GUIStyle currentBoxStyle;

        // Static
        private static GUIStyle titleStyle;

        // Properties
        public ConversationNode Info { get; protected set; }


        //---------------------------------
        // Constructor 
        //---------------------------------

        public UINode(ConversationNode infoNode, Vector2 pos)
        {
            Info = infoNode;

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle();
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.fontStyle = FontStyle.Bold;
            }
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




        //---------------------------------
        // Drawing
        //---------------------------------

        public void Draw()
        {
            // Box
            GUI.Box(rect, title, currentBoxStyle);

            OnDraw();
        }

        protected void DrawTitle(string text)
        {
            // Internals 
            Rect internalText = new Rect(rect.x, rect.y, rect.width, TITLE_HEIGHT);
            GUI.Label(internalText, text, titleStyle);
        }

        protected void DrawInternalText(string text)
        {
            Rect internalText = new Rect(rect.x + TEXT_BORDER, rect.y + TITLE_HEIGHT, rect.width - TEXT_BORDER * 2, TEXT_BOX_HEIGHT);
            GUI.Box(internalText, text, EditorStyles.textArea);
        }



        //---------------------------------
        // Interactions / Events
        //---------------------------------

        public void Drag(Vector2 moveDelta)
        {
            rect.position += moveDelta;
            Info.EditorInfo.xPos = rect.x;
            Info.EditorInfo.yPos = rect.y;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                isDragged = true;
                isSelected = true;
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
                        if (rect.Contains(e.mousePosition))
                        {
                            DialogueEditorWindow.NodeClickedOnThisUpdate = true;
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




        //---------------------------------
        // Abstract methods
        //---------------------------------

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
        // Events
        public delegate void CreateOptionEvent(UIActionNode node);
        public static CreateOptionEvent OnCreateOption;

        public delegate void ConnectToOptionEvent(UIActionNode node);
        public static ConnectToOptionEvent OnConnectToOption;

        // Static properties
        public static int Width { get { return 200; } }
        public static int Height { get { return 75; } }

        // Properties
        public ConversationAction ConversationNode { get { return Info as ConversationAction; } }

        // Static styles
        protected static GUIStyle defaultNodeStyle;
        protected static GUIStyle selectedNodeStyle;


        //---------------------------------
        // Constructor
        //---------------------------------

        public UIActionNode(ConversationNode infoNode, Vector2 pos) : base(infoNode, pos)
        {
            if (defaultNodeStyle == null)
            {
                defaultNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(163, 77, 77));
                t2d.Apply();
                defaultNodeStyle.normal.background = t2d;
            }
            if (selectedNodeStyle == null)
            {
                selectedNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(196, 92, 92));
                t2d.Apply();
                selectedNodeStyle.normal.background = t2d;
            }

            currentBoxStyle = defaultNodeStyle;

            CreateRect(pos, Width, Height);
        }



        //---------------------------------
        // Drawing
        //---------------------------------

        public override void OnDraw()
        {
            DrawTitle("Action node.");
            DrawInternalText(ConversationNode.Text);
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




        //---------------------------------
        // Interactions
        //---------------------------------

        protected override void OnSetSelected(bool selected)
        {
            if (selected)
                currentBoxStyle = selectedNodeStyle;
            else
                currentBoxStyle = defaultNodeStyle;
        }




        //---------------------------------
        // Right clicked
        //---------------------------------

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
            OnConnectToOption?.Invoke(this);
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
        // Events
        public delegate void CreateActionEvent(UIOptionNode node);
        public static CreateActionEvent OnCreateAction;

        public delegate void ConnectToActionEvent(UIOptionNode node);
        public static ConnectToActionEvent OnConnectToAction;

        // Static properties
        public static int Width { get { return 200; } }
        public static int Height { get { return 75; } }

        // Properties
        public ConversationOption OptionNode { get { return Info as ConversationOption; } }

        // Static styles
        protected static GUIStyle defaultNodeStyle;
        protected static GUIStyle selectedNodeStyle;


        //---------------------------------
        // Constructor 
        //---------------------------------

        public UIOptionNode(ConversationNode infoNode, Vector2 pos) : base(infoNode, pos)
        {
            if (defaultNodeStyle == null)
            {
                defaultNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(113, 150, 150));
                t2d.Apply();
                defaultNodeStyle.normal.background = t2d;
            }
            if (selectedNodeStyle == null)
            {
                selectedNodeStyle = new GUIStyle();
                Texture2D t2d = new Texture2D(Width, Height);
                for (int x = 0; x < Width - 1; x++)
                    for (int y = 0; y < Height - 1; y++)
                        t2d.SetPixel(x, y, DialogueEditorUtil.Colour(136, 184, 184));
                t2d.Apply();
                selectedNodeStyle.normal.background = t2d;
            }

            currentBoxStyle = defaultNodeStyle;

            CreateRect(pos, Width, Height);
        }




        //---------------------------------
        // Drawing
        //---------------------------------

        public override void OnDraw()
        {
            DrawTitle("Option node.");
            DrawInternalText(OptionNode.Text);
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




        //---------------------------------
        // Interactions
        //---------------------------------

        protected override void OnSetSelected(bool selected)
        {
            if (selected)
                currentBoxStyle = selectedNodeStyle;
            else
                currentBoxStyle = defaultNodeStyle;
        }




        //---------------------------------
        // Right clicked
        //---------------------------------

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