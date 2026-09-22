using UnityEngine;
using UnityEngine.Rendering;

public class BallRenderer : MonoBehaviour
{
    [SerializeField] private BallSimulation simulation;
    [SerializeField] private SDFVolume sdfVolume;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private ShadowCastingMode shadowCasting = ShadowCastingMode.On;

    private GraphicsBuffer positionBuffer;
    private GraphicsBuffer argsBuffer;

    private MaterialPropertyBlock properties;

    private RenderParams renderParams;

    private void OnEnable()
    {
        simulation.BallCountChanged += RecreateBuffers;
    }

    private void OnDisable()
    {
        simulation.BallCountChanged -= RecreateBuffers;
    }

    private void Start()
    {
        RecreateBuffers();
    }

    private void RecreateBuffers()
    {
        ReleaseBuffers();

        int count = simulation.BallCount;

        positionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            count,
            sizeof(float) * 4
        );

        argsBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.IndirectArguments,
            1,
            GraphicsBuffer.IndirectDrawIndexedArgs.size
        );

        var args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

        args[0].indexCountPerInstance = mesh.GetIndexCount(0);
        args[0].instanceCount = (uint)count;

        argsBuffer.SetData(args);

        properties ??= new MaterialPropertyBlock();

        properties.SetBuffer("_BallPositions", positionBuffer);
        properties.SetFloat("_BallRadius", simulation.BallRadius);

        Bounds bounds = sdfVolume.Bounds;
        bounds.Expand(simulation.BallRadius * 2f);

        renderParams = new(material)
        {
            worldBounds = bounds,
            matProps = properties,
            shadowCastingMode = shadowCasting
        };
    }

    private void LateUpdate()
    {
        positionBuffer.SetData(simulation.BallPositions);
        Graphics.RenderMeshIndirect(renderParams, mesh, argsBuffer);
    }

    private void ReleaseBuffers()
    {
        positionBuffer?.Release();
        positionBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }
}