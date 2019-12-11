using UnityEngine;

namespace DialogueEditor
{
    [System.Serializable]
    public class NodeEventHolder : MonoBehaviour
    {
        [SerializeField]
        public int NodeID;
        [SerializeField]
        public UnityEngine.Events.UnityEvent Event;
    }
}