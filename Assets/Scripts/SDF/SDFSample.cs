using Unity.Mathematics;

public struct SDFSample
{
    public float distance;
    public float3 normal;

    public SDFSample(float distance, float3 normal)
    {
        this.distance = distance;
        this.normal = normal;
    }
}