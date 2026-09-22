using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BallSimulation : MonoBehaviour
{
    public event Action BallCountChanged;

    [SerializeField] private SDFVolume sdfVolume;

    [Header("Settings")]
    [SerializeField, Min(1)] private int ballCount = 10000;
    [SerializeField, Min(0.01f)] private float ballRadius = 0.5f;
    [SerializeField, Min(0)] private float restitution = 0.3f;
    [SerializeField, Min(0)] private float friction = 0.8f;
    [SerializeField, Min(0)] private float gravity = 9.8f;
    [SerializeField, Min(0f)] private Vector2 spawnMarginXZ = new(1f, 1f);
    [SerializeField, Min(0.01f)] private float spawnHeight = 3f;

    [field: SerializeField, Range(1, 8)] public int SubSteps { get; private set; } = 4;

    public int BallCount => ballCount;
    public NativeArray<float4> BallPositions => ballPositions;
    public float BallRadius => ballRadius;

    public float SimulationTimeMs { get; private set; }

    private NativeArray<float4> ballPositions;
    private NativeArray<float3> ballVelocities;
    private NativeArray<Random> randoms;

    private Bounds spawnBounds;

    private void Awake()
    {
        spawnBounds = CalculateSpawnBounds();
        CreateBuffers();
        Initialize();
    }

    public void SetBallCount(int count)
    {
        count = Mathf.Max(1, count);

        if (count == ballCount) return;

        DisposeBuffers();

        ballCount = count;

        CreateBuffers();
        Initialize();

        BallCountChanged?.Invoke();
    }

    public void Reset()
    {
        Initialize();
    }

    private void CreateBuffers()
    {
        ballPositions = new NativeArray<float4>(ballCount, Allocator.Persistent);
        ballVelocities = new NativeArray<float3>(ballCount, Allocator.Persistent);
        randoms = new NativeArray<Random>(ballCount, Allocator.Persistent);
    }

    private void DisposeBuffers()
    {
        if (ballPositions.IsCreated) ballPositions.Dispose();
        if (ballVelocities.IsCreated) ballVelocities.Dispose();
        if (randoms.IsCreated) randoms.Dispose();
    }

    private void Update()
    {
        BallSimulationJob job = new()
        {
            positions = ballPositions,
            velocities = ballVelocities,
            randoms = randoms,
            sdf = sdfVolume.Data,
            spawnMin = spawnBounds.min,
            spawnMax = spawnBounds.max,
            deltaTime = Time.deltaTime,
            gravity = new(0f, -gravity, 0f),
            restitution = restitution,
            friction = friction,
            radius = ballRadius,
            subSteps = SubSteps
        };

        float startTime = Time.realtimeSinceStartup;

        JobHandle handle = job.Schedule(ballPositions.Length, 64);

        handle.Complete();

        SimulationTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;
    }

    private void Initialize()
    {
        Random random = new(12345);

        for (int i = 0; i < ballPositions.Length; i++)
        {
            float3 position = new(
                random.NextFloat(spawnBounds.min.x, spawnBounds.max.x),
                random.NextFloat(spawnBounds.min.y, spawnBounds.max.y),
                random.NextFloat(spawnBounds.min.z, spawnBounds.max.z)
            );

            ballPositions[i] = new float4(position, 1f);
            ballVelocities[i] = float3.zero;
            randoms[i] = new((uint)(i + 1));
        }
    }

    private Bounds CalculateSpawnBounds()
    {
        Bounds sdfBounds = sdfVolume.Bounds;

        float3 min = (float3)sdfBounds.min + (float3)ballRadius;
        float3 max = (float3)sdfBounds.max - (float3)ballRadius;

        min.x += spawnMarginXZ.x;
        max.x -= spawnMarginXZ.x;

        min.z += spawnMarginXZ.y;
        max.z -= spawnMarginXZ.y;

        min.y = max.y - spawnHeight;

        return new Bounds(
            (min + max) * 0.5f,
            max - min
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (sdfVolume == null || !sdfVolume.IsValid) return;

        spawnBounds = CalculateSpawnBounds();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            spawnBounds.center,
            spawnBounds.size
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            sdfVolume.Bounds.center,
            sdfVolume.Bounds.size
        );
    }

    private void OnDestroy()
    {
        DisposeBuffers();
    }
}
