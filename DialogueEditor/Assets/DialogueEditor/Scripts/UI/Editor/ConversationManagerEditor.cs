﻿using UnityEngine;
using UnityEditor;

namespace DialogueEditor
{
    [CustomEditor(typeof(ConversationManager))]
    public class ConversationManagerEditor : Editor
    {
        private const string PREVIEW_TEXT = "Placeholder text. This image acts as a preview of the in-game GUI.";
        private const float BOX_HEIGHT = 75;
        private const float BUFFER = 15;
        private const float ICON_SIZE = 55;
        private const float OPTION_HEIGHT = 35;
        private const float OPTION_BUFFER = 5;
        private const float OPTION_TEXT_BUF_Y = 10;

        public override void OnInspectorGUI()
        {
            ConversationManager t = (ConversationManager)target;

            RenderPreviewImage(t);

            // Create a gap in EditorGuiLayout for the preview image to be rendered
            EditorGUILayout.BeginVertical();
            GUILayout.Space(BOX_HEIGHT + OPTION_BUFFER + OPTION_HEIGHT);
            EditorGUILayout.EndVertical();

            // Background image
            GUILayout.Label("Dialogue Image Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Background Sprite");
            t.BackgroundImage = (Sprite)EditorGUILayout.ObjectField(t.BackgroundImage, typeof(Sprite), allowSceneObjects: false);
            EditorGUILayout.EndHorizontal();
            t.BackgroundImageSliced = EditorGUILayout.Toggle("Sliced image", t.BackgroundImageSliced);
            EditorGUILayout.Space();

            // Option image
            GUILayout.Label("Dialogue Image Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Option Sprite");
            t.OptionImage = (Sprite)EditorGUILayout.ObjectField(t.OptionImage, typeof(Sprite), allowSceneObjects: false);
            EditorGUILayout.EndHorizontal();
            t.OptionImageSliced = EditorGUILayout.Toggle("Sliced image", t.OptionImageSliced);
            EditorGUILayout.Space();

            // Text
            GUILayout.Label("Text options", EditorStyles.boldLabel);
            t.ScrollText = EditorGUILayout.Toggle("Scroll Text", t.ScrollText);
            if (t.ScrollText)
                t.ScrollSpeed = EditorGUILayout.FloatField("Scroll Speed", t.ScrollSpeed);
        }

        private void RenderPreviewImage(ConversationManager t)
        {
            Rect contextRect = EditorGUILayout.GetControlRect();

            // Draw a background box
            Rect boxRect;
            float width, height;
            width = contextRect.width * 0.75f;
            height = BOX_HEIGHT;
            if (t.BackgroundImage == null)
            {
                boxRect = new Rect(contextRect.x + contextRect.width * 0.125f, contextRect.y + 10, width, height);
                EditorGUI.DrawRect(boxRect, Color.black);
            }
            else
            {
                boxRect = new Rect(contextRect.x + contextRect.width * 0.125f, contextRect.y + 10, width, height);

                if (t.BackgroundImageSliced)
                {
                    GUIStyle style = new GUIStyle();
                    RectOffset ro = new RectOffset();
                    ro.left = (int)t.BackgroundImage.border.w;
                    ro.top = (int)t.BackgroundImage.border.x;
                    ro.right = (int)t.BackgroundImage.border.y;
                    ro.bottom = (int)t.BackgroundImage.border.z;
                    style.border = ro;
                    style.normal.background = t.BackgroundImage.texture;
                    EditorGUI.LabelField(boxRect, "", style);
                }
                else
                {
                    GUI.DrawTexture(boxRect, t.BackgroundImage.texture);
                }
            }


            // Draw icon
            float difference = BOX_HEIGHT - ICON_SIZE;
            Rect iconRect = new Rect(boxRect.x + BUFFER, boxRect.y + difference * 0.5f, ICON_SIZE, ICON_SIZE);
            EditorGUI.DrawRect(iconRect, Color.white);
            Rect tmpt = new Rect(iconRect);
            tmpt.x += 5;
            tmpt.y += ICON_SIZE * 0.35f;
            EditorGUI.LabelField(tmpt, "<Icon>");

            // Draw text
            float text_x, text_wid;
            text_x = iconRect.x + iconRect.width + difference * 0.5f;
            text_wid = ((boxRect.x + boxRect.width) - difference * 0.5f) - text_x;
            Rect textRect = new Rect(text_x, iconRect.y, text_wid, ICON_SIZE);
            GUIStyle textStyle = new GUIStyle();
            textStyle.normal.textColor = Color.white;
            textStyle.wordWrap = true;
            textStyle.clipping = TextClipping.Clip;
            EditorGUI.LabelField(textRect, PREVIEW_TEXT, textStyle);


            // Option (left)
            float option_x, option_wid;
            option_x = boxRect.x + OPTION_BUFFER;
            option_wid = (boxRect.width * 0.5f) - OPTION_BUFFER * 2;
            Rect optionRect = new Rect(option_x, boxRect.y + boxRect.height + OPTION_BUFFER, option_wid, OPTION_HEIGHT);
            Rect optionTextRect = new Rect(optionRect);
            optionTextRect.x += optionRect.width * 0.35f;
            optionTextRect.y += OPTION_TEXT_BUF_Y;
            if (t.OptionImage == null)
            {
                EditorGUI.DrawRect(optionRect, Color.black);
            }
            else
            {
                if (t.OptionImageSliced)
                {
                    GUIStyle style = new GUIStyle();
                    RectOffset ro = new RectOffset();
                    ro.left = (int)t.OptionImage.border.w;
                    ro.top = (int)t.OptionImage.border.x;
                    ro.right = (int)t.OptionImage.border.y;
                    ro.bottom = (int)t.OptionImage.border.z;
                    style.border = ro;
                    style.normal.background = t.OptionImage.texture;
                    EditorGUI.LabelField(optionRect, "", style);
                }
                else
                {
                    GUI.DrawTexture(optionRect, t.OptionImage.texture, ScaleMode.StretchToFill);
                }
            }
            EditorGUI.LabelField(optionTextRect, "Option 1.", textStyle);

            // Option (right)
            optionRect.x += boxRect.width * 0.5f;
            optionTextRect.x += boxRect.width * 0.5f;
            if (t.OptionImage == null)
            {
                EditorGUI.DrawRect(optionRect, Color.black);
            }
            else
            {
                if (t.OptionImageSliced)
                {
                    GUIStyle style = new GUIStyle();
                    RectOffset ro = new RectOffset();
                    ro.left = (int)t.OptionImage.border.w;
                    ro.top = (int)t.OptionImage.border.x;
                    ro.right = (int)t.OptionImage.border.y;
                    ro.bottom = (int)t.OptionImage.border.z;
                    style.border = ro;
                    style.normal.background = t.OptionImage.texture;
                    EditorGUI.LabelField(optionRect, "", style);
                }
                else
                {
                    GUI.DrawTexture(optionRect, t.OptionImage.texture, ScaleMode.StretchToFill);
                }
            }
            EditorGUI.LabelField(optionTextRect, "Option 2.", textStyle);
        }
    }
}