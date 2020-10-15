using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GrassPlacement : EditorWindow 
{
    [MenuItem("Tools/Grass _G")]
    public static void OpenGrassTool() => GetWindow<GrassPlacement>();
    
    //properties to be serialized
    public float radius = 2f;
    public float spawnDistance = 0.5f;
    public Mesh mesh;

    //serializedProperties
    private SerializedObject so;
    private SerializedProperty propRadius;
    private SerializedProperty propSpawnDistance;
    private SerializedProperty propMesh;
    
    //temporary point lists ******MOVE THESE OVER TO GRASS.CS**********
    //Currently deleted every time the GrassPlacement tool is closed and reopened
    private List<Vector2> _randomPoints = new List<Vector2>();
    private List<Vector3> allMeshPoints = new List<Vector3>();
    
    //GUI properties
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

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnFocus()
    {
        FocusOnGrass();
        focusedWindow.minSize = windowSize;
    }

    private void AssignMesh()
    {
        if ((Grass) FindObjectOfType(typeof(Grass)))
        {
            Grass grass = (Grass) FindObjectOfType(typeof(Grass));
            if (grass.GetComponent<MeshFilter>().sharedMesh)
            {
                mesh = grass.GetComponent<MeshFilter>().sharedMesh;
            }
            else
            {
                mesh = new Mesh
                {
                    name = "Grass"
                };
            }
        }
        else
        {
            mesh = new Mesh
            {
                name = "Grass"
            };
            if (!(Grass) FindObjectOfType(typeof(Grass)))
            {
                CreateNewObject(typeof(Grass), "Grass");
            }
            Grass grass = (Grass) FindObjectOfType(typeof(Grass));
            grass.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
    private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

    #region Editor Window GUI

    private void OnGUI()
    {
        so.Update();
        GUI.color = Color.Lerp(Color.cyan, Color.white, 0.7f);

        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Placement Options:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.ProgressBar(
            GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth - EditorGUIUtility.labelWidth, 20,
                EditorStyles.helpBox), propRadius.floatValue / 10,
            "Spawn Radius: " + propRadius.floatValue.ToString("N2"));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        propRadius.floatValue = EditorGUILayout.Slider(propRadius.floatValue, 0.1f, 10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.ProgressBar(
            GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth - EditorGUIUtility.labelWidth, 20,
                EditorStyles.helpBox), propSpawnDistance.floatValue / 3,
            "Spawn Distance: " + propSpawnDistance.floatValue.ToString("N2"));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        propSpawnDistance.floatValue = EditorGUILayout.Slider(propSpawnDistance.floatValue, 0.1f, 3f);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUI.color = Color.Lerp(Color.green, Color.white, 0.7f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(propMesh, GUILayout.MinWidth(EditorGUIUtility.fieldWidth), GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Mesh"))
        {
            ClearMesh();
        }

        if (GUILayout.Button("Focus on Grass"))
        {
            FocusOnGrass();
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.EndVertical();

        #region Custom Editor Icon

        EditorGUI.BeginChangeCheck();
        Grass grass = (Grass) FindObjectOfType(typeof(Grass));
        Editor gameObjectEditor = null;
        Texture2D previewBackgroundTexture =
            AssetDatabase.LoadAssetAtPath("Assets/Icons/grass_sprite.png", typeof(Texture2D)) as Texture2D;
        grass = (Grass) EditorGUILayout.ObjectField(grass, typeof(Grass), true);

        if (EditorGUI.EndChangeCheck())
        {
            if (gameObjectEditor != null) DestroyImmediate(gameObjectEditor);
        }

        GUIStyle bgColor = new GUIStyle
        {
            normal =
            {
                background = previewBackgroundTexture
            }
        };
        GUI.color = Color.Lerp(Color.green, Color.white, 0.4f);
        if (grass != null)
        {
            if (gameObjectEditor == null)

                gameObjectEditor = Editor.CreateEditor(grass);
            gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, 80),
                bgColor);
        }

        GUI.color = Color.Lerp(Color.cyan, Color.white, 0.7f);

        #endregion

        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
        
        //if you click left mouse button in the editor window
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
        GUI.FocusControl(null); //deselect previous selection
        Repaint(); //repaint the editor window UI
    }
    #endregion
    
    #region Scene Window GUI
    private void DuringSceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;
        
       Transform camTransform = sceneView.camera.transform;
        
        //repaint sceneView on MouseMove
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        bool holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (!Physics.Raycast(mouseRay, out RaycastHit mouseHit)) return;
        
        //mouse movement updates
        if (Event.current.type == EventType.MouseMove && Event.current.delta.magnitude > 0.3f)
        {
            _randomPoints = PointSpawner.Points(radius, spawnDistance);
            sceneView.Repaint(); //updates the current sceneView window
        }
        
        //scroll wheel updates
        if (Event.current.type == EventType.ScrollWheel && !holdingAlt && Physics.Raycast(mouseRay))
        {
            float scrollDir = -Mathf.Sign(Event.current.delta.y);
            if (!holdingCtrl) {
                if (propRadius.floatValue * 1 + scrollDir * 0.1f < 10 &&
                    propRadius.floatValue * 1 + scrollDir * 0.1f > 0.1f)
                {
                    so.Update();
                    propRadius.floatValue = (float) Math.Round(propRadius.floatValue * 1 + scrollDir * 0.1f, 2,
                        MidpointRounding.AwayFromZero);
                    _randomPoints = PointSpawner.Points(radius, spawnDistance);
                    sceneView.Repaint(); //updates the current sceneView window
                    so.ApplyModifiedProperties();
                    Repaint(); //updates the editor window
                }
            }
            else
            {
                if (propSpawnDistance.floatValue * 1 + scrollDir * 0.1f < 3 &&
                    propSpawnDistance.floatValue * 1 + scrollDir * 0.1f > 0.1f)
                {
                    so.Update();
                    propSpawnDistance.floatValue = (float) Math.Round(propSpawnDistance.floatValue * 1 + scrollDir * 0.1f, 2,
                        MidpointRounding.AwayFromZero);
                    _randomPoints = PointSpawner.Points(radius, spawnDistance);
                    sceneView.Repaint(); //updates the current sceneView window
                    so.ApplyModifiedProperties();
                    Repaint(); //updates the editor window
                }
            }
            Event.current.Use(); //consume the event, don't let it fall through
        }

        //set up tangent space
        Vector3 hitNormal = mouseHit.normal;
        Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
        Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

        Ray GetTangentRay(Vector2 tangentSpacePos)
        {
            Vector3 rayOrigin = mouseHit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y);
            const float offsetMargin = 2.0f;
            rayOrigin += hitNormal * offsetMargin;
            Vector3 rayDirection = -hitNormal;
            return new Ray(rayOrigin, rayDirection);
        }
        
        //draw the points
        foreach (Vector2 t in _randomPoints)    
        {
            //create ray for this point
            Ray pointRay = GetTangentRay(t);
            //raycast to find point on surface
            if (!Physics.Raycast(pointRay, out RaycastHit pointHit)) continue;
            //draw sphere and normal on surface
            DrawSphere(pointHit.point);
            Handles.DrawAAPolyLine(pointHit.point, pointHit.point + pointHit.normal * 0.5f);
        }
        
        List<Vector3> hitPoints = new List<Vector3>();

        //spawn the points on click
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
            UpdateMesh(mesh, allMeshPoints);
            if (mesh != null)
            {
                Undo.RecordObject(mesh, "Added new mesh vertices");
            }
            else
            {
                Undo.RegisterCreatedObjectUndo(mesh, "Created new Mesh");
            }

            FocusOnGrass();
            if (CheckForRelease())
            {
                FocusOnGrass();
            }
        }

        #region Draw Handles
        //draw orientation gizmo at the mouseHit
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitTangent);
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitBitangent);
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6,mouseHit.point, mouseHit.point + hitNormal);
        Handles.color = Color.white;
        
        //draw circle adapted to the terrain
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
        m.SetIndices(indices.ToArray(), MeshTopology.Points, 0);
        SaveMesh(m,m.name,false,true);
    }
    
    
    private static void SaveMesh(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh)
    {
        const string savePath = "Assets/Mesh/";
        string assetPath = savePath + name + ".asset";

        Mesh meshToSave = makeNewInstance ? Instantiate(mesh) as Mesh : mesh;
		
       if (optimizeMesh) 
           MeshUtility.Optimize(meshToSave);
       
       string newAssetPath = AssetDatabase.GetAssetPath(meshToSave);

       Mesh savedMesh = (Mesh) AssetDatabase.LoadAssetAtPath(newAssetPath, typeof(Mesh));
       if (!savedMesh)
       {
           SaveMeshFirst(mesh, name, makeNewInstance, optimizeMesh);
       }

       if (AssetDatabase.Contains(meshToSave)) { AssetDatabase.SaveAssets(); }
       else { AssetDatabase.CreateAsset(meshToSave, assetPath); AssetDatabase.SaveAssets(); }
    }

    private void ClearMesh()
    {
        _randomPoints.Clear();
        allMeshPoints.Clear();
        mesh.Clear();
        //DestroyMesh();
        Debug.Log("Cleared Mesh Vertices");
    }

    private void DestroyMesh()
    {
        Undo.DestroyObjectImmediate(mesh);
        AssetDatabase.DeleteAsset("Assets/Mesh/Grass.asset");
    }
    
    private static void SaveMeshFirst(Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
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
        //*********CHANGE TO CUSTOM GRASS SHADER LATER********
        renderer.sharedMaterial = new Material(Shader.Find("GeometryShader/Grass")) {name = "Grass-Material" };
        renderer.sharedMaterial.SetTexture(MainTex,(Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Materials/Grass/grassBlade.png", typeof(Texture2D)));
        renderer.sharedMaterial.SetTexture(AlphaTex,(Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Materials/Grass/grassBladeAlpha.png", typeof(Texture2D)));
        

        go.SetActive(true);
        return go;
    }

    private static GameObject CreateGameObject(string name, params Type[] types)
    {
        GameObject go = new GameObject(name);
        go.SetActive(false);
        foreach (Type type in types)
        {
            go.AddComponent(type);
        }
        go.SetActive(true);
        return go;
    }
    #endregion
    
    private const float TAU = 6.28318530718f;
    
    private static void FocusOnGrass()
    {
        Grass grass = (Grass) FindObjectOfType(typeof(Grass));
        if (Selection.activeObject != grass)
            Selection.activeObject = grass;
    }
    
    private static bool CheckForRelease()
    {
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
        return PoissonDisc.GeneratePoints(pointDistance, radius);
    }
}