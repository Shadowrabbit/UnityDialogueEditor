using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    public abstract class EditableParameter
    {
        public enum eParamType
        {
            Bool,
            Int
        }

        public EditableParameter(string name)
        {
            ParameterName = name;
        }

        public abstract eParamType ParameterType { get; }

        [DataMember] public string ParameterName;
    }

    [DataContract]
    public class EditableBoolParameter : EditableParameter
    {
        public EditableBoolParameter(string name) : base(name) { }

        public override eParamType ParameterType { get { return eParamType.Bool; } }

        [DataMember] public bool BoolValue;
    }

    [DataContract]
    public class EditableIntParameter : EditableParameter
    {
        public EditableIntParameter(string name) : base(name) { }

        public override eParamType ParameterType { get { return eParamType.Int; } }

        [DataMember] public int IntValue;
    }
}