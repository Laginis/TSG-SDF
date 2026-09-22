using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SDFVolume : MonoBehaviour
{
    [SerializeField] private SDFDataSO sdf;

    private NativeArray<float4> data;

    public bool IsValid => sdf != null && sdf.Texture != null;
    public Bounds Bounds => sdf.Bounds;

    public SDFVolumeData Data => new()
    {
        data = data,
        resolution = sdf.Resolution,
        boundsMin = sdf.Bounds.min,
        boundsSize = sdf.Bounds.size,
        voxelSize = sdf.VoxelSize
    };

    private void Awake()
    {
        if (!IsValid)
        {
            Debug.LogError($"{nameof(SDFVolume)} requires a valid SDFDataSO.", this);
            enabled = false;
            return;
        }

        NativeArray<float4> source = sdf.Texture.GetPixelData<float4>(0);
        data = new NativeArray<float4>(source.Length, Allocator.Persistent);
        NativeArray<float4>.Copy(source, data);
    }

    private void OnDestroy()
    {
        if (data.IsCreated) data.Dispose();
    }
}