﻿using System.Collections;
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
            ConnectingNodeToAction      = 2,
            PlacingAction               = 3,
            ConnectingActionToOption    = 4,
            draggingPanel               = 5,
        }

        // Consts
        public const float TOOLBAR_HEIGHT = 17;
        public const float START_PANEL_WIDTH = 250;
        private const string WINDOW_NAME = "DIALOGUE_EDITOR_WINDOW";
        private const string HELP_URL = "https://github.com/JosephBarber96/UnityDialogueEditor";

        // Static properties
        public static bool NodeClickedOnThisUpdate { get; set; }
        private static UINode CurrentlySelectedNode { get; set; }

        // Private variables:     
        private NPCConversation CurrentAsset;           // The Conversation scriptable object that is currently being viewed/edited
        public static EditableSpeechNode ConversationRoot { get; private set; }    // The root node of the conversation
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
        private EditableConversationNode m_connectionDeleteParent, m_connectionDeleteChild;




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
            EditableConversation conversation = CurrentAsset.DeserializeForEditor();
            if (conversation == null)
                conversation = new EditableConversation();
            ConversationRoot = conversation.GetRootNode();

            // If it's null, create a root
            if (ConversationRoot == null)
            {
                ConversationRoot = new EditableSpeechNode();
                ConversationRoot.EditorInfo.xPos = (Screen.width / 2) - (UISpeechNode.Width / 2);
                ConversationRoot.EditorInfo.yPos = 0;
                ConversationRoot.EditorInfo.isRoot = true;
                conversation.Actions.Add(ConversationRoot);
            }

            // Get a list of every node in the conversation
            List<EditableConversationNode> allNodes = new List<EditableConversationNode>();
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

                allNodes[i].parents = new List<EditableConversationNode>();
                for (int j = 0; j < allNodes[i].parentUIDs.Count; j++)
                {
                    allNodes[i].parents.Add(conversation.GetNodeByUID(allNodes[i].parentUIDs[j]));
                }

                if (allNodes[i] is EditableSpeechNode)
                {
                    // Action options
                    int count = (allNodes[i] as EditableSpeechNode).OptionUIDs.Count;
                    (allNodes[i] as EditableSpeechNode).Options = new List<EditableOptionNode>();
                    for (int j = 0; j < count; j++)
                    {
                        (allNodes[i] as EditableSpeechNode).Options.Add(
                            conversation.GetOptionByUID((allNodes[i] as EditableSpeechNode).OptionUIDs[j]));
                    }

                    // Action following action
                    (allNodes[i] as EditableSpeechNode).Action = conversation.
                        GetActionByUID((allNodes[i] as EditableSpeechNode).ActionUID);
                }
                else if (allNodes[i] is EditableOptionNode)
                {
                    (allNodes[i] as EditableOptionNode).Action = 
                        conversation.GetActionByUID((allNodes[i] as EditableOptionNode).ActionUID);
                }
            }

            // For every node: 
            // 1: Create a corresponding UI Node to represent it, and add it to the list
            // 2: Tell any of the nodes children that the node is the childs parent
            for (int i = 0; i < allNodes.Count; i++)
            {
                EditableConversationNode node = allNodes[i];

                if (node is EditableSpeechNode)
                {
                    // 1
                    UISpeechNode uiNode = new UISpeechNode(node,
                        new Vector2(node.EditorInfo.xPos, node.EditorInfo.yPos));

                    uiNodes.Add(uiNode);

                    // 2
                    EditableSpeechNode action = node as EditableSpeechNode;
                    if (action.Options != null)
                        for (int j = 0; j < action.Options.Count; j++)
                            action.Options[j].parents.Add(action);

                    if (action.Action != null)
                        action.Action.parents.Add(action);
                }
                else
                {
                    // 1
                    UIOptionNode uiNode = new UIOptionNode(node,
                        new Vector2(node.EditorInfo.xPos, node.EditorInfo.yPos));

                    uiNodes.Add(uiNode);

                    // 2
                    EditableOptionNode option = node as EditableOptionNode;
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
            UISpeechNode.OnCreateOption += CreateNewOption;
            UIOptionNode.OnCreateAction += CreateNewAction;
            UISpeechNode.OnConnectToOption += ConnectActionToOption;
            UIOptionNode.OnConnectToAction += ConnectOptionToAction;

            this.name = WINDOW_NAME;
            panelWidth = START_PANEL_WIDTH;
        }

        private void InitGUIStyles()
        {
            // Panel style
            panelStyle = new GUIStyle();
            panelStyle.normal.background = new Texture2D(10, 10);
            panelStyle.normal.background.SetPixel(0, 0, EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255));

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
            UISpeechNode.OnCreateOption -= CreateNewOption;
            UIOptionNode.OnCreateAction -= CreateNewAction;
            UISpeechNode.OnConnectToOption -= ConnectActionToOption;
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
            if (GUILayout.Button("Reset panel", EditorStyles.toolbarButton))
            {
                ResetPanelSize();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
            {
                Application.OpenURL(HELP_URL);
            }
            /*
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Save(true);
            }
            */
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

            if (m_inputState == eInputState.ConnectingNodeToAction || 
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
                Vector3 start = new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset;
                if (start.x > panelRect.x)
                    continue;
                Vector3 end = new Vector3(gridSpacing * i, position.height, 0f) + newOffset;
                Handles.DrawLine(start, end);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Vector3 start = new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset;
                Vector3 end = new Vector3(position.width, gridSpacing * j, 0f) + newOffset;
                if (end.x > panelRect.x)
                    end.x = panelRect.x;
                Handles.DrawLine(start, end);
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

            GUILayout.Space(10);

            if (CurrentlySelectedNode != null)
            {
                bool differentNodeSelected = (m_cachedSelectedNode != CurrentlySelectedNode);
                m_cachedSelectedNode = CurrentlySelectedNode;

                if (CurrentlySelectedNode is UISpeechNode)
                {
                    EditableSpeechNode node = (CurrentlySelectedNode.Info as EditableSpeechNode);
                    GUILayout.Label("[" + node.ID + "] NPC Dialogue Node.", panelTitleStyle);
                    
                    GUILayout.Label("Dialogue", EditorStyles.boldLabel);
                    node.Text = GUILayout.TextArea(node.Text);

                    GUILayout.Label("Icon", EditorStyles.boldLabel);
                    node.Icon = (Sprite)EditorGUILayout.ObjectField(node.Icon, typeof(Sprite), false);

                    GUILayout.Label("Audio", EditorStyles.boldLabel);
                    node.Audio = (AudioClip)EditorGUILayout.ObjectField(node.Audio, typeof(AudioClip), false);

                    GUILayout.Label("TMP Font", EditorStyles.boldLabel);
                    node.TMPFont = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField(node.TMPFont, typeof(TMPro.TMP_FontAsset), false);

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
                    EditableOptionNode node = (CurrentlySelectedNode.Info as EditableOptionNode);
                    GUILayout.Label("[" + node.ID + "] Option Node.", panelTitleStyle);
                    

                    GUILayout.Label("Option text:", EditorStyles.boldLabel);
                    node.Text = GUILayout.TextArea(node.Text);

                    GUILayout.Label("TMP Font", EditorStyles.boldLabel);
                    node.TMPFont = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField(node.TMPFont, typeof(TMPro.TMP_FontAsset), false);
                }
            }
            else
            {
                GUILayout.Label("Conversation options.", panelTitleStyle);

                GUILayout.Label("Main Icon:", EditorStyles.boldLabel);
                CurrentAsset.DefaultSprite = (Sprite)EditorGUILayout.ObjectField(CurrentAsset.DefaultSprite, typeof(Sprite), false);

                GUILayout.Label("Main font:", EditorStyles.boldLabel);
                CurrentAsset.DefaultFont = (TMPro.TMP_FontAsset)EditorGUILayout.ObjectField(CurrentAsset.DefaultFont, typeof(TMPro.TMP_FontAsset), false);
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
                    bool inPanel = panelRect.Contains(e.mousePosition) || e.mousePosition.y < TOOLBAR_HEIGHT;
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

                case eInputState.ConnectingNodeToAction:

                    // Left click
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        for (int i = 0; i < uiNodes.Count; i++)
                        {
                            if (uiNodes[i] == m_currentConnectingNode)
                                continue;

                            if (!(uiNodes[i] is UISpeechNode))
                                continue;

                            if (uiNodes[i].rect.Contains(e.mousePosition))
                            {
                                if (m_currentConnectingNode is UIOptionNode)
                                {
                                    (m_currentConnectingNode as UIOptionNode).OptionNode.
                                        SetAction((uiNodes[i] as UISpeechNode).ConversationNode);
                                    break;
                                }
                                else if (m_currentConnectingNode is UISpeechNode)
                                {
                                    UISpeechNode connecting = m_currentConnectingNode as UISpeechNode;
                                    UISpeechNode toBeChild = uiNodes[i] as UISpeechNode;

                                    // If a relationship between these actions already exists, swap it 
                                    // around, as a 2way action<->action relationship cannot exist.
                                    if (connecting.ConversationNode.parents.Contains(toBeChild.ConversationNode))
                                    {
                                        // Remove the relationship
                                        connecting.ConversationNode.parents.Remove(toBeChild.ConversationNode);
                                        toBeChild.ConversationNode.Action = null;
                                    }

                                    (m_currentConnectingNode as UISpeechNode).ConversationNode.
                                        SetAction((uiNodes[i] as UISpeechNode).ConversationNode);
                                    break;
                                }
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
                                (m_currentConnectingNode as UISpeechNode).ConversationNode.AddOption(
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
                        else if (e.mousePosition.y > TOOLBAR_HEIGHT)
                        {
                            clickInBox = false;
                            if (!DialogueEditorWindow.NodeClickedOnThisUpdate)
                            {
                                UnselectNode();
                                e.Use();
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

        public void CreateNewOption(UISpeechNode actionUI)
        {
            // Create new option, the argument action is the options parent
            EditableOptionNode newOption = new EditableOptionNode();
            newOption.ID = CurrentAsset.CurrentIDCounter++;

            // Give the action it's default values
            newOption.TMPFont = CurrentAsset.DefaultFont;

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


        public void CreateNewAction(UINode node)
        {
            // Create new action, the argument option is the actions parent
            EditableSpeechNode newAction = new EditableSpeechNode();
            newAction.ID = CurrentAsset.CurrentIDCounter++;

            // Give the action it's default values
            newAction.Icon = CurrentAsset.DefaultSprite;
            newAction.TMPFont = CurrentAsset.DefaultFont;

            // Set this new action as the options child
            if (node is UIOptionNode)
                (node as UIOptionNode).OptionNode.SetAction(newAction);
            else if (node is UISpeechNode)
                (node as UISpeechNode).ConversationNode.SetAction(newAction);

            // This new action doesn't have any children yet
            newAction.Options = null;

            // Create a new UI object to represent the new action
            UISpeechNode ui = new UISpeechNode(newAction, Vector2.zero);
            uiNodes.Add(ui);

            // Set the input state appropriately
            m_inputState = eInputState.PlacingAction;
            m_currentPlacingNode = ui;
        }


        /* -- Connecting Nodes -- */

        public void ConnectOptionToAction(UINode option)
        {
            // The option if what we are connecting
            m_currentConnectingNode = option;

            // Set the input state appropriately
            m_inputState = eInputState.ConnectingNodeToAction;
        }

        public void ConnectActionToOption(UISpeechNode action)
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

            // Delete the EventHolder script if it's an action node
            if (node is UISpeechNode)
            {
                CurrentAsset.DeleteEventHolderForID(node.Info.ID);
            }

            // Delete the UI classes
            uiNodes.Remove(node);
            node = null;

            // "Unselect" what we were looking at.
            CurrentlySelectedNode = null;
        }

        /* -- Deleting connection -- */

        public void DeleteConnection()
        {
            if (m_connectionDeleteParent != null && m_connectionDeleteChild != null)
            {
                // Remove parent relationship
                m_connectionDeleteChild.parents.Remove(m_connectionDeleteParent);

                // Remove child relationship
                if (m_connectionDeleteParent is EditableSpeechNode)
                {
                    (m_connectionDeleteParent as EditableSpeechNode).Options.Remove(
                        (m_connectionDeleteChild as EditableOptionNode));
                }
                else
                {
                    (m_connectionDeleteParent as EditableOptionNode).Action = null;
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

        private void Recenter()
        {
            if (ConversationRoot == null) { return; }

            // Calc delta to move head to (middle, 0) and then apply this to all nodes
            Vector2 target = new Vector2((position.width / 2) - (UISpeechNode.Width / 2) - (panelWidth / 2), TOOLBAR_HEIGHT);
            Vector2 delta = target - new Vector2(ConversationRoot.EditorInfo.xPos, ConversationRoot.EditorInfo.yPos);
            for (int i = 0; i < uiNodes.Count; i++)
            {
                uiNodes[i].Drag(delta);
            }
        }

        private void ResetPanelSize()
        {
            panelWidth = START_PANEL_WIDTH;
        }

        private void Save(bool manual = false)
        {
            if (CurrentAsset != null)
            {
                EditableConversation conversation = new EditableConversation();

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

                    if (uiNodes[i] is UISpeechNode)
                    {
                        conversation.Actions.Add((uiNodes[i] as UISpeechNode).ConversationNode);
                    }
                    else if (uiNodes[i] is UIOptionNode)
                    {
                        conversation.Options.Add((uiNodes[i] as UIOptionNode).OptionNode);
                    }
                }

                // Serialize
                CurrentAsset.Serialize(conversation);

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