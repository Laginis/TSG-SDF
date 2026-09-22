using Unity.Mathematics;

public static class SDF
{
    public static float Scene(float3 p, float3 groundCenter, float2 groundHalfSize,
        float3 cylinderCenter, float cylinderRadius, float cylinderHeight)
    {
        float ground = Ground(p, groundCenter, groundHalfSize);
        float cylinder = Cylinder(p, cylinderCenter, cylinderRadius, cylinderHeight);
        return math.min(ground, cylinder);
    }

    private static float Ground(float3 p, float3 center, float2 halfSize)
    {
        float3 local = p - center;

        float2 q = math.abs(local.xz) - halfSize;
        float distXZ = math.length(math.max(q, 0f)) + math.min(math.max(q.x, q.y), 0f);

        float distY = local.y;

        return math.max(distXZ, distY);
    }

    private static float Cylinder(float3 p, float3 center, float radius, float height)
    {
        float3 q = p - center;

        float d = math.length(q.xz) - radius;
        float y = math.abs(q.y) - height * 0.5f;

        return math.length(math.max(math.float2(d, y), 0f)) + math.min(math.max(d, y), 0f);
    }
}