using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    [KnownType(typeof(IntCondition))]
    [KnownType(typeof(BoolCondition))]
    public abstract class EditableConnection
    {
        public enum eConnectiontype
        {
            Speech,
            Option
        }

        public EditableConnection()
        {
            Conditions = new List<Condition>();
        }

        public abstract eConnectiontype ConnectionType { get; }

        [DataMember] public List<Condition> Conditions;
        [DataMember] public int NodeUID;
    }

    [DataContract]
    public class EditableSpeechConnection : EditableConnection
    {
        public EditableSpeechConnection(EditableSpeechNode node) : base()
        {
            Speech = node;
            NodeUID = node.ID;
        }

        public override eConnectiontype ConnectionType { get { return eConnectiontype.Speech; } }

        public EditableSpeechNode Speech;
    }

    [DataContract]
    public class EditableOptionConnection : EditableConnection
    {
        public EditableOptionConnection(EditableOptionNode node) : base()
        {
            Option = node;
            NodeUID = node.ID;
        }

        public override eConnectiontype ConnectionType { get { return eConnectiontype.Option; } }

        public EditableOptionNode Option;
    }
}