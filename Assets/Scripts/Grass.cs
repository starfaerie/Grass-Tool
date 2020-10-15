using UnityEngine;

[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class Grass : MonoBehaviour
{
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
        if (!_meshRenderer) _meshRenderer = GetComponent<MeshRenderer>();
        if (!_meshFilter) _meshFilter = GetComponent<MeshFilter>();
        _material = _meshRenderer.sharedMaterial;
    }
    
    private void OnValidate()
    {
        if (!_meshRenderer || !_meshFilter || !_material)
            AssignVariables();
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        SetHideFlags();
#endif
    }
    
    private void SetHideFlags()
    {
        if (!_meshRenderer || !_meshFilter || !_material)
            AssignVariables();
        _meshRenderer.hideFlags = (hideComponents) ? HideFlags.HideInInspector : HideFlags.None;
        _meshFilter.hideFlags = (hideComponents) ? HideFlags.HideInInspector : HideFlags.None;
        _material.hideFlags = (hideComponents) ? HideFlags.HideInInspector : HideFlags.None;
    }
}
