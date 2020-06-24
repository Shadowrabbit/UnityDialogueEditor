using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DialogueEditor
{
    [DataContract]
    public abstract class EditableConversationNode
    {
        /// <summary> Info used internally by the editor window. </summary>
        [DataContract]
        public class EditorArgs
        {
            [DataMember]
            public float xPos;

            [DataMember]
            public float yPos;

            [DataMember]
            public bool isRoot;
        }

        public EditableConversationNode()
        {
            parents = new List<EditableConversationNode>();
            parentUIDs = new List<int>();
            EditorInfo = new EditorArgs { xPos = 0, yPos = 0, isRoot = false };
        }

        /// <summary> Info used internally by the editor window. </summary>
        [DataMember]
        public EditorArgs EditorInfo;

        [DataMember]
        public int ID;

        [DataMember]
        public string Text;

        /// <summary> TextMeshPro font </summary>
        public TMPro.TMP_FontAsset TMPFont;
        [DataMember]
        public string TMPFontGUID;

        [DataMember]
        public List<int> parentUIDs;
        public List<EditableConversationNode> parents;

        // ------------------------

        public abstract void RemoveSelfFromTree();
        public abstract void RegisterUIDs();

        public virtual void PrepareForSerialization(NPCConversation conversation)
        {
            conversation.GetNodeData(this.ID).TMPFont = this.TMPFont;
        }

        public virtual void Deserialize(NPCConversation conversation)
        {
            this.TMPFont = conversation.GetNodeData(this.ID).TMPFont;

#if UNITY_EDITOR
            // If under V1.03, Load from database via GUID, so data is not lost for people who are upgrading
            if (conversation.Version < 103)
            {
                if (this.TMPFont == null)
                {
                    if (!string.IsNullOrEmpty(TMPFontGUID))
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(TMPFontGUID);
                        this.TMPFont = (TMPro.TMP_FontAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(TMPro.TMP_FontAsset));

                    }
                }
            }
#endif
        }
    }




    [DataContract]
    public class EditableSpeechNode : EditableConversationNode
    {
        public EditableSpeechNode() : base()
        {
            Options = new List<EditableOptionNode>();
            OptionUIDs = new List<int>();
            SpeechUID = EditableConversation.INVALID_UID; ;
        }

        [DataMember]
        public string Name;

        /// <summary>
        /// The selectable options of this Speech.
        /// </summary>
        public List<EditableOptionNode> Options;
        [DataMember] public List<int> OptionUIDs;

        /// <summary>
        /// The Speech this Speech leads onto (if no options).
        /// </summary>
        public EditableSpeechNode Speech;
        [DataMember] public int SpeechUID;

        /// <summary>
        /// The NPC Icon
        /// </summary>
        public Sprite Icon;
        [DataMember] public string IconGUID;

        /// <summary>
        /// The Audio Clip acompanying this Speech.
        /// </summary>
        public AudioClip Audio;
        [DataMember] public string AudioGUID;

        /// <summary>
        /// The Volume for the AudioClip;
        /// </summary>
        [DataMember] public float Volume;

        /// <summary>
        /// If this dialogue leads onto another dialogue... 
        /// Should the dialogue advance automatially?
        /// </summary>
        [DataMember] public bool AdvanceDialogueAutomatically;

        /// <summary>
        /// If this dialogue automatically advances, should it also display an 
        /// "end" / "continue" button?
        /// </summary>
        [DataMember] public bool AutoAdvanceShouldDisplayOption;

        /// <summary>
        /// The time it will take for the Dialogue to automaically advance
        /// </summary>
        [DataMember] public float TimeUntilAdvance;

        // ------------------------------

        public void AddOption(EditableOptionNode newOption)
        {
            if (Options == null)
                Options = new List<EditableOptionNode>();

            if (Options.Contains(newOption))
                return;

            // Delete the speech I point to, if any
            if (this.Speech != null)
            {
                this.Speech.parents.Remove(this);
            }
            this.Speech = null;

            // Setup option connection
            if (!newOption.parents.Contains(this))
                newOption.parents.Add(this);
            Options.Add(newOption);
        }

        public void SetSpeech(EditableSpeechNode newSpeech)
        {
            // Remove myself as a parent from the speech I was previously pointing to
            if (this.Speech != null)
            {
                this.Speech.parents.Remove(this);
            }

            // Remove any options I may have
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    // I am no longer the parents of these options
                    Options[i].parents.Remove(this);
                }
                Options.Clear();
            }

            this.Speech = newSpeech;
            if (!newSpeech.parents.Contains(this))
                newSpeech.parents.Add(this);
        }

        public override void RemoveSelfFromTree()
        {
            // This speech is no longer the parents resulting speech
            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i] != null)
                {
                    if (parents[i] is EditableOptionNode)
                    {
                        (parents[i] as EditableOptionNode).Speech = null;
                    }
                    else if (parents[i] is EditableSpeechNode)
                    {
                        (parents[i] as EditableSpeechNode).Speech = null;
                    }
                }
            }

            // This speech is no longer the parent of any children options
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    Options[i].parents.Clear();
                }
            }

            // This speech is no longer the parent of any speech nodes
            if (this.Speech != null)
            {
                this.Speech.parents.Remove(this);
            }
        }

        public override void RegisterUIDs()
        {
            if (parentUIDs != null)
                parentUIDs.Clear();
            parentUIDs = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                parentUIDs.Add(parents[i].ID);
            }

            if (OptionUIDs != null)
                OptionUIDs.Clear();
            OptionUIDs = new List<int>();
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    OptionUIDs.Add(Options[i].ID);
                }
            }

            if (Speech != null)
                SpeechUID = Speech.ID;
            else
                SpeechUID = EditableConversation.INVALID_UID;
        }

        public override void PrepareForSerialization(NPCConversation conversation)
        {
            base.PrepareForSerialization(conversation);

            conversation.GetNodeData(this.ID).Audio = this.Audio;
            conversation.GetNodeData(this.ID).Icon = this.Icon;
        }

        public override void Deserialize(NPCConversation conversation)
        {
            base.Deserialize(conversation);

            this.Audio = conversation.GetNodeData(this.ID).Audio;
            this.Icon = conversation.GetNodeData(this.ID).Icon;

#if UNITY_EDITOR
            // If under V1.03, Load from database via GUID, so data is not lost for people who are upgrading
            if (conversation.Version < 103)
            {
                if (this.Audio == null)
                {
                    if (!string.IsNullOrEmpty(AudioGUID))
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(AudioGUID);
                        this.Audio = (AudioClip)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(AudioClip));

                    }
                }

                if (this.Icon == null)
                {
                    if (!string.IsNullOrEmpty(IconGUID))
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(IconGUID);
                        this.Icon = (Sprite)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));

                    }
                }
            }
#endif
        }
    }


    [DataContract]
    [KnownType(typeof(IntCondition))]
    [KnownType(typeof(BoolCondition))]
    public class EditableOptionNode : EditableConversationNode
    {
        public EditableOptionNode() : base()
        {
            SpeechUID = EditableConversation.INVALID_UID;
            this.Conditions = new List<Condition>();
        }

        /// <summary> The Speech this option leads to. </summary>
        public EditableSpeechNode Speech;      
        [DataMember] public int SpeechUID;
        [DataMember] public List<Condition> Conditions;

        public void SetSpeech(EditableSpeechNode newSpeech)
        {
            // Remove myself as a parent from the speech I was previously pointing to
            if (this.Speech != null)
            {
                this.Speech.parents.Remove(this);
            }

            this.Speech = newSpeech;
            if (!newSpeech.parents.Contains(this))
                newSpeech.parents.Add(this);
        }

        public override void RemoveSelfFromTree()
        {
            // This option is no longer part of any parents speechs possible options
            for (int i = 0; i < parents.Count; i++)
            {
                (parents[i] as EditableSpeechNode).Options.Remove(this);
            }

            // This option is no longer the parent to its child speech
            if (Speech != null)
            {
                Speech.parents.Remove(this);
            }
        }

        public override void RegisterUIDs()
        {
            if (parentUIDs != null)
                parentUIDs.Clear();
            parentUIDs = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                parentUIDs.Add(parents[i].ID);
            }

            SpeechUID = EditableConversation.INVALID_UID;
            if (Speech != null)
                SpeechUID = Speech.ID;
        }
    }
}