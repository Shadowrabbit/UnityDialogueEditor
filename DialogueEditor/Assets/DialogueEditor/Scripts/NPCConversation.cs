using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    public enum eNodeType
    {
        Dialogue,
        Action
    }

    public class SimpleEvent : ScriptableObject
    {
        public UnityEngine.Events.UnityEvent Event;
    }

    public class SimpleEventEditor : UnityEditor.EditorWindow
    {
        private UnityEditor.SerializedObject serializedObject;
        private SimpleEvent Event;
    }

    [DataContract]
    public class Conversation
    {
        public const int INVALID_UID = -1;

        public Conversation()
        {
            Actions = new List<ConversationAction>();
            Options = new List<ConversationOption>();
        }

        [DataMember]
        public List<ConversationAction> Actions;

        [DataMember]
        public List<ConversationOption> Options;

        public ConversationAction GetRootNode()
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i].EditorInfo.isRoot)
                    return Actions[i];
            }
            return null;
        }

        public ConversationNode GetNodeByUID(int uid)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions[i].ID == uid)
                    return Actions[i];

            for (int i = 0; i < Options.Count; i++)
                if (Options[i].ID == uid)
                    return Options[i];

            return null;
        }

        public ConversationAction GetActionByUID(int uid)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions[i].ID == uid)
                    return Actions[i];

            return null;
        }

        public ConversationOption GetOptionByUID(int uid)
        {
            for (int i = 0; i < Options.Count; i++)
                if (Options[i].ID == uid)
                    return Options[i];

            return null;
        }
    }

    [DataContract]
    public abstract class ConversationNode
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

        public ConversationNode()
        {
            parents = new List<ConversationNode>();
            parentUIDs = new List<int>();
            EditorInfo = new EditorArgs { xPos = 0, yPos = 0, isRoot = false };
        }

        /// <summary> Info used internally by the editor window. </summary>
        [DataMember]
        public EditorArgs EditorInfo;

        [DataMember]
        public string Text;

        [DataMember]
        public int ID;

        public List<ConversationNode> parents;

        [DataMember]
        public List<int> parentUIDs;

        public SimpleEvent EventHolder;
     
        public abstract void RemoveSelfFromTree();
        public abstract void RegisterUIDs();
        public abstract void PrepareForSerialization();
        public abstract void Deserialize();
    }

    [DataContract]
    public class ConversationAction : ConversationNode
    {
        public ConversationAction() : base()
        {
            Options = new List<ConversationOption>();
            OptionUIDs = new List<int>();
        }

        /// <summary>
        /// The selectable options of this Action.
        /// </summary>
        public List<ConversationOption> Options;
        [DataMember] public List<int> OptionUIDs;

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

        public void AddOption(ConversationOption newOption)
        {
            if (Options == null)
                Options = new List<ConversationOption>();

            if (Options.Contains(newOption))
                return;

            newOption.parents.Add(this);
            Options.Add(newOption);
        }

        public override void RemoveSelfFromTree()
        {
            // This action is no longer the parents resulting action
            for (int i = 0; i < parents.Count; i++)
                (parents[i] as ConversationOption).Action = null;

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
        }

        public override void PrepareForSerialization()
        {
            string guid;
            long li;

            if (Audio != null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Audio, out guid, out li))
                    AudioGUID = guid;
            }

            if (Icon != null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Icon, out guid, out li))
                    IconGUID = guid;
            }
        }

        public override void Deserialize()
        {
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
    public class ConversationOption : ConversationNode
    {
        public ConversationOption() : base()
        {

        }

        /// <summary>
        /// The Action this option leads to.
        /// </summary>        
        public ConversationAction Action;

        [DataMember]
        public int ActionUID;

        public void SetAction(ConversationAction newAction)
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
                (parents[i] as ConversationAction).Options.Remove(this);
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

            ActionUID = Conversation.INVALID_UID;
            if (Action != null)
                ActionUID = Action.ID;
        }

        public override void PrepareForSerialization()
        {

        }

        public override void Deserialize()
        {

        }
    }

    [System.Serializable]
    public class NodeEventHolder : ScriptableObject
    {
        [SerializeField]
        public int NodeID;
        [SerializeField]
        public UnityEngine.Events.UnityEvent Event;
    }


    //--------------------------------------
    // Scriptable and Serialization
    //--------------------------------------

    [CreateAssetMenu(fileName = "NPC_Conversation", menuName = "DialogueEditor/Conversation")]
    [System.Serializable]
    public class NPCConversation : ScriptableObject
    {
        [SerializeField]
        public string json;

        [SerializeField]
        [HideInInspector]
        public List<NodeEventHolder> Events = new List<NodeEventHolder>();

        [HideInInspector]
        public UnityEngine.Events.UnityEvent Event;

        [SerializeField]
        [HideInInspector]
        public int CurrentIDCounter = 1;

        public NodeEventHolder GetEventHolderForID(int id)
        {
            if (Events == null)
                Events = new List<NodeEventHolder>();

            for (int i = 0; i < Events.Count; i++)
                if (Events[i].NodeID == id)
                    return Events[i];

            NodeEventHolder h = ScriptableObject.CreateInstance<NodeEventHolder>();
            h.NodeID = id;
            h.Event = new UnityEngine.Events.UnityEvent();
            Events.Add(h);
            return h;
        }

        public void Serialize(Conversation conversation)
        {
            if (conversation == null || conversation.Options == null) { return; }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Conversation));
            ser.WriteObject(ms, conversation);
            byte[] jsonData = ms.ToArray();
            ms.Close();
            json = System.Text.Encoding.UTF8.GetString(jsonData, 0, jsonData.Length);
        }

        public Conversation GetDeserialized()
        {
            if (json == null || json == "")
                return null;

            Conversation conversation = new Conversation();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(conversation.GetType());
            conversation = ser.ReadObject(ms) as Conversation;
            ms.Close();
            // Deserialize the indivudual nodes
            {
                if (conversation.Actions != null)
                    for (int i = 0; i < conversation.Actions.Count; i++)
                        conversation.Actions[i].Deserialize();

                if (conversation.Options != null)
                    for (int i = 0; i < conversation.Options.Count; i++)
                        conversation.Options[i].Deserialize();
            }
            // Clear our dummy event
            Event = new UnityEngine.Events.UnityEvent();

            return conversation;
        }
    }
}