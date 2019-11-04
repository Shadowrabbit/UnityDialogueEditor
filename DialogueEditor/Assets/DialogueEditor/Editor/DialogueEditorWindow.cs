using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    public class DialogueEditorWindow : EditorWindow
    {

        public enum eInputState
        {
            Regular             =   0,
            PlacingOption,
            ConnectingOption,
            PlacingAction,
            ConnectingAction,
        }

        // Consts
        public const float TOOLBAR_HEIGHT = 17;
        public const float PANEL_WIDTH = 200;

        // Static
        public static bool NodeClickedOnThisUpdate { get; set; }

        private static UINode CurrentlySelectedNode { get; set; }

        // The NPCDialogue scriptable object that is currently being viewed/edited
        private NPCConversation CurrentAsset;
        private ConversationAction ConversationRoot;

        // List of all nodes and connections currently being drawn in editor window
        private List<UINode> uiNodes;

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

        // Input and input-state logic
        private eInputState m_inputState;
        private UINode m_currentPlacingNode = null;
        private UINode m_currentConnectingNode = null;

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
            NPCConversation conversation = EditorUtility.InstanceIDToObject(assetInstanceID) as NPCConversation;

            if (conversation != null)
            {
                DialogueEditorWindow window = ShowWindow();
                window.CurrentAsset = conversation;
                window.OnNewAssetSelected();
                return true;
            }
            return false;
        }




        //--------------------------------------
        // New NPCDialogue asset selected
        //--------------------------------------

        public void OnNewAssetSelected()
        {
            uiNodes.Clear();

            // Deseralize the asset to get the conversation root
            ConversationRoot = CurrentAsset.GetDeserialized();

            // If it's null, create a root
            if (ConversationRoot == null)
            {
                ConversationRoot = new ConversationAction();
                ConversationRoot.EditorInfo.xPos = (Screen.width / 2) - (UIActionNode.Width / 2);
                ConversationRoot.EditorInfo.yPos = 0;
            }

            // Get a list of every node in the conversation
            List<ConversationNode> allNodes = DialogueEditorUtil.GetAllNodes(ConversationRoot);

            // For every node: 
            // 1: Create a corresponding UI Node to represent it, and add it to the list
            // 2: Tell any of the nodes children that the node is the childs parent
            for (int i = 0; i < allNodes.Count; i++)
            {
                ConversationNode node = allNodes[i];

                if (node is ConversationAction)
                {
                    // 1
                    UIActionNode uiNode = new UIActionNode(node,
                        new Vector2(node.EditorInfo.xPos, node.EditorInfo.yPos));

                    uiNodes.Add(uiNode);

                    // 2
                    ConversationAction action = node as ConversationAction;
                    if (action.Options != null)
                        for (int j = 0; j < action.Options.Count; j++)
                            action.Options[j].Parent = action;
                }
                else
                {
                    // 1
                    UIOptionNode uiNode = new UIOptionNode(node,
                        new Vector2(node.EditorInfo.xPos, node.EditorInfo.yPos));

                    uiNodes.Add(uiNode);

                    // 2
                    ConversationOption option = node as ConversationOption;
                    if (option.Action != null)
                        option.Action.Parent = option;
                }
            }


            Repaint();
        }




        //--------------------------------------
        // OnEnable, OnDisable
        //--------------------------------------

        private void OnEnable()
        {
            if (uiNodes == null)
                uiNodes = new List<UINode>();

            InitGUIStyles();

            UINode.OnUINodeSelected += SelectNode;
            UINode.OnUINodeDeleted += DeleteUINode;
            UIActionNode.OnCreateOption += CreateNewOption;
            UIOptionNode.OnCreateAction += CreateNewAction;
        }

        private void InitGUIStyles()
        {
            // Panel style
            panelStyle = new GUIStyle();
            panelStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/backgroundwithinnershadow.png") as Texture2D;
        }

        private void OnDisable()
        {
            UINode.OnUINodeSelected -= SelectNode;
            UINode.OnUINodeDeleted -= DeleteUINode;
            UIActionNode.OnCreateOption -= CreateNewOption;
            UIOptionNode.OnCreateAction -= CreateNewAction;
        }




        //--------------------------------------
        // Update
        //--------------------------------------

        NPCConversation _cachedSelectedAsset;
        ScriptableObject _newlySelectedAsset;    

        private void Update()
        {
            _newlySelectedAsset = EditorUtility.InstanceIDToObject(Selection.activeInstanceID) as ScriptableObject;
            if (_newlySelectedAsset != null)
            {
                if (_newlySelectedAsset is NPCConversation)
                {
                    _cachedSelectedAsset = _newlySelectedAsset as NPCConversation;

                    if (_cachedSelectedAsset != CurrentAsset)
                    {
                        CurrentAsset = _cachedSelectedAsset;
                        OnNewAssetSelected();
                    }                      
                }
                else
                {
                    CurrentAsset = null;
                    Repaint();
                }
            }
            else
            {
                CurrentAsset = null;
                Repaint();
            }

            switch (m_inputState)
            {
                case eInputState.PlacingOption:
                case eInputState.PlacingAction:
                    Repaint();
                    break;
            }
        }




        //--------------------------------------
        // Draw
        //--------------------------------------

        private void OnGUI()
        {
            if (CurrentAsset == null)
            {
                DrawTitleBar();
                Repaint();
                return;
            }

            // Process interactions
            ProcessInput();

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
                Recenter();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Save();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNodes()
        {
            if (uiNodes != null)
            {
                for (int i = 0; i < uiNodes.Count; i++)
                {
                    uiNodes[i].Draw();
                }
            }
        }

        private void DrawConnections()
        {
            for (int i = 0; i < uiNodes.Count; i++)
            {
                uiNodes[i].DrawConnections();
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
            if (CurrentlySelectedNode != null)
            {
                // Clear focus upon switching focus to another node.
                if (CurrentlySelectedNode != m_cachedSelectedNode)
                {
                    GUI.FocusControl("");
                }
                m_cachedSelectedNode = CurrentlySelectedNode;

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
                if (CurrentlySelectedNode is UIActionNode)
                {
                    ConversationAction node = (CurrentlySelectedNode.Info as ConversationAction);

                    // Title
                    EditorGUI.LabelField(propertyRect, "Action Node", panelTitleStyle);
                    propertyRect.y += bigGap;

                    // Action text
                    int textBoxHeight = 100;
                    propertyRect.height += textBoxHeight;
                    node.Text = EditorGUI.TextField(propertyRect, node.Text);
                    propertyRect.height -= textBoxHeight;
                    propertyRect.y += bigGap + textBoxHeight;
                }

                // Option node info
                else if (CurrentlySelectedNode is UIOptionNode)
                {
                    ConversationOption node = CurrentlySelectedNode.Info as ConversationOption;

                    // Title
                    EditorGUI.LabelField(propertyRect, "Option Node", panelTitleStyle);
                    propertyRect.y += bigGap;

                    // Option text Value
                    string valueTitle = "Chat option";
                    EditorGUI.LabelField(propertyRect, valueTitle, panelPropertyStyle);
                    propertyRect.y += smallGap;
                    int textBoxHeight = 100;
                    propertyRect.height += textBoxHeight;
                    node.Text = EditorGUI.TextField(propertyRect, node.Text);
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
        // Input
        //--------------------------------------

        private void ProcessInput()
        {
            Event e = Event.current;

            switch (m_inputState)
            {
                case eInputState.Regular:
                    ProcessNodeEvents(e);
                    ProcessEvents(e);
                    break;

                case eInputState.PlacingOption:
                    m_currentPlacingNode.SetPosition(e.mousePosition);

                    // Left click
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // Place the option
                        SelectNode(m_currentPlacingNode, true);
                        m_inputState = eInputState.Regular;
                        Repaint();
                    }
                    break;

                case eInputState.ConnectingOption:
                    break;

                case eInputState.PlacingAction:
                    m_currentPlacingNode.SetPosition(e.mousePosition);

                    // Left click
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // Place the option
                        SelectNode(m_currentPlacingNode, true);
                        m_inputState = eInputState.Regular;
                        Repaint();
                    }
                    break;

                case eInputState.ConnectingAction:
                    break;
            }


        }

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
                                UnselectNode();
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
            if (uiNodes != null)
            {
                NodeClickedOnThisUpdate = false;

                for (int i = 0; i < uiNodes.Count; i++)
                {
                    bool guiChanged = uiNodes[i].ProcessEvents(e);
                    if (guiChanged)
                        GUI.changed = true;
                }
            }
        }

        private void OnDrag(Vector2 delta)
        {
            dragDelta = delta;

            if (uiNodes != null)
            {
                for (int i = 0; i < uiNodes.Count; i++)
                {
                    uiNodes[i].Drag(delta);
                }
            }

            GUI.changed = true;
        }




        //--------------------------------------
        // Event listeners
        //--------------------------------------

        /* -- Creating Nodes -- */

        public void CreateNewOption(UIActionNode actionUI)
        {
            // Create new option, the argument action is the options parent
            ConversationOption newOption = new ConversationOption();

            // Add the option to the actions' list of options
            actionUI.ConversationNode.AddOption(newOption);       

            // The option doesn't point to an action yet
            newOption.Action = null;

            // Create a new UI object to represent the new option
            UIOptionNode ui = new UIOptionNode(newOption, Vector2.zero);
            uiNodes.Add(ui);

            // Set the input state appropriately
            m_inputState = eInputState.PlacingOption;
            m_currentPlacingNode = ui;
        }


        public void CreateNewAction(UIOptionNode option)
        {
            // Create new action, the argument option is the actions parent
            ConversationAction newAction = new ConversationAction();

            // Set this new action as the options child
            option.OptionNode.SetAction(newAction);

            // This new action doesn't have any children yet
            newAction.Options = null;

            // Create a new UI object to represent the new action
            UIActionNode ui = new UIActionNode(newAction, Vector2.zero);
            uiNodes.Add(ui);

            // Set the input state appropriately
            m_inputState = eInputState.PlacingAction;
            m_currentPlacingNode = ui;
        }


        /* -- Deleting Nodes -- */

        public void DeleteUINode(UINode node)
        {
            // Delete tree/internal objects
            node.Info.RemoveSelfFromTree();

            // Delete the UI classes
            uiNodes.Remove(node);
            node = null;
        }




        //--------------------------------------
        // Util
        //--------------------------------------

        private void SelectNode(UINode node, bool selected)
        {
            if (selected)
            {
                if (CurrentlySelectedNode != null)
                    CurrentlySelectedNode.SetSelected(false);

                CurrentlySelectedNode = node;
                CurrentlySelectedNode.SetSelected(true);
            }
            else
            {
                node.SetSelected(false);
                CurrentlySelectedNode = null;
            }
        }

        private void UnselectNode()
        {
            if (CurrentlySelectedNode != null)
                CurrentlySelectedNode.SetSelected(false);
        }

        private bool IsANodeSelected()
        {
            if (uiNodes != null)
            {
                for (int i = 0; i < uiNodes.Count; i++)
                {
                    if (uiNodes[i].isSelected) return true;
                }
            }
            return false;
        }

        public void Recenter()
        {
            if (ConversationRoot == null) { return; }

            // Calc delta to move head to (middle, 0) and then apply this to all nodes
            Vector2 target = new Vector2((position.width / 2) - (UIActionNode.Width / 2) - (PANEL_WIDTH / 2), TOOLBAR_HEIGHT);
            Vector2 delta = target - new Vector2(ConversationRoot.EditorInfo.xPos, ConversationRoot.EditorInfo.yPos);
            for (int i = 0; i < uiNodes.Count; i++)
            {
                uiNodes[i].Drag(delta);
            }
        }

        private void Save()
        {
            if (CurrentAsset != null)
                CurrentAsset.Serialize(ConversationRoot);
        }
    }
}