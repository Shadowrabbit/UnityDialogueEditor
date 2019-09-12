using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public enum eNodeType
    {
        Dialogue,
        Action
    }

    [CreateAssetMenu(fileName = "NPCDialogue", menuName = "NPCDialogue/Dialogue")]
    [System.Serializable]
    public class NPCDialogue : ScriptableObject
    {
        [SerializeField]
        public NPCActionNode Root;

        public List<NPCNode> GetAllNodesInTree()
        {
            List<NPCNode> nodes = new List<NPCNode>();
            nodes.Add(Root);
            Root.AddChildrenToList(ref nodes);
            return nodes;
        }
    }

    [System.Serializable]
    public abstract class NPCNode
    {
        [SerializeField]
        public float uiX;
        [SerializeField]
        public float uiY;

        internal abstract void AddChildrenToList(ref List<NPCNode> nodes);
    }

    [System.Serializable]
    public class NPCActionNode : NPCNode
    {
        /// <summary>
        /// Determines whether this node will result in the NPC saying 
        /// some dialogue or performing a given action (e.g. opening a shop, 
        /// accepting a quest). 
        /// </summary>
        [SerializeField]
        public eNodeType ActionType;

        /// <summary>
        /// If Dialogue: The speech value of the node
        /// E.g. (Greetings Traveler, how are you today?)
        /// 
        /// If Action: The value to send to the NPC
        /// E.g. (ACCEPT_QUEST)
        /// </summary>
        [SerializeField]
        public string ActionValue;

        /// <summary>
        /// An optional argument for an Action
        /// E.g. (QUEST_DELIVER_MEDICINE)
        /// </summary>
        [SerializeField]
        public string Arg;

        /// <summary>
        /// A list of possible options this Node has
        /// </summary>
        [SerializeField]
        public List<NPCOptionNode> Options;

        public void AddOption(NPCOptionNode node)
        {
            if (Options == null) { Options = new List<NPCOptionNode>(); }
            Options.Add(node);
        }

        internal override void AddChildrenToList(ref List<NPCNode> nodes)
        {
            if (Options == null) { Options = new List<NPCOptionNode>(); }

            if (Options.Count > 0)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    nodes.Add(Options[i]);
                    Options[i].AddChildrenToList(ref nodes);
                }
            }
        }
    }

    [System.Serializable]
    public class NPCOptionNode : NPCNode
    {
        /// <summary>
        /// The text value of this option. 
        /// E.g. (Accept Quest)
        /// </summary>
        [SerializeField]
        public string Value;

        /// <summary>
        /// The NPCDialogueNode this Option leads to
        /// </summary>
        [SerializeField]
        public NPCActionNode Action;

        public void SetResult(NPCActionNode node)
        {
            Action = node;
        }

        internal override void AddChildrenToList(ref List<NPCNode> nodes)
        {
            if (Action != null)
            {
                nodes.Add(Action);
                Action.AddChildrenToList(ref nodes);
            }
        }
    }
}