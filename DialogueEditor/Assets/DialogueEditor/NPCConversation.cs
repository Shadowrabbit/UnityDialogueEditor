using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    public abstract class ConversationNode
    {
        /// <summary> Info used internally by the editor window. </summary>
        [DataMember]
        public EditorArgs EditorInfo = new EditorArgs();

        [DataMember]
        public string Text;

        // [DataMember]
        public List<ConversationNode> Parents
        {
            get
            {
                if (parents == null)
                    parents = new List<ConversationNode>();
                return parents; 
            }
        }
        public List<ConversationNode> parents = new List<ConversationNode>();

        public abstract void RemoveSelfFromTree();
    }

    [DataContract]
    public class ConversationAction : ConversationNode
    {
        /// <summary>
        /// The selectable options of this Action.
        /// </summary>
        [DataMember]
        public List<ConversationOption> Options;

        public void AddOption(ConversationOption newOption)
        {
            if (Options == null)
                Options = new List<ConversationOption>();

            if (Options.Contains(newOption))
                return;

            newOption.Parents.Add(this);
            Options.Add(newOption);
        }

        public override void RemoveSelfFromTree()
        {
            // This action is no longer the parents resulting action
            for (int i = 0; i < Parents.Count; i++)
                (Parents[i] as ConversationOption).Action = null;

            // This action is no longer the parent of any children options
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    Options[i].Parents.Clear();
                }
            }
        }
    }

    [DataContract]
    public class ConversationOption : ConversationNode
    {
        /// <summary>
        /// The Action this option leads to.
        /// </summary>
        [DataMember]
        public ConversationAction Action;

        public void SetAction(ConversationAction newAction)
        {
            // Remove myself as a parent from the action I was previously pointing to
            if (this.Action != null)
            {
                this.Action.Parents.Remove(this);
            }

            this.Action = newAction;
            newAction.Parents.Add(this);
        }

        public override void RemoveSelfFromTree()
        {
            // This option is no longer part of any parents actions possible options
            for (int i = 0; i < Parents.Count; i++)
            {
                (Parents[i] as ConversationAction).Options.Remove(this);
            }

            // This option is no longer the parent to its child action
            if (Action != null)
            {
                Action.Parents.Remove(this);
            }
        }
    }

    /// <summary> Info used internally by the editor window. </summary>
    [DataContract]
    public class EditorArgs
    {
        [DataMember]
        public float xPos;

        [DataMember]
        public float yPos;
    }




    //--------------------------------------
    // Scriptable and Serialization
    //--------------------------------------

    [CreateAssetMenu(fileName = "NPC_Conversation", menuName = "NPC_Conversation/Conversation")]
    [System.Serializable]
    public class NPCConversation : ScriptableObject
    {
        [SerializeField]
        public string json;

        public void Serialize(ConversationAction conversation)
        {
            if (conversation == null || conversation.Options == null) { return; }

            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ConversationAction));
            ser.WriteObject(ms, conversation);
            byte[] jsonData = ms.ToArray();
            ms.Close();
            json = System.Text.Encoding.UTF8.GetString(jsonData, 0, jsonData.Length);
        }

        public ConversationAction GetDeserialized()
        {
            if (json == null || json == "")
                return null;

            ConversationAction deserialized = new ConversationAction();
            System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(deserialized.GetType());
            deserialized = ser.ReadObject(ms) as ConversationAction;
            ms.Close();
            return deserialized;
        }
    }
}