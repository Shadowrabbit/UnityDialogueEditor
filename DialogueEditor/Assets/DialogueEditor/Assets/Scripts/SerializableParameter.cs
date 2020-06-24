using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    public abstract class Parameter
    {
        public Parameter(string name)
        {
            ParameterName = name;
        }

        [DataMember]
        public string ParameterName;
    }

    [DataContract]
    public class BoolParameter : Parameter
    {
        public BoolParameter(string name) : base(name) { }

        [DataMember]
        public bool BoolValue;
    }

    [DataContract]
    public class IntParameter : Parameter
    {
        public IntParameter(string name) : base(name) { }

        [DataMember]
        public int IntValue;
    }
}