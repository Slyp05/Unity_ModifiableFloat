using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
    
    ModifiableFloat created by Lucas Sarkadi.

    Creative Commons Zero v1.0 Universal licence, 
    meaning it's free to use in any project with no need to ask permission or credits the author.

    Check out the github page for more informations:
    https://github.com/Slyp05/Unity_ModifiableFloat/

*/
[CustomPropertyDrawer(typeof(ModifiableFloat))]
public class ModifiableFloatDrawer : PropertyDrawer
{
    const float checkBoxSize = 18f;
    const float checkBoxDecal = 3.5f;
    const float marginDecal = 14;

    bool isOpened = false;
    bool modifierOpened = false;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect posCopy;
        GUIStyle baseStyle = new GUIStyle(GUI.skin.textField);

        ModifiableFloat targetMF = fieldInfo.GetValue(property.serializedObject.targetObject) as ModifiableFloat;

        SerializedProperty baseValProp = property.FindPropertyRelative("_baseValue");
        SerializedProperty ignoreModifProp = property.FindPropertyRelative("_ignoreModification");

        // manual update value
        targetMF._EDITOR_ONLY_ForceProcess();
        int nbOfModifier = targetMF._EDITOR_ONLY_GetNumberOfModifier();
        bool isIgnore = targetMF.IgnoreModification;
        
        label = EditorGUI.BeginProperty(position, label, property);
        position.height = 16;

        isOpened = EditorGUI.Foldout(new Rect(position.x, position.y, 0, 16),
            isOpened, GUIContent.none, false);

        if (!isOpened)
        {
            // Show base parameters stacked on a line
            bool gottaShowResult = nbOfModifier > 0 && !isIgnore;
            float extraWidth = !gottaShowResult ? checkBoxSize :
                baseStyle.CalcSize(new GUIContent(targetMF.Value.ToString())).x + checkBoxSize;

            posCopy = new Rect(position);
            posCopy.width -= extraWidth;
            EditorGUI.PropertyField(posCopy, baseValProp, label);

            posCopy = new Rect(position);
            posCopy.x = position.xMax - checkBoxSize + checkBoxDecal;
            posCopy.width = checkBoxSize;
            EditorGUI.PropertyField(posCopy, ignoreModifProp, GUIContent.none);

            if (gottaShowResult)
            {
                posCopy = new Rect(position);
                posCopy.x = position.xMax - extraWidth;
                posCopy.width = extraWidth - checkBoxSize;
                GUI.enabled = false;
                EditorGUI.FloatField(posCopy, GUIContent.none, targetMF.Value);
                GUI.enabled = true;
            }
        }
        else
        {
            // show label
            EditorGUI.LabelField(position, label);
            position.xMin += marginDecal;
            EditorGUIUtility.labelWidth -= marginDecal;
            position.y += 18;

            // show base value
            EditorGUI.PropertyField(position, baseValProp, 
                new GUIContent((isIgnore ? "Value" : "Base Value"), "Value before any modification"));
            position.y += 18;

            // show ignore modifications checkbox
            float temp2 = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = position.width - checkBoxSize + checkBoxDecal - 2.5f;
            EditorGUI.PropertyField(position, ignoreModifProp, 
                new GUIContent("Ignore modification", "Toggle to ignore all modifications"));
            EditorGUIUtility.labelWidth = temp2;
            position.y += 18;

            if (!isIgnore)
            {
                // show result value
                posCopy = EditorGUI.PrefixLabel(position, 
                    new GUIContent("Modified Value", "Value after modification"));
                GUI.enabled = false;
                EditorGUI.FloatField(posCopy, GUIContent.none, targetMF.Value);
                GUI.enabled = true;
                position.y += 18;

                // show integers value
                posCopy = EditorGUI.PrefixLabel(position, 
                    new GUIContent("Integer Value", "Value converted to an integer"));

                int floor = targetMF.Floor;
                int round = targetMF.Round;
                int ceil = targetMF.Ceil;

                float floorDrawSize = baseStyle.CalcSize(new GUIContent(floor.ToString())).x;
                float roundDrawSize = baseStyle.CalcSize(new GUIContent(round.ToString())).x;
                float ceilDrawSize = baseStyle.CalcSize(new GUIContent(ceil.ToString())).x;

                GUI.enabled = false;
                if (floorDrawSize + roundDrawSize + ceilDrawSize <= posCopy.width) // show tout
                {
                    float leftOverSize = posCopy.width - (floorDrawSize + roundDrawSize + ceilDrawSize);

                    posCopy.width = floorDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, floor);
                    posCopy.x += posCopy.width;
                    posCopy.width = leftOverSize / 3;
                    EditorGUI.TextField(posCopy, "Floor", GUI.skin.label);
                    posCopy.x += leftOverSize / 3;

                    posCopy.width = roundDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, round);
                    posCopy.x += posCopy.width;
                    posCopy.width = leftOverSize / 3;
                    EditorGUI.TextField(posCopy, "Round", GUI.skin.label);
                    posCopy.x += leftOverSize / 3;

                    posCopy.width = ceilDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, ceil);
                    posCopy.x += posCopy.width;
                    posCopy.width = leftOverSize / 3;
                    EditorGUI.TextField(posCopy, "Ceil", GUI.skin.label);
                }
                else if (floorDrawSize + ceilDrawSize <= posCopy.width) // show floor et ceil
                {
                    bool roundIsFloor = (round == floor); // si false, roundIsCeil

                    float leftOverSize = posCopy.width - (floorDrawSize + ceilDrawSize);

                    posCopy.width = floorDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, floor);
                    posCopy.x += posCopy.width;
                    posCopy.width = leftOverSize / 2;
                    EditorGUI.TextField(posCopy, (roundIsFloor ? "Round" : "Floor"), GUI.skin.label);
                    posCopy.x += leftOverSize / 2;

                    posCopy.width = ceilDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, ceil);
                    posCopy.x += posCopy.width;
                    posCopy.width = leftOverSize / 2;
                    EditorGUI.TextField(posCopy, (!roundIsFloor ? "Round" : "Ceil"), GUI.skin.label);
                }
                else // show round only
                {
                    posCopy.width = roundDrawSize;
                    EditorGUI.IntField(posCopy, GUIContent.none, round);
                    posCopy.x += posCopy.width;
                    EditorGUI.TextField(posCopy, "Round", GUI.skin.label);
                }
                position.y += 18;
                GUI.enabled = true;

                // Show modifiers
                modifierOpened = EditorGUI.Foldout(new Rect(position.x, position.y, 0, 16), modifierOpened,
                    GUIContent.none, false);
                GUI.enabled = false;

                if (!modifierOpened)
                {
                    posCopy = EditorGUI.PrefixLabel(position, new GUIContent("Modifiers", "Number of modifiers"));
                    EditorGUI.IntField(posCopy, GUIContent.none, nbOfModifier);
                }
                else
                {
                    string debugString = targetMF.DebugString();
                    float areaHeight = baseStyle.CalcSize(new GUIContent(debugString)).y;

                    posCopy = new Rect(position);
                    posCopy.height = areaHeight;

                    EditorGUI.TextArea(posCopy, debugString);

                    position.y += areaHeight;
                }

                GUI.enabled = true;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ModifiableFloat targetMF = fieldInfo.GetValue(property.serializedObject.targetObject) as ModifiableFloat;
        bool isIgnore = targetMF.IgnoreModification;

        if (!isOpened)
            return 16;

        if (isIgnore)
            return 16 + 18 + 18;

        if (!modifierOpened)
            return 16 + 18 + 18 + 18 + 18 + 18;

        GUIStyle baseStyle = new GUIStyle(GUI.skin.textField);
        string debugString = targetMF.DebugString();
        float areaHeight = baseStyle.CalcSize(new GUIContent(debugString)).y;

        return 16 + 18 + 18 + 18 + 18 + Mathf.Max(18, areaHeight);
    }
}
