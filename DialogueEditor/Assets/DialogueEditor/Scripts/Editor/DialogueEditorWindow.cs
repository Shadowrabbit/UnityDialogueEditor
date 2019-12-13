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
            Regular                     = 0,
            PlacingOption               = 1,
            ConnectingOptionToAction    = 2,
            PlacingAction               = 3,
            ConnectingActionToOption    = 4,
            draggingPanel               = 5,
        }

        // Consts
        public const float TOOLBAR_HEIGHT = 17;
        public const float START_PANEL_WIDTH = 250;
        private const string WINDOW_NAME = "DIALOGUE_EDITOR_WINDOW";

        // Static properties
        public static bool NodeClickedOnThisUpdate { get; set; }
        private static UINode CurrentlySelectedNode { get; set; }

        // Private variables:     
        private NPCConversation CurrentAsset;           // The Conversation scriptable object that is currently being viewed/edited
        public static ConversationAction ConversationRoot { get; private set; }    // The root node of the conversation
        private List<UINode> uiNodes;                   // List of all UI nodes

        // Selected asset logic
        private NPCConversation currentlySelectedAsset;
        private Transform newlySelectedAsset;

        // Right-hand display pannel vars
        private float panelWidth;
        private Rect panelRect;
        private GUIStyle panelStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle panelPropertyStyle;
        private Rect panelResizerRect;
        private GUIStyle resizerStyle;
        private UINode m_cachedSelectedNode;

        // Dragging information
        private bool clickInBox;
        private Vector2 offset;
        private Vector2 dragDelta;

        // Input and input-state logic
        private eInputState m_inputState;
        private UINode m_currentPlacingNode = null;
        private UINode m_currentConnectingNode = null;
        private ConversationNode m_connectionDeleteParent, m_connectionDeleteChild;




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
                window.LoadNewAsset(conversation);
                return true;
            }
            return false;
        }




        //--------------------------------------
        // Load New Asset
        //--------------------------------------

        public void LoadNewAsset(NPCConversation asset)
        {
            CurrentAsset = asset;
            Log("Loading new asset: " + CurrentAsset.name);

            // Clear all current UI Nodes
            uiNodes.Clear();

            // Deseralize the asset and get the conversation root
            Conversation conversation = CurrentAsset.Dejsonify();
            if (conversation == null)
                conversation = new Conversation();
            ConversationRoot = conversation.GetRootNode();

            // If it's null, create a root
            if (ConversationRoot == null)
            {
                ConversationRoot = new ConversationAction();
                ConversationRoot.EditorInfo.xPos = (Screen.width / 2) - (UIActionNode.Width / 2);
                ConversationRoot.EditorInfo.yPos = 0;
                ConversationRoot.EditorInfo.isRoot = true;
                conversation.Actions.Add(ConversationRoot);
            }

            // Get a list of every node in the conversation
            List<ConversationNode> allNodes = new List<ConversationNode>();
            for (int i = 0; i < conversation.Actions.Count; i++)
                allNodes.Add(conversation.Actions[i]);
            for (int i = 0; i < conversation.Options.Count; i++)
                allNodes.Add(conversation.Options[i]);

            // For every node: 
            // Find the children and parents by UID
            for (int i = 0; i < allNodes.Count; i++)
            {
                // Remove duplicate parent UIDs
                HashSet<int> noDupes = new HashSet<int>(allNodes[i].parentUIDs);
                allNodes[i].parentUIDs.Clear();
                foreach (int j in noDupes)
                    allNodes[i].parentUIDs.Add(j);          

                allNodes[i].parents = new List<ConversationNode>();
                for (int j = 0; j < allNodes[i].parentUIDs.Count; j++)
                {
                    allNodes[i].parents.Add(conversation.GetNodeByUID(allNodes[i].parentUIDs[j]));
                }

                if (allNodes[i] is ConversationAction)
                {
                    int count = (allNodes[i] as ConversationAction).OptionUIDs.Count;
                    (allNodes[i] as ConversationAction).Options = new List<ConversationOption>();
                    for (int j = 0; j < count; j++)
                    {
                        (allNodes[i] as ConversationAction).Options.Add(
                            conversation.GetOptionByUID((allNodes[i] as ConversationAction).OptionUIDs[j]));
                    }
                }
                else if (allNodes[i] is ConversationOption)
                {
                    (allNodes[i] as ConversationOption).Action = 
                        conversation.GetActionByUID((allNodes[i] as ConversationOption).ActionUID);
                }
            }

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
                            action.Options[j].parents.Add(action);
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
                        option.Action.parents.Add(option);
                }
            }

            Repaint();
        }




        //--------------------------------------
        // OnEnable, OnDisable, OnFocus, LostFocus, 
        // Destroy, SelectionChange, ReloadScripts
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
            UIActionNode.OnConnectToOption += ConnectActionToOption;
            UIOptionNode.OnConnectToAction += ConnectOptionToAction;

            this.name = WINDOW_NAME;
            panelWidth = START_PANEL_WIDTH;
        }

        private void InitGUIStyles()
        {
            // Panel style
            panelStyle = new GUIStyle();
            panelStyle.normal.background = EditorGUIUtility.Load("builtin skins/lightskin/images/backgroundwithinnershadow.png") as Texture2D;

            // Panel title style
            panelTitleStyle = new GUIStyle();
            panelTitleStyle.alignment = TextAnchor.MiddleCenter;
            panelTitleStyle.fontStyle = FontStyle.Bold;

            // Resizer style
            resizerStyle = new GUIStyle();
            resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;

        }

        private void OnDisable()
        {
            Log("Saving. Reason: Disable.");
            Save();

            UINode.OnUINodeSelected -= SelectNode;
            UINode.OnUINodeDeleted -= DeleteUINode;
            UIActionNode.OnCreateOption -= CreateNewOption;
            UIOptionNode.OnCreateAction -= CreateNewAction;
            UIActionNode.OnConnectToOption -= ConnectActionToOption;
            UIOptionNode.OnConnectToAction -= ConnectOptionToAction;
        }

        protected void OnFocus()
        {
            // Get asset the user is selecting
            newlySelectedAsset = Selection.activeTransform;

            // If it's not null
            if (newlySelectedAsset != null)
            {
                // If its a conversation scriptable, load new asset
                if (newlySelectedAsset.GetComponent<NPCConversation>() != null)
                {
                    currentlySelectedAsset = newlySelectedAsset.GetComponent<NPCConversation>();

                    if (currentlySelectedAsset != CurrentAsset)
                    {
                        LoadNewAsset(currentlySelectedAsset);
                    }
                }
            }
        }

        protected void OnLostFocus()
        {
            bool keepOnWindow = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text.Equals("Dialogue Editor");

            if (CurrentAsset != null && !keepOnWindow)
            {
                Log("Saving conversation. Reason: Window Lost Focus.");
                Save();
            }
        }

        protected void OnDestroy()
        {
            Log("Saving conversation. Reason: Window closed.");
            Save();
        }

        protected void OnSelectionChange()
        {
            // Get asset the user is selecting
            newlySelectedAsset = Selection.activeTransform;

            // If it's not null
            if (newlySelectedAsset != null)
            {
                // If it's a different asset and our current asset isn't null, save our current asset
                if (currentlySelectedAsset != null && currentlySelectedAsset != newlySelectedAsset)
                {
                    Log("Saving conversation. Reason: Different asset selected");
                    Save();
                    currentlySelectedAsset = null;
                }

                // If its a conversation scriptable, load new asset
                currentlySelectedAsset = newlySelectedAsset.GetComponent<NPCConversation>();
                if (currentlySelectedAsset != null && currentlySelectedAsset != CurrentAsset)
                {
                    LoadNewAsset(currentlySelectedAsset);
                }
                else
                {
                    CurrentAsset = null;
                    Repaint();
                }
            }
            else
            {
                Log("Saving conversation. Reason: Conversation asset de-selected");
                Save();

                CurrentAsset = null;
                currentlySelectedAsset = null;
                Repaint();
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // Clear our reffrence to the CurrentAsset on script reload in order to prevent 
            // save detection overwriting the object with an empty conversation (save triggerred 
            // with no uiNodes present in window due to recompile). 
            Log("Scripts reloaded. Clearing current asset.");
            ShowWindow().CurrentAsset = null;
        }



        //--------------------------------------
        // Update
        //--------------------------------------

        private void Update()
        {
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
                Save(true);
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

            if (m_inputState == eInputState.ConnectingOptionToAction || 
                m_inputState == eInputState.ConnectingActionToOption)
            {
                Vector2 start, end;
                start = new Vector2(m_currentConnectingNode.rect.x + UIOptionNode.Width / 2,
                    m_currentConnectingNode.rect.y + UIOptionNode.Height / 2);
                end = Event.current.mousePosition;

                Vector2 toOption = (start - end).normalized;
                Vector2 toAction = (end - start).normalized;

                Handles.DrawBezier(
                    start, end,
                    start + toAction * 50f,
                    end + toOption * 50f,
                    Color.black, null, 5f);

                Repaint();
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

        private Vector2 panelVerticalScroll;

        private void DrawPanel()
        {
            panelRect = new Rect(position.width - panelWidth, TOOLBAR_HEIGHT, panelWidth, position.height - TOOLBAR_HEIGHT);
            GUILayout.BeginArea(panelRect, panelStyle);
            GUILayout.BeginVertical();
            panelVerticalScroll = GUILayout.BeginScrollView(panelVerticalScroll);

            GUILayout.Label("Dialogue Editor.", panelTitleStyle);

            if (CurrentlySelectedNode != null)
            {
                bool differentNodeSelected = (m_cachedSelectedNode != CurrentlySelectedNode);
                m_cachedSelectedNode = CurrentlySelectedNode;

                GUILayout.Space(10);

                if (CurrentlySelectedNode is UIActionNode)
                {
                    ConversationAction node = (CurrentlySelectedNode.Info as ConversationAction);
                    GUILayout.Label("[" + node.ID + "] NPC Dialogue Node.", panelTitleStyle);
                    

                    GUILayout.Label("Dialogue", EditorStyles.boldLabel);
                    node.Text = GUILayout.TextArea(node.Text);

                    GUILayout.Label("Icon", EditorStyles.boldLabel);
                    node.Icon = (Sprite)EditorGUILayout.ObjectField(node.Icon, typeof(Sprite), false);

                    GUILayout.Label("Audio", EditorStyles.boldLabel);
                    node.Audio = (AudioClip)EditorGUILayout.ObjectField(node.Audio, typeof(AudioClip), false);

                    // Events
                    {
                        NodeEventHolder NodeEvent = CurrentAsset.GetEventHolderForID(node.ID);
                        if (differentNodeSelected)
                        {
                            CurrentAsset.Event = NodeEvent.Event;
                        }

                        if (NodeEvent != null && NodeEvent.Event != null)
                        {
                            // Load the object and property of the node
                            SerializedObject o = new SerializedObject(NodeEvent);
                            SerializedProperty p = o.FindProperty("Event");

                            // Load the dummy event
                            SerializedObject o2 = new SerializedObject(CurrentAsset);
                            SerializedProperty p2 = o2.FindProperty("Event");

                            // Draw dummy event
                            GUILayout.Label("Events:", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(p2);

                            // Apply changes to dummy
                            o2.ApplyModifiedProperties();

                            // Copy dummy changes onto the nodes event
                            p = p2;
                            o.ApplyModifiedProperties();
                        }
                    }
                }
                else if (CurrentlySelectedNode is UIOptionNode)
                {
                    ConversationOption node = (CurrentlySelectedNode.Info as ConversationOption);
                    GUILayout.Label("[" + node.ID + "] Option Node.", panelTitleStyle);
                    

                    GUILayout.Label("Option text:", EditorStyles.boldLabel);
                    node.Text = GUILayout.TextArea(node.Text);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawResizer()
        {
            panelResizerRect = new Rect(
                position.width - panelWidth - 2,
                0,
                5,
                (position.height) - TOOLBAR_HEIGHT);
            GUILayout.BeginArea(new Rect(panelResizerRect.position, new Vector2(2, position.height)), resizerStyle);
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
                    bool inPanel = panelRect.Contains(e.mousePosition);
                    ProcessNodeEvents(e, inPanel);
                    ProcessEvents(e);
                    break;

                case eInputState.draggingPanel:
                    panelWidth = (position.width - e.mousePosition.x);
                    if (panelWidth < 100)
                        panelWidth = 100;

                    if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        m_inputState = eInputState.Regular;
                        e.Use();
                    }
                    Repaint();
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
                        e.Use();
                    }
                    break;

                case eInputState.ConnectingOptionToAction:

                    // Left click
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        for (int i = 0; i < uiNodes.Count; i++)
                        {
                            if (uiNodes[i] == m_currentConnectingNode)
                                continue;

                            if (!(uiNodes[i] is UIActionNode))
                                continue;

                            if (uiNodes[i].rect.Contains(e.mousePosition))
                            {
                                (m_currentConnectingNode as UIOptionNode).OptionNode.SetAction(
                                    (uiNodes[i] as UIActionNode).ConversationNode);
                                break;
                            }
                        }
                        m_inputState = eInputState.Regular;
                        e.Use();
                    }

                    // Esc
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                    {
                        m_inputState = eInputState.Regular;
                    }
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
                        e.Use();
                    }
                    break;

                case eInputState.ConnectingActionToOption:

                    // Left click
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        for (int i = 0; i < uiNodes.Count; i++)
                        {
                            if (uiNodes[i] == m_currentConnectingNode)
                                continue;

                            if (!(uiNodes[i] is UIOptionNode))
                                continue;

                            if (uiNodes[i].rect.Contains(e.mousePosition))
                            {
                                (m_currentConnectingNode as UIActionNode).ConversationNode.AddOption(
                                    (uiNodes[i] as UIOptionNode).OptionNode);
                                break;
                            }
                        }
                        m_inputState = eInputState.Regular;
                        e.Use();
                    }

                    // Esc
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                    {
                        m_inputState = eInputState.Regular;
                    }
                    break;
            }


        }

        private void ProcessEvents(Event e)
        {
            dragDelta = Vector2.zero;

            switch (e.type)
            {
                case EventType.MouseDown:
                    const float resizerPadding = 5;

                    // Left click
                    if (e.button == 0)
                    {
                        if (panelRect.Contains(e.mousePosition))
                        {
                            clickInBox = true;
                        }
                        else if (e.mousePosition.x > panelResizerRect.x - resizerPadding && 
                            e.mousePosition.x < panelResizerRect.x + panelResizerRect.width + resizerPadding && 
                            e.mousePosition.y > panelResizerRect.y &&
                            panelResizerRect.y < panelResizerRect.y + panelResizerRect.height)
                        {
                            clickInBox = true;
                            m_inputState = eInputState.draggingPanel;
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
                    // Right click
                    else if (e.button == 1)
                    {
                        if (DialogueEditorUtil.IsPointerNearConnection(uiNodes, e.mousePosition, out m_connectionDeleteParent, out m_connectionDeleteChild))
                        {
                            GenericMenu rightClickMenu = new GenericMenu();
                            rightClickMenu.AddItem(new GUIContent("Delete this connection"), false, DeleteConnection);
                            rightClickMenu.ShowAsContext();
                            rightClickMenu.ShowAsContext();
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

        private void ProcessNodeEvents(Event e, bool inPanel)
        {
            if (uiNodes != null)
            {
                NodeClickedOnThisUpdate = false;

                for (int i = 0; i < uiNodes.Count; i++)
                {
                    bool guiChanged = uiNodes[i].ProcessEvents(e, inPanel);
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
            newOption.ID = CurrentAsset.CurrentIDCounter++;

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
            newAction.ID = CurrentAsset.CurrentIDCounter++;

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


        /* -- Connecting Nodes -- */

        public void ConnectOptionToAction(UIOptionNode option)
        {
            // The option if what we are connecting
            m_currentConnectingNode = option;

            // Set the input state appropriately
            m_inputState = eInputState.ConnectingOptionToAction;
        }

        public void ConnectActionToOption(UIActionNode action)
        {
            // The option if what we are connecting
            m_currentConnectingNode = action;

            // Set the input state appropriately
            m_inputState = eInputState.ConnectingActionToOption;
        }


        /* -- Deleting Nodes -- */

        public void DeleteUINode(UINode node)
        {
            if (ConversationRoot == node.Info)
            {
                Log("Cannot delete root node.");
                return;
            }

            // Delete tree/internal objects
            node.Info.RemoveSelfFromTree();

            // Delete the UI classes
            uiNodes.Remove(node);
            node = null;
        }

        /* -- Deleting connection -- */

        public void DeleteConnection()
        {
            if (m_connectionDeleteParent != null && m_connectionDeleteChild != null)
            {
                // Remove parent relationship
                m_connectionDeleteChild.parents.Remove(m_connectionDeleteParent);

                // Remove child relationship
                if (m_connectionDeleteParent is ConversationAction)
                {
                    (m_connectionDeleteParent as ConversationAction).Options.Remove(
                        (m_connectionDeleteChild as ConversationOption));
                }
                else
                {
                    (m_connectionDeleteParent as ConversationOption).Action = null;
                }
            }

            // m_ConnectionDeleteA, m_connectionDeleteB
            m_connectionDeleteParent = null;
            m_connectionDeleteChild = null;
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
            CurrentlySelectedNode = null;
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

        private static void Log(string str)
        {
#if DIALOGUE_DEBUG || true
            Debug.Log("[DialogueEditor]: " + str);
#endif
        }




        //--------------------------------------
        // User / Save functionality
        //--------------------------------------

        public void Recenter()
        {
            if (ConversationRoot == null) { return; }

            // Calc delta to move head to (middle, 0) and then apply this to all nodes
            Vector2 target = new Vector2((position.width / 2) - (UIActionNode.Width / 2) - (panelWidth / 2), TOOLBAR_HEIGHT);
            Vector2 delta = target - new Vector2(ConversationRoot.EditorInfo.xPos, ConversationRoot.EditorInfo.yPos);
            for (int i = 0; i < uiNodes.Count; i++)
            {
                uiNodes[i].Drag(delta);
            }
        }

        private void Save(bool manual = false)
        {
            if (CurrentAsset != null)
            {
                Conversation conversation = new Conversation();

                // Give each node a uid
                // Prepare each node for serialization
                for (int i = 0; i < uiNodes.Count; i++)
                {
                    uiNodes[i].Info.PrepareForSerialization();
                }

                // Now that each node has a UID:
                // - Register the UIDs of their parents/children
                // - Add it to the conversation
                for (int i = 0; i < uiNodes.Count; i++)
                {
                    uiNodes[i].Info.RegisterUIDs();

                    if (uiNodes[i] is UIActionNode)
                    {
                        conversation.Actions.Add((uiNodes[i] as UIActionNode).ConversationNode);
                    }
                    else if (uiNodes[i] is UIOptionNode)
                    {
                        conversation.Options.Add((uiNodes[i] as UIOptionNode).OptionNode);
                    }
                }

                // Serialize
                CurrentAsset.Jsonify(conversation);

                // Null / clear everything. We aren't pointing to it anymore. 
                if (!manual)
                {
                    CurrentAsset = null;
                    while (uiNodes.Count != 0)
                        uiNodes.RemoveAt(0);
                    CurrentlySelectedNode = null;
                }
            }
        }
    }
}