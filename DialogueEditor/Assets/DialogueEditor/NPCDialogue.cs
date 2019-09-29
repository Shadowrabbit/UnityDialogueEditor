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

    //[CreateAssetMenu(fileName = "NPCDialogue", menuName = "NPCDialogue/Dialogue")]
    //[System.Serializable]
    //public class OLDNPCDialogue : ScriptableObject
    //{
    //    public OLDNPCActionNode Root;

    //    public string json;

    //    public void Jsonify()
    //    {

    //    }

    //    public List<OLDNPCNode> GetAllNodesInTree()
    //    {
    //        List<OLDNPCNode> nodes = new List<OLDNPCNode>();
    //        nodes.Add(Root);
    //        Root.AddChildrenToList(ref nodes);
    //        return nodes;
    //    }
    //}

    //[System.Serializable]
    //public abstract class OLDNPCNode
    //{
    //    public float uiX;
    //    public float uiY;

    //    internal abstract void AddChildrenToList(ref List<OLDNPCNode> nodes);
    //}

    //[System.Serializable]
    //public class OLDNPCActionNode : OLDNPCNode
    //{
    //    /// <summary>
    //    /// Determines whether this node will result in the NPC saying 
    //    /// some dialogue or performing a given action (e.g. opening a shop, 
    //    /// accepting a quest). This option has zero impact on anything, it is 
    //    /// merely for your own organisation. 
    //    /// </summary>
    //    public eNodeType ActionType = eNodeType.Dialogue;

    //    /// <summary>
    //    /// If Dialogue: The speech value of the node
    //    /// E.g. (Greetings Traveler, how are you today?)
    //    /// 
    //    /// If Action: The value to send to the NPC
    //    /// E.g. (ACCEPT_QUEST)
    //    /// </summary>
    //    public string ActionValue;

    //    /// <summary>
    //    /// A list of possible options this Node has
    //    /// </summary>
    //    public List<OLDNPCOptionNode> Options;

    //    public void AddOption(OLDNPCOptionNode node)
    //    {
    //        if (Options == null) { Options = new List<OLDNPCOptionNode>(); }
    //        Options.Add(node);
    //    }


    //    internal override void AddChildrenToList(ref List<OLDNPCNode> nodes)
    //    {
    //        if (Options == null) { Options = new List<OLDNPCOptionNode>(); }

    //        if (Options.Count > 0)
    //        {
    //            for (int i = 0; i < Options.Count; i++)
    //            {
    //                nodes.Add(Options[i]);
    //                Options[i].AddChildrenToList(ref nodes);
    //            }
    //        }
    //    }

    //    public bool IsEffectivelyNull()
    //    {
    //        return (Options == null && ActionValue == null && uiX == 0 && uiY == 0);
    //    }
    //}

    //[System.Serializable]
    //public class OLDNPCOptionNode : OLDNPCNode
    //{
    //    /// <summary>
    //    /// The text value of this option. 
    //    /// E.g. (Accept Quest)
    //    /// </summary>
    //    public string Value = "";

    //    /// <summary>
    //    /// The NPCDialogueNode this Option leads to
    //    /// </summary>
    //    public OLDNPCActionNode Action = null;

    //    internal override void AddChildrenToList(ref List<OLDNPCNode> nodes)
    //    {
    //        if (Action != null)
    //        {
    //            nodes.Add(Action);
    //            Action.AddChildrenToList(ref nodes);
    //        }
    //    }
    //}
}