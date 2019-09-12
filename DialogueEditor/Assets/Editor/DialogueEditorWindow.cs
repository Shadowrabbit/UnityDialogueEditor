using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public class DialogueEditorWindow : EditorWindow
    {
        // Consts
        public const float TOOLBAR_HEIGHT = 17;
        public const float PANEL_WIDTH = 200;

        // Static
        public static bool NodeClickedOnThisUpdate { get; set; }

        // The NPCDialogue scriptable object that is currently being viewed/edited
        private NPCDialogue Dialogue;

        // List of all nodes and connections currently being drawn in editor window
        private List<UINode> nodes;

        // Node GUIStyles
        private GUIStyle defaultNodeStyle;
        private GUIStyle selectedNodeStyle;

        // Right-hand display pannel vars
        private Rect panelRect;
        private GUIStyle panelStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle panelPropertyStyle;
        private UINode m_cachedSelectedNode;

        // Dragging information
        private bool clickInBox;
        private Vector2 offset;
        private Vector2 dragDelta;   

        // Cleanup
        bool placeholder_toggle_bool;




        //--------------------------------------
        // Open window
        //--------------------------------------

        [MenuItem("Window/DialogueEditor")]
        public static DialogueEditorWindow ShowWindow()
        {
            return EditorWindow.GetWindow<DialogueEditorWindow>("Dialogue Editor");
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OpenDialogue(int assetInstanceID, int line)
        {
            NPCDialogue dialogue = EditorUtility.InstanceIDToObject(assetInstanceID) as NPCDialogue;

            if (dialogue != null)
            {
                DialogueEditorWindow window = ShowWindow();
                window.Dialogue = dialogue;
                window.OnDialogueChanged();
                return true;
            }
            return false;
        }




        //--------------------------------------
        // New NPCDialogue asset selected
        //--------------------------------------

        public void OnDialogueChanged()
        {
            nodes.Clear();

            if (Dialogue.Root == null)
            {
                Dialogue.Root = new NPCActionNode();
                Dialogue.Root.uiX = (Screen.width / 2) - (UIActionNode.Width / 2);
                Dialogue.Root.uiY = 0;
            }

            List<NPCNode> npcNodes = Dialogue.GetAllNodesInTree();

            foreach (NPCNode node in npcNodes)
            {
                if (node is NPCActionNode)
                {
                    nodes.Add(new UIActionNode(node, new Vector2(node.uiX, node.uiY), defaultNodeStyle, selectedNodeStyle));
                }
                else if (node is NPCOptionNode)
                {
                    nodes.Add(new UIOptionNode(node, new Vector2(node.uiX, node.uiY), defaultNodeStyle, selectedNodeStyle));
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Info == Dialogue.Root)
                {
                    UINode.SelectedNode = nodes[i];
                }
            }

            Repaint();
        }




        //--------------------------------------
        // OnEnable, OnDisable
        //--------------------------------------

        private void OnEnable()
        {
            if (nodes == null)
                nodes = new List<UINode>();

            InitGUIStyles();
            UINode.OnNodeRemoved += RemoveNode;
            UINode.OnOptionAdded += AddOption;
        }

        private void InitGUIStyles()
        {
            // Default node style
            defaultNodeStyle = new GUIStyle();
            defaultNodeStyle.normal.background = new Texture2D(1, 1);
            defaultNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/node0.png") as Texture2D;
            defaultNodeStyle.border = new RectOffset(12, 12, 12, 12);

            // Selected node style
            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = new Texture2D(1, 1);
            selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/node0 on.png") as Texture2D;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

            // Panel style
            panelStyle = new GUIStyle();
            panelStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/backgroundwithinnershadow.png") as Texture2D;
        }

        private void OnDisable()
        {
            UINode.OnNodeRemoved -= RemoveNode;
            UINode.OnOptionAdded -= AddOption;
        }




        //--------------------------------------
        // Update
        //--------------------------------------

        ScriptableObject _currentlySelectedAsset;
        NPCDialogue _npcDialogue;

        private void Update()
        {
            _currentlySelectedAsset = EditorUtility.InstanceIDToObject(Selection.activeInstanceID) as ScriptableObject;
            if (_currentlySelectedAsset)
            {
                if (_currentlySelectedAsset is NPCDialogue)
                {
                    _npcDialogue = _currentlySelectedAsset as NPCDialogue;

                    if (_npcDialogue != Dialogue)
                    {
                        Dialogue = _npcDialogue;
                        OnDialogueChanged();
                    }                      
                }
                else
                {
                    Dialogue = null;
                    Repaint();
                }
            }
            else
            {
                Dialogue = null;
                Repaint();
            }          
        }




        //--------------------------------------
        // Draw
        //--------------------------------------

        private void OnGUI()
        {
            if (Dialogue == null)
            {
                DrawTitleBar();
                Repaint();
                return;
            }

            // Process interactions
            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);

            // Draw
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);
            DrawConnections();
            DrawNodes();
            DrawPanel();
            DrawResizer();
            DrawTitleBar();

            if (GUI.changed)
                Repaint();
        }

        private void DrawTitleBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Reset view", EditorStyles.toolbarButton))
            {
                RepositionNodes();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawNodes()
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Draw();
                }
            }
        }

        private void DrawConnections()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].DrawConnection();
            }
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            offset += dragDelta * 0.5f;
            Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawPanel()
        {
            // Create rect, begin area
            panelRect = new Rect(position.width - PANEL_WIDTH, TOOLBAR_HEIGHT, PANEL_WIDTH, position.height - TOOLBAR_HEIGHT);
            GUILayout.BeginArea(panelRect, panelStyle);

            // Draw info of selected node
            if (UINode.SelectedNode != null)
            {
                // Clear focus upon switching focus to another node.
                if (UINode.SelectedNode != m_cachedSelectedNode)
                {
                    GUI.FocusControl("");
                }
                m_cachedSelectedNode = UINode.SelectedNode;

                // GUIStyle for box title
                panelTitleStyle = new GUIStyle();
                panelTitleStyle.alignment = TextAnchor.MiddleCenter;
                panelTitleStyle.fontStyle = FontStyle.Bold;

                // GUIStyle for property title
                panelPropertyStyle = new GUIStyle();
                panelPropertyStyle.fontStyle = FontStyle.Bold;

                // Rect for elements
                int padding = 12;
                Rect propertyRect = new Rect(padding, 0, panelRect.width - padding * 2, 20);
                int smallGap = 20;
                int bigGap = 30;

                // Action node info
                if (UINode.SelectedNode is UIActionNode)
                {
                    NPCActionNode node = (UINode.SelectedNode.Info as NPCActionNode);

                    // Title
                    EditorGUI.LabelField(propertyRect, "Action Node", panelTitleStyle);
                    propertyRect.y += bigGap;

                    // Action Type
                    EditorGUI.LabelField(propertyRect, "Action Type", panelPropertyStyle);
                    propertyRect.y += smallGap;
                    node.ActionType = (eNodeType)EditorGUI.EnumPopup(propertyRect, node.ActionType);
                    propertyRect.y += bigGap;

                    // Action Value
                    string valueTitle = (node.ActionType == eNodeType.Dialogue) ? "Dialogue" : "Action";
                    EditorGUI.LabelField(propertyRect, valueTitle, panelPropertyStyle);
                    propertyRect.y += smallGap;
                    int textBoxHeight = 100;
                    propertyRect.height += textBoxHeight;
                    node.ActionValue = EditorGUI.TextField(propertyRect, node.ActionValue);
                    propertyRect.height -= textBoxHeight;
                    propertyRect.y += bigGap + textBoxHeight;
                }

                // Option node info
                else if (UINode.SelectedNode is UIOptionNode)
                {
                    NPCOptionNode node = UINode.SelectedNode.Info as NPCOptionNode;

                    // Title
                    EditorGUI.LabelField(propertyRect, "Option Node", panelTitleStyle);
                    propertyRect.y += bigGap;

                    // Option text Value
                    string valueTitle = "Chat option";
                    EditorGUI.LabelField(propertyRect, valueTitle, panelPropertyStyle);
                    propertyRect.y += smallGap;
                    int textBoxHeight = 100;
                    propertyRect.height += textBoxHeight;
                    node.Value = EditorGUI.TextField(propertyRect, node.Value);
                    propertyRect.height -= textBoxHeight;
                    propertyRect.y += bigGap + textBoxHeight;
                }
            }

            GUILayout.EndArea();
        }

        private void DrawResizer()
        {

            GUIStyle resizerStyle;
            Rect resizer;
            resizerStyle = new GUIStyle();
            resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;

            resizer = new Rect(position.width - PANEL_WIDTH - 2, 0, 5, (position.height) - TOOLBAR_HEIGHT);

            GUILayout.BeginArea(new Rect(resizer.position, new Vector2(2, position.height)), resizerStyle);
            GUILayout.EndArea();
        }




        //--------------------------------------
        // Input/Events
        //--------------------------------------

        private void ProcessEvents(Event e)
        {
            dragDelta = Vector2.zero;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (panelRect.Contains(e.mousePosition))
                        {
                            clickInBox = true;
                        }
                        else
                        {
                            clickInBox = false;
                            if (!DialogueEditorWindow.NodeClickedOnThisUpdate)
                            {
                                UINode.SelectedNode = null;
                            }
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && !clickInBox && !IsANodeSelected())
                    {
                        OnDrag(e.delta);
                    }
                    break;
            }
        }

        private void ProcessNodeEvents(Event e)
        {
            if (nodes != null)
            {
                NodeClickedOnThisUpdate = false;

                for (int i = 0; i < nodes.Count; i++)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);
                    if (guiChanged)
                        GUI.changed = true;
                }
            }
        }

        private void RemoveNode(UINode node)
        {
            if (node.Info == Dialogue.Root)
            {
                Debug.Log("You cannot delete the root node");
                return;
            }
            nodes.Remove(node);
        }

        private void AddOption(UIActionNode node)
        {
            NPCOptionNode option = new NPCOptionNode();
            NPCActionNode resultingAction = new NPCActionNode();

            // node -> option -> action
            (node.Info as NPCActionNode).AddOption(option);
            option.SetResult(resultingAction);

            // Create UI nodes
            nodes.Add(new UIOptionNode(option, Vector2.zero, defaultNodeStyle, selectedNodeStyle));
            nodes.Add(new UIActionNode(resultingAction, Vector2.one * 5, defaultNodeStyle, selectedNodeStyle));
        }

        private void OnDrag(Vector2 delta)
        {
            dragDelta = delta;

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Drag(delta);
                }
            }

            GUI.changed = true;
        }

        private bool IsANodeSelected()
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].isSelected) return true;
                }
            }
            return false;
        }




        //--------------------------------------
        // Util
        //--------------------------------------

        public void RepositionNodes()
        {
            // Calc delta to move head to (middle, 0) and then apply this to all nodes
            Vector2 target = new Vector2((position.width / 2) - (UIActionNode.Width / 2), TOOLBAR_HEIGHT);
            Vector2 delta = target - new Vector2(Dialogue.Root.uiX, Dialogue.Root.uiY);
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Drag(delta);
            }
        }
    }
}