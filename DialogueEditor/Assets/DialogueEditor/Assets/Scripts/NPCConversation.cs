using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
        /// <summary> Version 1.03 </summary>
        public const int CurrentVersion = 103;

        private readonly string CHILD_NAME = "ConversationEventInfo";

        // Serialized data
        [SerializeField] public int CurrentIDCounter = 1;
        [SerializeField] private string json;
        [SerializeField] private int saveVersion;
        [SerializeField] public string DefaultName;
        [SerializeField] public Sprite DefaultSprite;
        [SerializeField] public TMPro.TMP_FontAsset DefaultFont;
        [FormerlySerializedAs("Events")]
        [SerializeField] private List<NodeEventHolder> NodeSerializedDataList;

        // Runtime vars
        public UnityEngine.Events.UnityEvent Event;
        public List<Parameter> ParameterList; // Serialized into the json string

        public int Version { get { return saveVersion; } }


        //--------------------------------------
        // Util
        //--------------------------------------

        public NodeEventHolder GetNodeData(int id)
        {
            // Create list if none
            if (NodeSerializedDataList == null)
                NodeSerializedDataList = new List<NodeEventHolder>();

            // Look through list to find by ID
            for (int i = 0; i < NodeSerializedDataList.Count; i++)
                if (NodeSerializedDataList[i].NodeID == id)
                    return NodeSerializedDataList[i];

            // If none exist, create a new GameObject
            Transform EventInfo = this.transform.Find(CHILD_NAME);
            if (EventInfo == null)
            {
                GameObject obj = new GameObject(CHILD_NAME);
                obj.transform.SetParent(this.transform);
            }
            EventInfo = this.transform.Find(CHILD_NAME);

            // Add a new Component for this node
            NodeEventHolder h = EventInfo.gameObject.AddComponent<NodeEventHolder>();
            h.NodeID = id;
            h.Event = new UnityEngine.Events.UnityEvent();
            NodeSerializedDataList.Add(h);
            return h;
        }

        public void DeleteDataForNode(int id)
        {
            if (NodeSerializedDataList == null)
                return;

            for (int i = 0; i < NodeSerializedDataList.Count; i++)
            {
                if (NodeSerializedDataList[i].NodeID == id)
                {
                    GameObject.DestroyImmediate(NodeSerializedDataList[i]);
                    NodeSerializedDataList.RemoveAt(i);
                }
            }
        }

        public Parameter GetParameter(string name)
        {
            for (int i = 0; i < this.ParameterList.Count; i++)
            {
                if (ParameterList[i].ParameterName == name)
                {
                    return ParameterList[i];
                }
            }
            return null;
        }


        //--------------------------------------
        // Serialize and Deserialize
        //--------------------------------------

        public void Serialize(EditableConversation conversation)
        {
            conversation.Parameters = this.ParameterList;
            json = Jsonify(conversation);
            saveVersion = CurrentVersion;
        }

        public Conversation Deserialize()
        {
            // Deserialize an editor-version (containing all info) that 
            // we will use to construct the user-facing Conversation data structure. 
            EditableConversation ec = this.DeserializeForEditor();

            // Create a conversation object
            Conversation conversation = new Conversation();

            // Construct the parameters
            for (int i = 0; i < ec.Parameters.Count; i++)
            {
                if (ec.Parameters[i] is BoolParameter)
                {
                    BoolParameter boolParam = ec.Parameters[i] as BoolParameter;
                    conversation.Parameters.Add(boolParam);
                }
                else if (ec.Parameters[i] is IntParameter)
                {
                    IntParameter intParam = ec.Parameters[i] as IntParameter;
                    conversation.Parameters.Add(intParam);
                }
            }

            // Create a dictionary to store our created nodes by UID
            Dictionary<int, SpeechNode> dialogues = new Dictionary<int, SpeechNode>();
            Dictionary<int, OptionNode> options = new Dictionary<int, OptionNode>();

            // Create a Dialogue and Option node for each in the conversation
            // Put them in the dictionary
            for (int i = 0; i < ec.SpeechNodes.Count; i++)
            {
                SpeechNode node = new SpeechNode();
                node.Name = ec.SpeechNodes[i].Name;
                node.Text = ec.SpeechNodes[i].Text;
                node.AutomaticallyAdvance = ec.SpeechNodes[i].AdvanceDialogueAutomatically;
                node.AutoAdvanceShouldDisplayOption = ec.SpeechNodes[i].AutoAdvanceShouldDisplayOption;
                node.TimeUntilAdvance = ec.SpeechNodes[i].TimeUntilAdvance;
                node.TMPFont = ec.SpeechNodes[i].TMPFont;
                node.Icon = ec.SpeechNodes[i].Icon;
                node.Audio = ec.SpeechNodes[i].Audio;
                node.Volume = ec.SpeechNodes[i].Volume;
                node.Options = new List<OptionNode>();
                if (this.GetNodeData(ec.SpeechNodes[i].ID) != null)
                {
                    node.Event = this.GetNodeData(ec.SpeechNodes[i].ID).Event;
                }

                dialogues.Add(ec.SpeechNodes[i].ID, node);
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
            for (int i = 0; i < ec.SpeechNodes.Count; i++)
            {
                // Connect dialogue to options
                for (int j = 0; j < ec.SpeechNodes[i].OptionUIDs.Count; j++)
                {
                    dialogues[ec.SpeechNodes[i].ID].Options.Add(options[ec.SpeechNodes[i].OptionUIDs[j]]);
                }

                // Connect dialogue to following dialogue
                if (ec.SpeechNodes[i].SpeechUID != EditableConversation.INVALID_UID)
                {
                    dialogues[ec.SpeechNodes[i].ID].Dialogue = dialogues[ec.SpeechNodes[i].SpeechUID];
                }

                // Check if root
                if (ec.SpeechNodes[i].EditorInfo.isRoot)
                {
                    conversation.Root = dialogues[ec.SpeechNodes[i].ID];
                }
            }


            for (int i = 0; i < ec.Options.Count; i++)
            {
                // Connect option to following dialogue
                if (dialogues.ContainsKey(ec.Options[i].SpeechUID))
                {
                    options[ec.Options[i].ID].Dialogue = dialogues[ec.Options[i].SpeechUID];
                }
            }

            return conversation;
        }



        public EditableConversation DeserializeForEditor()
        {
            // Dejsonify 
            EditableConversation conversation = Dejsonify();
            this.ParameterList = conversation.Parameters;

            if (conversation != null)
            {
                // Deserialize the indivudual nodes
                {
                    if (conversation.SpeechNodes != null)
                        for (int i = 0; i < conversation.SpeechNodes.Count; i++)
                            conversation.SpeechNodes[i].Deserialize(this);

                    if (conversation.Options != null)
                        for (int i = 0; i < conversation.Options.Count; i++)
                            conversation.Options[i].Deserialize(this);
                }
            }

            // Clear our dummy event
            Event = new UnityEngine.Events.UnityEvent();

            return conversation;
        }

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
}