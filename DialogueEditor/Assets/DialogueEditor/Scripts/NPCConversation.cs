using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    //--------------------------------------
    // Conversation Monobehaviour (Serialized)
    //--------------------------------------

    [System.Serializable]
    [DisallowMultipleComponent]
    public class NPCConversation : MonoBehaviour
    {
        // Serialized data
        [SerializeField] private string json;
        [SerializeField] public int CurrentIDCounter = 1;
        [SerializeField] public Sprite DefaultSprite;
        [SerializeField] public TMPro.TMP_FontAsset DefaultFont;
        [SerializeField] private List<NodeEventHolder> Events;

        // Runtime vars
        public UnityEngine.Events.UnityEvent Event;


        //--------------------------------------
        // Util
        //--------------------------------------

        public NodeEventHolder GetEventHolderForID(int id)
        {
            if (Events == null)
                Events = new List<NodeEventHolder>();

            for (int i = 0; i < Events.Count; i++)
                if (Events[i].NodeID == id)
                    return Events[i];

            NodeEventHolder h = this.gameObject.AddComponent<NodeEventHolder>();
            h.NodeID = id;
            h.Event = new UnityEngine.Events.UnityEvent();
            Events.Add(h);
            return h;
        }

        public void DeleteEventHolderForID(int id)
        {
            if (Events == null)
                return;

            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].NodeID == id)
                {
                    GameObject.DestroyImmediate(Events[i]);
                    Events.RemoveAt(i);
                }
            }
        }


        //--------------------------------------
        // Serialize and Deserialize
        //--------------------------------------

        public void Serialize(EditableConversation conversation)
        {
            json = Jsonify(conversation);
        }

        public Conversation Deserialize()
        {
            // Deserialize an editor-version (containing all info) that 
            // we will use to construct the user-facing Conversation data structure. 
            EditableConversation ec = this.DeserializeForEditor();

            // Create a conversation. 
            Conversation conversation = new Conversation();
            // Create a dictionary to store our created nodes by UID
            Dictionary<int, DialogueNode> dialogues = new Dictionary<int, DialogueNode>();
            Dictionary<int, OptionNode> options = new Dictionary<int, OptionNode>();

            // Create a Dialogue and Option node for each in the conversation
            // Put them in the dictionary
            for (int i = 0; i < ec.Actions.Count; i++)
            {
                DialogueNode node = new DialogueNode();
                node.Text = ec.Actions[i].Text;
                node.TMPFont = ec.Actions[i].TMPFont;
                node.Icon = ec.Actions[i].Icon;
                node.Audio = ec.Actions[i].Audio;
                node.Options = new List<OptionNode>();
                if (this.GetEventHolderForID(ec.Actions[i].ID) != null)
                {
                    node.Event = this.GetEventHolderForID(ec.Actions[i].ID).Event;
                }

                dialogues.Add(ec.Actions[i].ID, node);
            }
            for (int i = 0; i < ec.Options.Count; i++)
            {
                OptionNode node = new OptionNode();
                node.Text = ec.Options[i].Text;
                node.TMPFont = ec.Options[i].TMPFont;

                options.Add(ec.Options[i].ID, node);
            }

            // Now that we have every node in the dictionary, reconstruct the tree 
            // And also look for the root
            for (int i = 0; i < ec.Actions.Count; i++)
            {
                // Connect dialogue to options
                for (int j = 0; j < ec.Actions[i].OptionUIDs.Count; j++)
                {
                    dialogues[ec.Actions[i].ID].Options.Add(options[ec.Actions[i].OptionUIDs[j]]);
                }

                // Connect dialogue to following dialogue
                if (ec.Actions[i].ActionUID != EditableConversation.INVALID_UID)
                {
                    dialogues[ec.Actions[i].ID].Dialogue = dialogues[ec.Actions[i].ActionUID];
                }

                // Check if root
                if (ec.Actions[i].EditorInfo.isRoot)
                {
                    conversation.Root = dialogues[ec.Actions[i].ID];
                }
            }
            for (int i = 0; i < ec.Options.Count; i++)
            {
                // Connect option to following dialogue
                options[ec.Options[i].ID].Dialogue = dialogues[ec.Options[i].ActionUID];
            }

            return conversation;
        }

        public EditableConversation DeserializeForEditor()
        {
            // Dejsonify 
            EditableConversation conversation = Dejsonify();

            if (conversation != null)
            {
                // Deserialize the indivudual nodes
                {
                    if (conversation.Actions != null)
                        for (int i = 0; i < conversation.Actions.Count; i++)
                            conversation.Actions[i].Deserialize();

                    if (conversation.Options != null)
                        for (int i = 0; i < conversation.Options.Count; i++)
                            conversation.Options[i].Deserialize();
                }
            }

            // Clear our dummy event
            Event = new UnityEngine.Events.UnityEvent();

            return conversation;
        }



        //--------------------------------------
        // Serialize and Deserialize
        //--------------------------------------

        private string Jsonify(EditableConversation conversation)
        {
            if (conversation == null || conversation.Options == null) { return ""; }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(EditableConversation));
            ser.WriteObject(ms, conversation);
            byte[] jsonData = ms.ToArray();
            ms.Close();
            string toJson = System.Text.Encoding.UTF8.GetString(jsonData, 0, jsonData.Length);

            return toJson;
        }

        private EditableConversation Dejsonify()
        {
            if (json == null || json == "")
                return null;

            EditableConversation conversation = new EditableConversation();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(conversation.GetType());
            conversation = ser.ReadObject(ms) as EditableConversation;
            ms.Close();

            return conversation;
        }
    }



    //--------------------------------------
    // Conversation C# class - For use by user (Deserialized)
    //--------------------------------------

    public class Conversation
    {
        public DialogueNode Root;

        public Sprite DefaultIcon;
        public TMPro.TMP_FontAsset DefaultTMPFont;
    }

    public abstract class ConversationNode
    {
        public string Text;
        public TMPro.TMP_FontAsset TMPFont;
    }

    public class DialogueNode : ConversationNode
    {
        public Sprite Icon;
        public AudioClip Audio;
        public List<OptionNode> Options;
        public DialogueNode Dialogue;
        public UnityEngine.Events.UnityEvent Event;
    }

    public class OptionNode : ConversationNode
    {
        public DialogueNode Dialogue;
    }



    //--------------------------------------
    // Conversation C# class - For use in editor (Deserialized)
    //--------------------------------------

    [DataContract]
    public class EditableConversation
    {
        public const int INVALID_UID = -1;

        public EditableConversation()
        {
            Actions = new List<EditableSpeechNode>();
            Options = new List<EditableOptionNode>();
        }

        [DataMember]
        public List<EditableSpeechNode> Actions;

        [DataMember]
        public List<EditableOptionNode> Options;

        public EditableSpeechNode GetRootNode()
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i].EditorInfo.isRoot)
                    return Actions[i];
            }
            return null;
        }

        public EditableConversationNode GetNodeByUID(int uid)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions[i].ID == uid)
                    return Actions[i];

            for (int i = 0; i < Options.Count; i++)
                if (Options[i].ID == uid)
                    return Options[i];

            return null;
        }

        public EditableSpeechNode GetActionByUID(int uid)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions[i].ID == uid)
                    return Actions[i];

            return null;
        }

        public EditableOptionNode GetOptionByUID(int uid)
        {
            for (int i = 0; i < Options.Count; i++)
                if (Options[i].ID == uid)
                    return Options[i];

            return null;
        }
    }

    //--------------------------------------
    // Abstract Node class
    //--------------------------------------

    [DataContract]
    public abstract class EditableConversationNode
    {
        /// <summary> Info used internally by the editor window. </summary>
        [DataContract]
        public class EditorArgs
        {
            [DataMember]
            public float xPos;

            [DataMember]
            public float yPos;

            [DataMember]
            public bool isRoot;
        }

        public EditableConversationNode()
        {
            parents = new List<EditableConversationNode>();
            parentUIDs = new List<int>();
            EditorInfo = new EditorArgs { xPos = 0, yPos = 0, isRoot = false };
        }

        /// <summary> Info used internally by the editor window. </summary>
        [DataMember]
        public EditorArgs EditorInfo;

        [DataMember]
        public int ID;

        [DataMember]
        public string Text;

        /// <summary>
        /// TextMeshPro font 
        /// </summary>
        public TMPro.TMP_FontAsset TMPFont;
        [DataMember] public string TMPFontGUID;

        [DataMember]
        public List<int> parentUIDs;

        public List<EditableConversationNode> parents;

        public abstract void RemoveSelfFromTree();
        public abstract void RegisterUIDs();

        public virtual void PrepareForSerialization()
        {
            string guid;
            long li;

            if (TMPFont != null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(TMPFont, out guid, out li))
                    TMPFontGUID = guid;
            }
            else
                TMPFontGUID = "";
        }

        public virtual void Deserialize()
        {
            if (!string.IsNullOrEmpty(TMPFontGUID))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(TMPFontGUID);
                TMPFont = (TMPro.TMP_FontAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(TMPro.TMP_FontAsset));
            }
        }
    }

    [DataContract]
    public class EditableSpeechNode : EditableConversationNode
    {
        public EditableSpeechNode() : base()
        {
            Options = new List<EditableOptionNode>();
            OptionUIDs = new List<int>();
            ActionUID = EditableConversation.INVALID_UID;
        }

        /// <summary>
        /// The selectable options of this Action.
        /// </summary>
        public List<EditableOptionNode> Options;
        [DataMember] public List<int> OptionUIDs;

        /// <summary>
        /// The Action this Action leads onto (if no options).
        /// </summary>
        public EditableSpeechNode Action;
        [DataMember] public int ActionUID;

        /// <summary>
        /// The NPC Icon
        /// </summary>
        public Sprite Icon;
        [DataMember] public string IconGUID;

        /// <summary>
        /// The Audio Clip acompanying this Action.
        /// </summary>
        public AudioClip Audio;
        [DataMember] public string AudioGUID;

        public void AddOption(EditableOptionNode newOption)
        {
            if (Options == null)
                Options = new List<EditableOptionNode>();

            if (Options.Contains(newOption))
                return;

            // Delete the action I point to, if any
            if (this.Action != null)
            {
                this.Action.parents.Remove(this);
            }
            this.Action = null;

            newOption.parents.Add(this);
            Options.Add(newOption);
        }

        public void SetAction(EditableSpeechNode newAction)
        {
            // Remove myself as a parent from the action I was previously pointing to
            if (this.Action != null)
            {
                this.Action.parents.Remove(this);
            }

            // Remove any options I may have
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    // I am no longer the parents of these options
                    Options[i].parents.Remove(this);
                }
                Options.Clear();
            }

            this.Action = newAction;
            newAction.parents.Add(this);
        }

        public override void RemoveSelfFromTree()
        {
            // This action is no longer the parents resulting action
            for (int i = 0; i < parents.Count; i++)
                (parents[i] as EditableOptionNode).Action = null;

            // This action is no longer the parent of any children options
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    Options[i].parents.Clear();
                }
            }
        }

        public override void RegisterUIDs()
        {
            if (parentUIDs != null)
                parentUIDs.Clear();
            parentUIDs = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                parentUIDs.Add(parents[i].ID);
            }

            if (OptionUIDs != null)
                OptionUIDs.Clear();
            OptionUIDs = new List<int>();
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    OptionUIDs.Add(Options[i].ID);
                }
            }

            if (Action != null)
                ActionUID = Action.ID;
            else
                ActionUID = EditableConversation.INVALID_UID;
        }

        public override void PrepareForSerialization()
        {
            base.PrepareForSerialization();

            string guid;
            long li;

            if (Audio != null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Audio, out guid, out li))
                    AudioGUID = guid;
            }
            else
                AudioGUID = "";

            if (Icon != null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Icon, out guid, out li))
                    IconGUID = guid;
            }
            else
                IconGUID = "";
        }

        public override void Deserialize()
        {
            base.Deserialize();

            if (!string.IsNullOrEmpty(AudioGUID))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(AudioGUID);
                Audio = (AudioClip)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(AudioClip));
            }

            if (!string.IsNullOrEmpty(IconGUID))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(IconGUID);
                Icon = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));
            }
        }
    }

    [DataContract]
    public class EditableOptionNode : EditableConversationNode
    {
        public EditableOptionNode() : base()
        {
            ActionUID = EditableConversation.INVALID_UID;
        }

        /// <summary>
        /// The Action this option leads to.
        /// </summary>        
        public EditableSpeechNode Action;

        [DataMember]
        public int ActionUID;

        public void SetAction(EditableSpeechNode newAction)
        {
            // Remove myself as a parent from the action I was previously pointing to
            if (this.Action != null)
            {
                this.Action.parents.Remove(this);
            }

            this.Action = newAction;
            newAction.parents.Add(this);
        }

        public override void RemoveSelfFromTree()
        {
            // This option is no longer part of any parents actions possible options
            for (int i = 0; i < parents.Count; i++)
            {
                (parents[i] as EditableSpeechNode).Options.Remove(this);
            }

            // This option is no longer the parent to its child action
            if (Action != null)
            {
                Action.parents.Remove(this);
            }
        }

        public override void RegisterUIDs()
        {
            if (parentUIDs != null)
                parentUIDs.Clear();
            parentUIDs = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                parentUIDs.Add(parents[i].ID);
            }

            ActionUID = EditableConversation.INVALID_UID;
            if (Action != null)
                ActionUID = Action.ID;
        }
    }
}