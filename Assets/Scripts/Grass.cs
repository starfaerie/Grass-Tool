using UnityEngine;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class Grass : MonoBehaviour
{
    //flagging this variable with [SerializeField] ensures that it shows up in the Unity inspector even though it's a private variable
    [SerializeField] private bool hideComponents = false;
    private Material _material;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    private void OnEnable()
    {
        AssignVariables();
    }

    private void AssignVariables()
    {
        //if (!_meshRenderer) is a more concise way of writing:
        //if (_meshRenderer == null)
        //that instead of actually comparing the value to null merely checks if the variable has been assigned to
        //(which is muuuch faster than actually comparing to null)
        if (!_meshRenderer) _meshRenderer = GetComponent<MeshRenderer>();
        if (!_meshFilter) _meshFilter = GetComponent<MeshFilter>();
        _material = _meshRenderer.sharedMaterial;
    }
    
    private void OnValidate()
    {
        if (!_meshRenderer || !_meshFilter || !_material)
            AssignVariables();
        //the '#if UNITY_EDITOR' block ensures that these lines will not be compiled in the final build,
        //which avoids compiling and running code not relevant to the actual game in the final build
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        SetHideFlags();
#endif
    }
    
    private void SetHideFlags()
    {
        if (!_meshRenderer || !_meshFilter || !_material)
            AssignVariables();
        //<summary> .hideFlags explanation:
        //.hideFlags is a property that all Components have in Unity. By default it's set to 'HideFlags.None' which ensures that the components show up in the Inspector
        //I'm using the .hideFlags property to remove the visual clutter from components that don't need to be changed by an environmental artist using the grass drawing tool
        //These lines could have been written like this:
        //
        //    if (hideComponents == true)
        //    {
        //        _meshRenderer.hideFlags = HideFlags.HideInInspector;
        //        _meshFilter.hideFlags = HideFlags.HideInInspector;
        //        _material.hideFlags = HideFlags.HideInInspector;
        //    }
        //    else
        //    {
        //        _meshRenderer.hideFlags = HideFlags.None;
        //        _meshFilter.hideFlags = HideFlags.None;
        //        _material.hideFlags = HideFlags.None;
        //    }
        //
        //<summary>
        _meshRenderer.hideFlags = hideComponents ? HideFlags.HideInInspector : HideFlags.None;
        _meshFilter.hideFlags = hideComponents ? HideFlags.HideInInspector : HideFlags.None;
        _material.hideFlags = hideComponents ? HideFlags.HideInInspector : HideFlags.None;
    }
}
