using Unity.Collections;
using Unity.Mathematics;

public struct SDFVolumeData
{
    [ReadOnly] public NativeArray<float4> data;

    public int3 resolution;
    public float3 boundsMin;
    public float3 boundsSize;
    public float3 voxelSize;
}