using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneSDF", menuName = "SO/SDF/Scene SDF")]
public class SDFDataSO : ScriptableObject
{
    public Texture3D Texture;
    public Bounds Bounds;
    public int3 Resolution;
    public float3 VoxelSize;
}