using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public static class DialogueEditorUtil
    {
        public static List<ConversationNode> GetAllNodes(ConversationAction root)
        {
            List<ConversationNode> nodes = new List<ConversationNode>();
            nodes.Add(root);

            AddChildrenToList(root, ref nodes);

            return nodes;
        }

        private static void AddChildrenToList(ConversationNode node, ref List<ConversationNode> nodes)
        {
            if (node is ConversationAction)
            {
                ConversationAction action = node as ConversationAction;
                if (action.Options != null)
                {
                    for (int i = 0; i < action.Options.Count; i++)
                    {
                        nodes.Add(action.Options[i]);
                        AddChildrenToList(action.Options[i], ref nodes);
                    }
                }
            }
            else
            {
                ConversationOption option = node as ConversationOption;
                if (option.Action != null)
                {
                    nodes.Add(option.Action);
                    AddChildrenToList(option.Action, ref nodes);
                }
            }
        }
    }
}