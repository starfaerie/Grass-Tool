using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

//any script that extends from 'Editor' or 'EditorWindow' MUST be in a folder named "Editor" (case sensitive) in order for Unity to recognize it as an Editor script
public class GrassPlacement : EditorWindow 
{
    //<summary>
    //Tells Unity to creates a menu item at the top of the Unity UI named 'Tools' which has another menu named 'Grass'
    //The '_G' creates a keyboard shortcut for opening this tool by pressing the 'G' key
    //Unity already has dozens of keyboard shortcuts, make sure you pick a shortcut for your tool that is unique!
    //<a href="https://docs.unity3d.com/2018.1/Documentation/Manual/UnityHotkeys.html">Unity Shortcut Documentation</a>
    //</summary>
    [MenuItem("Tools/Grass _G")]
    //<summary> GetWindow<>()
    //the first static function after the [MenuItem] tag is automagically executed after clicking the menu item or using the keyboard shortcut
    //In this case I'm calling the built in UnityFunction GetWindow<CLASSNAME>() which creates a new dockable Unity Window
    //By giving the GetWindow function the name of this class I'm telling Unity to treat this class as that new Window.
    //</summary>
    public static void OpenGrassTool() => GetWindow<GrassPlacement>(); //if I gave this method the 'focusedWindow' property the window would be undockable like Unity's Gradient EditorWindow. 
    
    //variables to be used by this EditorWindow
    public float radius = 2f;
    public float spawnDistance = 0.5f;
    public Mesh mesh;

    //<summary>SerializedProperties
    //I have to declare each property as a SerializedProperty deriving from a SerializedObject
    //Otherwise the changes I make in editor will not actually update the above declared variables
    //Creating SerializedProperties is also necessary for saving the state of a custom Unity EditorWindow between sessions
    //Something I haven't yet implemented here, but is next on my to-do list after fixing the Undo/Redo serialization
    //</summary>
    private SerializedObject so;
    private SerializedProperty propRadius;
    private SerializedProperty propSpawnDistance;
    private SerializedProperty propMesh;
    
    //<summary>
    //temporary point lists ******NOTE TO SELF: MOVE THESE OVER TO GRASS.CS**********
    //Currently these lists are deleted every time the GrassPlacement tool is closed and reopened
    //I could save these lists using Unity's serialization but much simpler would be to put these lists in the Grass.cs class
    //Which would allow me to save and modify the data of multiple Grass Meshes using the same EditorWindow
    //</summary>
    private List<Vector2> _randomPoints = new List<Vector2>();
    private List<Vector3> allMeshPoints = new List<Vector3>();
    
    //GUI property that I'm using to set a minimum size for the EditorWindow
    private readonly Vector2 windowSize = new Vector2(240, 270);
    
    //shader properties
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int AlphaTex = Shader.PropertyToID("_AlphaTex");

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnDistance = so.FindProperty("spawnDistance");
        propMesh = so.FindProperty("mesh");

        _randomPoints = PointSpawner.Points(radius,spawnDistance);

        AssignMesh();

        //Subscribe the SceneView to the DuringSceneGUI() Delegate when this window is open.
        //This uses the Delegate Design System we learned in Dr. Dan's class.
        //We'll unsubscribe from this method when the window is closed in OnDisable()
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnFocus()
    {
        FocusOnGrass();
        focusedWindow.minSize = windowSize; //ensures that the user cannot shrink the size of this EditorWindow below a certain value
    }

    private void AssignMesh()
    {
        if ((Grass) FindObjectOfType(typeof(Grass))) //if an object of type Grass exists
        {
            Grass grass = (Grass) FindObjectOfType(typeof(Grass));
            if (grass.GetComponent<MeshFilter>().sharedMesh) //and that object has an assigned mesh
            {
                mesh = grass.GetComponent<MeshFilter>().sharedMesh; //assign the value of this tool's Mesh to that object's already assigned mesh
            }
            else //otherwise, create a new Mesh
            {
                mesh = new Mesh
                {
                    name = "Grass"
                };
            }
        }
        else //if no object of type Grass exists
        {
            mesh = new Mesh //create a new Mesh
            {
                name = "Grass"
            };
            if (!(Grass) FindObjectOfType(typeof(Grass))) //redundant. will fix later
            {
                CreateNewObject(typeof(Grass), "Grass"); //create a new GameObject named 'Grass' and assign it the Grass component
            }
            Grass grass = (Grass) FindObjectOfType(typeof(Grass));
            grass.GetComponent<MeshFilter>().sharedMesh = mesh; //then assign that GameObject's mesh to new one we just made
        }
    }
    private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI; //unsubscribe the SceneView from the DuringSceneGUI() method.

    #region Editor Window GUI
    private void OnGUI()
    {
        //Put all of the onGUI calls between Update() and an ApplyModifiedProperties() call
        //or else those properties will not update when you make changes to what is displayed in the GUI
        so.Update();
        //Setting the color of the subsequently drawn Inspector GUI properties to a BlueIsh hue because I can. Color.white is the default
        GUI.color = Color.Lerp(Color.cyan, Color.white, 0.7f);

        //<summary> Inspector Window Unity Markup Language Shenanigans
        //Unity essentially has its own markup language for drawing inspectors
        //Actually, it has two. But one of them isn't supported before Unity 2018.x
        //The new one more closely resembles HTML and will probably replace what I'm doing here sometime in 2021 but Unity hasn't yet announced when.
        //All of the following shenanigans is, believe it or not, *shorthand* using built in Unity stylesheets that allows me to avoid coding all of the GUI values by hand
        //while still allowing me to deviate significantly from the default Unity Inspector
        //<summary>
        GUILayout.BeginVertical(EditorStyles.helpBox); //Marks the beginning of a box. Think of GUILayout.BeginVertical() as an HTML <div> tag that ends with GUILayout.EndVertical()
        GUILayout.Label("Placement Options:", EditorStyles.boldLabel); //Drawing a bold label

        EditorGUILayout.BeginHorizontal(); //think of this like an HTML <p> tag that will be closed by EditorGUILayout.EndHorizontal();
        //Drawing a dynamic ProgressBar instead of the default Unity Label property.
        //Yes, this is a thing you can do in custom Editor Windows.
        EditorGUI.ProgressBar(
            GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth - EditorGUIUtility.labelWidth, 20,
                EditorStyles.helpBox), propRadius.floatValue / 10,
            "Spawn Radius: " + propRadius.floatValue.ToString("N2"));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        //Creating a Slider and assigning the value of that slider to a serialized property
        //so Unity recognizes changes to it and updates the value of my 'radius' float to reflect those changes
        propRadius.floatValue = EditorGUILayout.Slider(propRadius.floatValue, 0.1f, 10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.ProgressBar(
            GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth - EditorGUIUtility.labelWidth, 20,
                EditorStyles.helpBox), propSpawnDistance.floatValue / 3,
            "Spawn Distance: " + propSpawnDistance.floatValue.ToString("N2"));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        propSpawnDistance.floatValue = EditorGUILayout.Slider(propSpawnDistance.floatValue, 0.2f, 3f);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        //Setting the color of the subsequently drawn Inspector GUI properties to a GreenIsh hue
        //to further visually differentiate them from the previous properties
        GUI.color = Color.Lerp(Color.green, Color.white, 0.7f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();
        //PropertyFields have all of the default Unity Inspector behaviours and GUI built in.
        //They're an easy way to avoid having to re-invent the wheel so I'm using one here to draw a Mesh field;
        //I'm only changing a couple parameters in order to make the field a bit bigger than it is by default for readability
        EditorGUILayout.PropertyField(propMesh, GUILayout.MinWidth(EditorGUIUtility.fieldWidth), GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        //Still draws the button no matter water but if it's clicked calls the ClearMesh() function
        if (GUILayout.Button("Clear Mesh"))
        {
            ClearMesh();
        }
        //Because these two Buttons are within the same Horizontal Scope
        //(They're between a 'EditorGUILayout.BeginHorizontal()' and a 'EditorGUILayout.EndHorizontal()')
        //they will be drawn on the ~same line~
        if (GUILayout.Button("Focus on Grass"))
        {
            FocusOnGrass();
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.EndVertical();

        #region Custom Editor Icon

        EditorGUI.BeginChangeCheck(); //using this method allows me to remove the grass icon from the Editor when there's no currently existent Grass object in the Inspector
        Grass grass = (Grass) FindObjectOfType(typeof(Grass)); //I KNOW. I WRITE THIS LINE WAY TOO MANY TIMES. I'LL FIX IT LATER.
        Editor gameObjectEditor = null;
        //Draws a nice grass sprite in the Editor Window
        //<b>***Drawing Nice Grass Icon In the Window Part 1***</b>
        Texture2D previewBackgroundTexture =
            AssetDatabase.LoadAssetAtPath("Assets/Icons/grass_sprite.png", typeof(Texture2D)) as Texture2D;
        //Shows the user which 'Grass' GameObject they're currently editing
        grass = (Grass) EditorGUILayout.ObjectField(grass, typeof(Grass), true);

        if (EditorGUI.EndChangeCheck())
        {
            if (gameObjectEditor != null)
            {
                DestroyImmediate(gameObjectEditor);
                Debug.LogError("CRITICAL ERROR. YOU DUN HECCED UP. WE SHOULD'VE NEVER GOTTEN HERE.");
            }
        }

        //<b>***Drawing Nice Grass Icon In the Window Part 2***</b>
        GUIStyle bgColor = new GUIStyle
        {
            normal =
            {
                background = previewBackgroundTexture
            }
        };
        //Tints the Grass Icon Green by changing the GUI color (default is white)
        //***Drawing Nice Grass Icon In the Window Part 3***
        GUI.color = Color.Lerp(Color.green, Color.white, 0.4f);
        //<summary> Clarifying how the icon drawing is Conditional
        //Checking to see if the current Grass object field has been assigned makes the final drawing of the grass icon below conditional on if the grass has been assigned
        //because that assignment took place in between a 'EditorGUI.BeginChangeCheck()' and 'EditorGUI.EndChangeCheck()'
        //the icon will only be drawn if there is currently an active Grass GameObject in the Unity Inspector.
        //This gives the environmental artist using the tool some visual feedback that everything has been set up correctly;
        //though this should be explicitly mentioned to the artist in the tool using a note in the inspector
        //which I will be adding to a later version
        //</summary>
        if (grass != null)
        {
            //should always still be null but I've somehow opened multiple editor windows in the past while editing code that made this not null?
            //having this check prevented Unity from crashing when I do that
            if (gameObjectEditor == null)

                gameObjectEditor = Editor.CreateEditor(grass); //if you only have one line under an 'if' statement you don't need to put it in braces {}
            gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, 80), bgColor); //***Drawing Nice Grass Icon In the Window Part 4*** Finally drawing the icon in a rectangle with the Width of the current Editor Window
        }

        //Setting the color of the Inspector back to a BlueIsh hue for later. Currently does nothing since no further GUI is drawn
        GUI.color = Color.Lerp(Color.cyan, Color.white, 0.7f);

        #endregion

        //if any of the serialized properties were changed, update the SerializedObject to reflect those changes
        if (so.ApplyModifiedProperties())
        {
            //redraw the SceneView to reflect those changes
            SceneView.RepaintAll();
        }
        
        //if you click left mouse button in the editor window
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
        GUI.FocusControl(null); //deselect previous selection
        Repaint(); //repaint the editor window UI (if you don't do this explicitly the editor visually lags behind on your selection. very annoying)
    }
    #endregion
    
    #region Scene Window GUI
    //<summary>
    //This method handles all of the SceneView specific behavior for this Editor tool
    //</summary>
    private void DuringSceneGUI(SceneView sceneView)
    {
        //Ensures that all newly drawn Scene Handles will have proper render sorting/zTesting
        //<a href="https://docs.unity3d.com/ScriptReference/Handles-zTest.html">More Info Here</a>
        Handles.zTest = CompareFunction.LessEqual;
        
       Transform camTransform = sceneView.camera.transform;
        
        //repaint the current sceneView on MouseMove
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        //This is the simplest way to detect if the user is holding a key from within an Editor script
        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        bool holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;

        //Get a ray from the current mouse position
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (!Physics.Raycast(mouseRay, out RaycastHit mouseHit)) return; //if the mouse ray doesn't hit anything, return out of this function (stops execution here)
        
        //Mouse movement updates
        if (Event.current.type == EventType.MouseMove && Event.current.delta.magnitude > 0.3f) //if the user moves the mouse more than a certain threshold
        {
            _randomPoints = PointSpawner.Points(radius, spawnDistance); //add new points to the _randomPoints list
            sceneView.Repaint(); //update the current sceneView window
        }
        
        //Scroll wheel updates
        if (Event.current.type == EventType.ScrollWheel && !holdingAlt && Physics.Raycast(mouseRay))
        {
            //Which direction is the user scrolling?
            float scrollDir = -Mathf.Sign(Event.current.delta.y);
            //The user isn't holding the Ctrl key
            if (!holdingCtrl) {
                //If the amount the user is scrolling doesn't exceed a certain threshold in either direction
                if (propRadius.floatValue * 1 + scrollDir * 0.1f < 10 &&
                    propRadius.floatValue * 1 + scrollDir * 0.1f > 0.1f)
                {
                    so.Update(); //telling Unity we're going to make changes to the ScriptableObject
                    //set a new value for the Radius in the EditorWindow's GUI
                    propRadius.floatValue = (float) Math.Round(propRadius.floatValue * 1 + scrollDir * 0.1f, 2,
                        MidpointRounding.AwayFromZero);
                    _randomPoints = PointSpawner.Points(radius, spawnDistance); //generate new points and add them to the _randomPoints list
                    sceneView.Repaint(); //update the current sceneView window
                    so.ApplyModifiedProperties(); //apply the changes we made to the ScriptableObject
                    Repaint(); //update the editor window
                }
            }
            //The user is holding the Ctrl key
            else
            {
                if (propSpawnDistance.floatValue * 1 + scrollDir * 0.1f < 3 &&
                    propSpawnDistance.floatValue * 1 + scrollDir * 0.1f > 0.2f)
                {
                    so.Update();
                    //set a new value for the SpawnDistance in the EditorWindow's GUI
                    propSpawnDistance.floatValue = (float) Math.Round(propSpawnDistance.floatValue * 1 + scrollDir * 0.1f, 2,
                        MidpointRounding.AwayFromZero);
                    _randomPoints = PointSpawner.Points(radius, spawnDistance);
                    sceneView.Repaint(); //updates the current sceneView window
                    so.ApplyModifiedProperties();
                    Repaint(); //updates the editor window
                }
            }
            //tell's Unity to 'consume' the event, don't let it 'fall through'
            //this stop's Unity's default behaviour of zooming in and out from happening
            //because the Event is terminated before Unity executes the default behaviours
            Event.current.Use();
        }

        //set up tangent space for the mouse raycast
        Vector3 hitNormal = mouseHit.normal;
        Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
        Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

        //helper function for calculating future raycasts based on the initial mouse raycast
        Ray GetTangentRay(Vector2 tangentSpacePos)
        {
            //fancy Matrix maths that converts localTangentSpace into WorldSpace
            Vector3 rayOrigin = mouseHit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y);
            const float offsetMargin = 2.0f; //allows for some margin of error above the raycast, avoids some visual glitching and allows the placement of points under other objects
            rayOrigin += hitNormal * offsetMargin;
            Vector3 rayDirection = -hitNormal;
            return new Ray(rayOrigin, rayDirection);
        }
        
        //draw the point Handles in the currently open sceneView
        foreach (Vector2 t in _randomPoints)    
        {
            //create ray for this point
            Ray pointRay = GetTangentRay(t);
            //raycast to find point on surface
            if (!Physics.Raycast(pointRay, out RaycastHit pointHit)) continue;
            //draw sphere on surface at the point
            DrawSphere(pointHit.point);
            //draw normal on surface from the point
            Handles.DrawAAPolyLine(pointHit.point, pointHit.point + pointHit.normal * 0.5f);
        }
        
        //Currently a local variable to this window.
        //***CAUSES KNOWN ISSUES if the user creates grass then closes the window and tries to create more grass***
        //This should eventually be put into the Grass class so that the point data remains even after the Editor window is closed
        //Once I do this I should also be able to implement the ability to 'erase' points in a certain area when the user right-clicks
        List<Vector3> hitPoints = new List<Vector3>();

        //spawn the points on click, unless the user is holding Alt, then let Unity do the default stuff
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !holdingAlt)
        {
            foreach (Vector2 t in _randomPoints)
            {
                //create ray for this point
                Ray pointRay = GetTangentRay(t);
                //raycast to find point on surface
                if (!Physics.Raycast(pointRay, out RaycastHit pointHit)) continue;
                //add this point to the hitPoints List
                hitPoints.Add(pointHit.point);
            }

            allMeshPoints.AddRange(hitPoints);
            UpdateMesh(mesh, allMeshPoints); //update the Grass mesh with the newly generated points
            if (mesh)
            {
                Undo.RecordObject(mesh, "Added new mesh vertices"); //currently not working, not sure why
            }
            else
            {
                Undo.RegisterCreatedObjectUndo(mesh, "Created new Mesh"); //also not yet working...
            }

            FocusOnGrass(); //necessary to 'consume' the UnityEvent EventType.MouseDown so that Unity doesn't try to select the clicked GameObject
            if (CheckForRelease()) //necessary to 'consume' the UnityEvent EventType.MouseUp so that Unity doesn't try to select the clicked GameObject
            {
                FocusOnGrass();
            }
        }

        #region Draw Scene Handles
        //draw an orientation gizmo at the mouseHit.point
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitTangent);
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitBitangent);
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitNormal);
        Handles.color = Color.white;
        
        //draw a 2D circle, somewhat adapted to the terrain
        const int circleDetail = 64;
        Vector3[] ringPoints = new Vector3[circleDetail + 1];
        for (int i = 0; i < circleDetail + 1; i++)
        {
            float t = i / (float) circleDetail;
            float angRad = t * TAU;
            Vector2 dir = new Vector2(Mathf.Cos(angRad),Mathf.Sin(angRad)) * radius;
            Ray r = GetTangentRay(dir);
            if (Physics.Raycast(r, out RaycastHit cHit))
            {
                ringPoints[i] = cHit.point + cHit.normal * 0.04f;
            }
            else
            {
                ringPoints[i] = r.origin;
            }
        }
        Handles.DrawAAPolyLine(ringPoints);
        #endregion
    }
    #endregion

    #region Helper Utilities

    #region Mesh Utilities
    private void UpdateMesh(Mesh m, List<Vector3> vertices)
    {
        AssignMesh();
        m.SetVertices(vertices);
        List<int> indices = new List<int>();
        if (indices.Count > 0)
        {
            indices.Clear();
        }
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }
        //this line is necessary in order to create a Mesh made up of nothing but points:
        m.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
        SaveMesh(m,m.name,false,true);
    }
    
    
    private static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        const string savePath = "Assets/Mesh/";
        string assetPath = savePath + name + ".asset";

        Mesh meshToSave = makeNewInstance ? Instantiate(mesh) as Mesh : mesh;
		
       if (optimizeMesh) 
           MeshUtility.Optimize(meshToSave); //not sure this actually does anything since the mesh is made up of nothing but points
       
       //Creates a string based on the existing path of the mesh .asset
       string newAssetPath = AssetDatabase.GetAssetPath(meshToSave);

       //AssetDatabase magic that updates the existing asset
       Mesh savedMesh = (Mesh) AssetDatabase.LoadAssetAtPath(newAssetPath, typeof(Mesh));
       if (!savedMesh) //if the mesh.asset doesn't yet exist
       {
           //Run this magic function that allows the user to create that asset and choose where
           SaveMeshFirst(mesh, name, makeNewInstance, optimizeMesh);
       }
       //Save the updated .asset
       if (AssetDatabase.Contains(meshToSave)) { AssetDatabase.SaveAssets(); }
       else { AssetDatabase.CreateAsset(meshToSave, assetPath); AssetDatabase.SaveAssets(); }
    }

    private void ClearMesh()
    {
        //just clears the vertices and mesh data
        _randomPoints.Clear();
        allMeshPoints.Clear();
        mesh.Clear();
        //DestroyMesh();
        Debug.Log("Cleared Mesh Vertices");
    }

    private void DestroyMesh()
    {
        //Using this for testing. This function will be deleted later
        Undo.DestroyObjectImmediate(mesh);
        AssetDatabase.DeleteAsset("Assets/Mesh/Grass.asset");
    }
    
    private static void SaveMeshFirst(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
        //Allows the user to create a new mesh.asset file and name and save it where they want to
        string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/Mesh", name, "asset");
            if (string.IsNullOrEmpty(path)) return;

            path = FileUtil.GetProjectRelativePath(path);

            Mesh meshToSave = (makeNewInstance) ? Instantiate(mesh) as Mesh : mesh;

            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);

            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    }
    #endregion

    #region Create New Object in Hierarchy View
    public static void CreateNewObject(Type type, string name)
    {
        GameObject go = CreateObject(type, name);
        
        // Ensure the GameObject gets automatically renamed if its name in the Hierarchy is not unique
        GameObjectUtility.EnsureUniqueNameForSibling(go);

        // Register the creation of the object in Unity's undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

        // Select that newly created object
        Selection.activeGameObject = go;
    }

    private static GameObject CreateObject(Type type, string name)
    { 
        string ASSET_PATH = "Assets/";
        if (type == typeof(Grass))
        {
            ASSET_PATH += "Mesh/" + name + ".asset";
        }
        // Automagically add the MeshFilter and MeshRenderer components, as well as the custom script
        GameObject go = CreateGameObject(name, typeof(MeshFilter), typeof(MeshRenderer), type);

        go.SetActive(false);

        go.GetComponent<MeshFilter>().mesh = (Mesh)AssetDatabase.LoadAssetAtPath(ASSET_PATH, typeof(Mesh));
        Renderer renderer = go.GetComponent<Renderer>();
        //setting all the shader properties
        renderer.sharedMaterial = new Material(Shader.Find("GeometryShader/Grass")) {name = "Grass-Material" };
        renderer.sharedMaterial.SetTexture(MainTex,(Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Materials/Grass/grassBlade.png", typeof(Texture2D)));
        renderer.sharedMaterial.SetTexture(AlphaTex,(Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Materials/Grass/grassBladeAlpha.png", typeof(Texture2D)));
        

        go.SetActive(true);
        return go;
    }

    private static GameObject CreateGameObject(string name, params Type[] types)
    {
        GameObject go = new GameObject(name);
        go.SetActive(false); //redundant but, ya know, just in case
        foreach (Type type in types)
        {
            go.AddComponent(type);
        }
        go.SetActive(true);
        return go;
    }
    #endregion
    
    private const float TAU = 6.28318530718f; //because I am not a member of the cult of PI
    
    private static void FocusOnGrass()
    {
        Grass grass = (Grass) FindObjectOfType(typeof(Grass)); //I really shouldn't call this line so many times
        if (Selection.activeObject != grass)
            Selection.activeObject = grass;
    }
    
    private static bool CheckForRelease()
    {
        //This function 'consumes' the current event and also returns 'true' if the user has let go of the Left Mouse Button
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                GUIUtility.hotControl = controlID;
                e.Use();
                break;
            case EventType.MouseUp:
                GUIUtility.hotControl = 0;
                e.Use();
                return true;
            case EventType.MouseDrag:
                GUIUtility.hotControl = controlID;
                e.Use();
                break;
        }
        return false;
    }
    
    private static void DrawSphere(Vector3 position)
    {
        Handles.SphereHandleCap(-1, position, Quaternion.identity, 0.1f,EventType.Repaint);
    }

    #endregion
}

public static class PointSpawner
{
    public static List<Vector2> Points(float radius, float pointDistance)
    {
        //generates a list of points using the Poisson Disc algorithm
        return PoissonDisc.GeneratePoints(pointDistance, radius);
    }
}