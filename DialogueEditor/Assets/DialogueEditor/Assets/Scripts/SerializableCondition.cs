using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    public abstract class Condition
    {
        public Condition(string name)
        {
            ParameterName = name;
        }

        [DataMember] public string ParameterName;
    }

    [DataContract]
    public class IntCondition : Condition
    {
        public IntCondition(string name) : base(name) { }

        public enum eCheckType
        {
            equal,
            lessThan,
            greaterThan
        }

        [DataMember] public eCheckType CheckType;
        [DataMember] public int RequiredValue;
    }

    [DataContract]
    public class BoolCondition : Condition
    {
        public BoolCondition(string name) : base(name) { }

        public enum eCheckType
        {
            equal,
            notEqual
        }

        [DataMember] public eCheckType CheckType;
        [DataMember] public bool RequiredValue;
    }
}