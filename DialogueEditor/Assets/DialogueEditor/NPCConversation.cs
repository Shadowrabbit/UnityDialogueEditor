using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
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

    [DataContract]
    public abstract class ConversationNode
    {
        /// <summary> Info used internally by the editor window. </summary>
        [DataMember]
        public EditorArgs EditorInfo = new EditorArgs();

        [DataMember]
        public string Text;
    }

    [DataContract]
    public class ConversationAction : ConversationNode
    {
        [DataMember]
        public List<ConversationOption> Options;
    }

    [DataContract]
    public class ConversationOption : ConversationNode
    {
        [DataMember]
        public ConversationAction Action;
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
}