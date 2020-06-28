using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    [KnownType(typeof(IntCondition))]
    [KnownType(typeof(BoolCondition))]
    public abstract class Connection
    {
        public Connection()
        {
            Conditions = new List<Condition>();
        }

        [DataMember] public List<Condition> Conditions;
        [DataMember] public int NodeUID;
    }

    [DataContract]
    public class SpeechConnection : Connection
    {
        public SpeechConnection(EditableSpeechNode node) : base()
        {
            Speech = node;
            NodeUID = node.ID;
        }

        public EditableSpeechNode Speech;
    }

    [DataContract]
    public class OptionConnection : Connection
    {
        public OptionConnection(EditableOptionNode node) : base()
        {
            Option = node;
            NodeUID = node.ID;
        }

        public EditableOptionNode Option;
    }
}