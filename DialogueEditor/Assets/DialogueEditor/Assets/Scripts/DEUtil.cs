using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor
{
    public static class DEUtil
    {
        //--------------------------------------
        // Text + Language Util
        //--------------------------------------

        /// <summary>
        /// This function will set the text of a TextMesh object, 
        /// and ensure the text object is setup correctly to RTL languges
        /// </summary>
        public static void SetTextmeshText(TMPro.TextMeshProUGUI textmesh, string text, SystemLanguage language)
        {
            textmesh.text = text;

            if (IsRightToLeftLanguage(language))
            {
                textmesh.isRightToLeftText = true;
                textmesh.alignment = TMPro.TextAlignmentOptions.TopRight;
            }
            else
            {
                textmesh.isRightToLeftText = false;
                textmesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
            }
        }

        public static bool IsRightToLeftLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Arabic:
                case SystemLanguage.Hebrew:
                    return true;
            }

            return false;
        }
    }
}