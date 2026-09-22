using Unity.Mathematics;

public static class SDFSampler
{
    public static SDFSample Sample(in SDFVolumeData sdf, float3 worldPosition)
    {
        float3 gridPosition = (worldPosition - sdf.boundsMin) / sdf.voxelSize - 0.5f;

        bool outside =
            math.any(gridPosition < -0.5f)
            || math.any(gridPosition > (float3)sdf.resolution - 0.5f);

        if (outside)
        {
            return new SDFSample(
                float.PositiveInfinity,
                new float3(0, 1, 0)
            );
        }

        gridPosition = math.clamp(
            gridPosition,
            0f,
            (float3)sdf.resolution - 1f
        );

        int3 min = (int3)math.floor(gridPosition);
        int3 max = math.min(min + 1, sdf.resolution - 1);

        float3 t = math.frac(gridPosition);

        float4 v000 = GetValue(sdf, min.x, min.y, min.z);
        float4 v100 = GetValue(sdf, max.x, min.y, min.z);
        float4 v010 = GetValue(sdf, min.x, max.y, min.z);
        float4 v110 = GetValue(sdf, max.x, max.y, min.z);

        float4 v001 = GetValue(sdf, min.x, min.y, max.z);
        float4 v101 = GetValue(sdf, max.x, min.y, max.z);
        float4 v011 = GetValue(sdf, min.x, max.y, max.z);
        float4 v111 = GetValue(sdf, max.x, max.y, max.z);

        float4 v00 = math.lerp(v000, v100, t.x);
        float4 v10 = math.lerp(v010, v110, t.x);
        float4 v01 = math.lerp(v001, v101, t.x);
        float4 v11 = math.lerp(v011, v111, t.x);

        float4 v0 = math.lerp(v00, v10, t.y);
        float4 v1 = math.lerp(v01, v11, t.y);

        float4 value = math.lerp(v0, v1, t.z);

        return new SDFSample(
            value.x,
            math.normalizesafe(value.yzw, new float3(0f, 1f, 0f))
        );
    }

    private static float4 GetValue(in SDFVolumeData sdf, int x, int y, int z)
    {
        int index =
            x +
            y * sdf.resolution.x +
            z * sdf.resolution.x * sdf.resolution.y;

        return sdf.data[index];
    }
}
