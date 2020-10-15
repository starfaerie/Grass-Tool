/*The MIT License (MIT)
Copyright (c) 2016 Edward Rowe (@edwardlrowe)
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws a Comment Icon on GameObjects in the Hierarchy that contain the Comment component.
/// </summary>
[InitializeOnLoad]
public class HierarchyIcons
{
    private static readonly Texture2D GrassIcon;

    static HierarchyIcons()
    {
        GrassIcon = AssetDatabase.LoadAssetAtPath("Assets/Icons/grass_sprite.png", typeof(Texture2D)) as Texture2D;

        if (GrassIcon == null)
        {
            return;
        } 

        EditorApplication.hierarchyWindowItemOnGUI += DrawGrassIconOnWindowItem;
    }

    private static void DrawGrassIconOnWindowItem(int instanceID, Rect rect)
    {
        if (GrassIcon == null)
        {
            return;
        }

        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (gameObject == null)
        {
            return;
        }

        Grass grass = gameObject.GetComponent<Grass>();
        if (grass == null) return;
        const float iconWidth = 35;
        EditorGUIUtility.SetIconSize(new Vector2(iconWidth, iconWidth));
        Vector2 padding = new Vector2(5, 0);
        Rect iconDrawRect = new Rect(
            rect.xMax - (iconWidth + padding.x), 
            rect.yMin, 
            rect.width, 
            rect.height);
        GUIContent iconGUIContent = new GUIContent(GrassIcon);
        EditorGUI.LabelField(iconDrawRect, iconGUIContent);
        EditorGUIUtility.SetIconSize(Vector2.zero);
    }
}