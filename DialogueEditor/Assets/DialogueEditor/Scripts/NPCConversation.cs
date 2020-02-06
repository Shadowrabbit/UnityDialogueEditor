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

        public void Serialize(Conversation conversation)
        {
            json = Jsonify(conversation);
        }

        public Conversation Deserialize()
        {
            // Dejsonify 
            Conversation conversation = Dejsonify();

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

        private string Jsonify(Conversation conversation)
        {
            if (conversation == null || conversation.Options == null) { return ""; }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Conversation));
            ser.WriteObject(ms, conversation);
            byte[] jsonData = ms.ToArray();
            ms.Close();
            string toJson = System.Text.Encoding.UTF8.GetString(jsonData, 0, jsonData.Length);

            return toJson;
        }

        private Conversation Dejsonify()
        {
            if (json == null || json == "")
                return null;

            Conversation conversation = new Conversation();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(conversation.GetType());
            conversation = ser.ReadObject(ms) as Conversation;
            ms.Close();

            return conversation;
        }
    }



    //--------------------------------------
    // Conversation C# class (Deserialized)
    //--------------------------------------

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



    //--------------------------------------
    // Abstract Node class
    //--------------------------------------

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

        public List<ConversationNode> parents;

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
    public class ConversationAction : ConversationNode
    {
        public ConversationAction() : base()
        {
            Options = new List<ConversationOption>();
            OptionUIDs = new List<int>();
            ActionUID = Conversation.INVALID_UID;
        }

        /// <summary>
        /// The selectable options of this Action.
        /// </summary>
        public List<ConversationOption> Options;
        [DataMember] public List<int> OptionUIDs;

        /// <summary>
        /// The Action this Action leads onto (if no options).
        /// </summary>
        public ConversationAction Action;
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

        public void AddOption(ConversationOption newOption)
        {
            if (Options == null)
                Options = new List<ConversationOption>();

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

        public void SetAction(ConversationAction newAction)
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

            if (Action != null)
                ActionUID = Action.ID;
            else
                ActionUID = Conversation.INVALID_UID;
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
    public class ConversationOption : ConversationNode
    {
        public ConversationOption() : base()
        {
            ActionUID = Conversation.INVALID_UID;
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
    }
}