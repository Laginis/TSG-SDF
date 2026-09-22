using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public struct BallSimulationJob : IJobParallelFor
{
    public NativeArray<float4> positions;
    public NativeArray<float3> velocities;
    public NativeArray<Random> randoms;

    [ReadOnly] public SDFVolumeData sdf;

    public float3 spawnMin;
    public float3 spawnMax;

    public float deltaTime;
    public float3 gravity;
    public float restitution;
    public float friction;
    public float radius;

    public int subSteps;

    public void Execute(int index)
    {
        float3 position = positions[index].xyz;
        float3 velocity = velocities[index];

        Random random = randoms[index];

        float subDt = deltaTime / subSteps;

        for (int i = 0; i < subSteps; i++)
        {
            Simulate(
                ref position,
                ref velocity,
                ref random,
                subDt
            );
        }

        positions[index] = new float4(position, 1f);
        velocities[index] = velocity;
        randoms[index] = random;
    }

    private void Simulate(ref float3 position, ref float3 velocity, ref Random random, float dt)
    {
        velocity += gravity * dt;
        position += velocity * dt;

        if (IsOutsideVolume(position))
        {
            position = random.NextFloat3(spawnMin, spawnMax);
            velocity = float3.zero;
            return;
        }

        SDFSample sample = SDFSampler.Sample(sdf, position);

        if (sample.distance < radius)
        {
            float penetration = radius - sample.distance;

            position += sample.normal * penetration;

            float velocityIntoSurface = math.dot(velocity, sample.normal);

            if (velocityIntoSurface < 0f)
            {
                velocity -=
                    (1f + restitution) *
                    velocityIntoSurface *
                    sample.normal;
            }

            float3 tangentVelocity = velocity - math.dot(velocity, sample.normal) * sample.normal;
            velocity -= dt * friction * tangentVelocity;
        }
    }

    private bool IsOutsideVolume(float3 worldPosition)
    {
        float3 uvw = (worldPosition - sdf.boundsMin) / sdf.boundsSize;
        return math.any(uvw < 0f) || math.any(uvw > 1f);
    }
}