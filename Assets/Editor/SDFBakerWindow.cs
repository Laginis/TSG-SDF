using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class SDFBakerWindow : EditorWindow
{
    private MeshRenderer plane;
    private MeshRenderer cylinder;

    private float voxelSize = 0.05f;

    private Vector3 boundsCenter = new(0, 3, 0);
    private Vector3 boundsSize = new(12, 8, 12);

    private const string OUTPUT_FOLDER = "Assets/SDF";
    private string assetName = "SceneSDF";


    [MenuItem("Tools/SDF/Baker")]
    public static void Open()
    {
        SDFBakerWindow window = GetWindow<SDFBakerWindow>("SDF Baker");

        SceneView.duringSceneGui -= window.OnSceneGUI;
        SceneView.duringSceneGui += window.OnSceneGUI;

        window.Show();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Bounds
        Handles.color = Color.cyan;
        Bounds bounds = new(boundsCenter, boundsSize);
        Handles.DrawWireCube(bounds.center, bounds.size);

        // Voxel
        Handles.color = Color.purple;
        Vector3 voxel = new(voxelSize, voxelSize, voxelSize);
        Vector3 voxelPos = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z) + new Vector3(voxelSize, -voxelSize, voxelSize) / 2f;
        Handles.DrawWireCube(voxelPos, voxel);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Geometry", EditorStyles.boldLabel);

        plane = (MeshRenderer)EditorGUILayout.ObjectField("Plane", plane, typeof(MeshRenderer), true);
        cylinder = (MeshRenderer)EditorGUILayout.ObjectField("Cylinder", cylinder, typeof(MeshRenderer), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);

        voxelSize = EditorGUILayout.FloatField("Voxel Size", voxelSize);
        voxelSize = Mathf.Max(0.001f, voxelSize);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);

        boundsCenter = EditorGUILayout.Vector3Field("Center", boundsCenter);
        boundsSize = EditorGUILayout.Vector3Field("Size", boundsSize);

        EditorGUILayout.Space();

        ShowCalculatedResolution();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"Output Folder", $"{OUTPUT_FOLDER}");

        assetName = EditorGUILayout.TextField("Asset Name", assetName);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(plane == null || cylinder == null))
        {
            if (GUILayout.Button("Bake SDF", GUILayout.Height(35)))
            {
                Bake();
            }
        }
    }

    private void ShowCalculatedResolution()
    {
        int3 resolution = CalculateResolution();
        EditorGUILayout.LabelField("Resolution", $"{resolution.x} x {resolution.y} x {resolution.z}");
    }

    private int3 CalculateResolution()
    {
        return new int3(
            Mathf.CeilToInt(boundsSize.x / voxelSize),
            Mathf.CeilToInt(boundsSize.y / voxelSize),
            Mathf.CeilToInt(boundsSize.z / voxelSize)
        );
    }

    private void Bake()
    {
        if (plane == null || cylinder == null)
        {
            Debug.LogError("SDF Baker: Plane and Cylinder are required.");
            return;
        }

        Bounds sceneBounds = new(boundsCenter, boundsSize);

        int3 resolution = CalculateResolution();
        float3 actualVoxelSize = (float3)sceneBounds.size / resolution;

        float epsilon = math.cmin(actualVoxelSize) * 0.5f;

        Texture3D texture = new(resolution.x, resolution.y, resolution.z, TextureFormat.RGBAFloat, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float3 groundCenter = plane.bounds.center;
        float2 groundHalfSize = new(plane.bounds.extents.x, plane.bounds.extents.z);

        float3 cylinderCenter = cylinder.transform.position;
        float cylinderHeight = 2f * cylinder.transform.lossyScale.y;
        float cylinderRadius = 0.5f * math.max(cylinder.transform.lossyScale.x, cylinder.transform.lossyScale.z);

        int voxelCount = resolution.x * resolution.y * resolution.z;

        Color[] pixels = new Color[voxelCount];

        for (int z = 0; z < resolution.z; z++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    float3 uvw = new(
                        (x + 0.5f) / resolution.x,
                        (y + 0.5f) / resolution.y,
                        (z + 0.5f) / resolution.z
                    );

                    float3 worldPosition = (float3)sceneBounds.min + uvw * (float3)sceneBounds.size;

                    float distance =
                        SDF.Scene(
                            worldPosition,
                            groundCenter,
                            groundHalfSize,
                            cylinderCenter,
                            cylinderRadius,
                            cylinderHeight
                        );

                    float3 normal =
                        CalculateNormal(
                            worldPosition,
                            epsilon,
                            groundCenter,
                            groundHalfSize,
                            cylinderCenter,
                            cylinderRadius,
                            cylinderHeight
                        );

                    int index =
                        x +
                        y * resolution.x +
                        z * resolution.x * resolution.y;

                    pixels[index] = new Color(
                        distance,
                        normal.x,
                        normal.y,
                        normal.z
                    );
                }
            }

            EditorUtility.DisplayProgressBar(
                "Baking SDF",
                $"Slice {z + 1}/{resolution.z}",
                (float)(z + 1) / resolution.z
            );
        }

        EditorUtility.ClearProgressBar();

        texture.SetPixels(pixels);
        texture.Apply();

        CreateAsset(texture, sceneBounds, resolution, actualVoxelSize);

        Debug.Log(
            $"SDF baked successfully.\n" +
            $"Resolution: {resolution.x} x {resolution.y} x {resolution.z}\n" +
            $"Voxel size: {voxelSize}\n" +
            $"Bounds: {sceneBounds}"
        );
    }

    private float3 CalculateNormal(
        float3 p,
        float epsilon,
        float3 groundCenter,
        float2 groundHalfSize,
        float3 cylinderCenter,
        float cylinderRadius,
        float cylinderHeight)
    {
        float x =
            SDF.Scene(
                p + new float3(epsilon, 0, 0),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            )
            -
            SDF.Scene(
                p - new float3(epsilon, 0, 0),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            );

        float y =
            SDF.Scene(
                p + new float3(0, epsilon, 0),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            )
            -
            SDF.Scene(
                p - new float3(0, epsilon, 0),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            );

        float z =
            SDF.Scene(
                p + new float3(0, 0, epsilon),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            )
            -
            SDF.Scene(
                p - new float3(0, 0, epsilon),
                groundCenter,
                groundHalfSize,
                cylinderCenter,
                cylinderRadius,
                cylinderHeight
            );

        return math.normalize(
            new float3(x, y, z)
        );
    }

    private void CreateAsset(Texture3D texture, Bounds bounds, int3 resolution, float3 voxelSize)
    {
        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", "SDF");
        }

        string texturePath = $"{OUTPUT_FOLDER}/{assetName}_Texture.asset";
        string sdfPath = $"{OUTPUT_FOLDER}/{assetName}.asset";

        AssetDatabase.DeleteAsset(texturePath);
        AssetDatabase.DeleteAsset(sdfPath);

        AssetDatabase.CreateAsset(texture, texturePath);

        SDFDataSO asset = CreateInstance<SDFDataSO>();

        asset.Texture = texture;
        asset.Bounds = bounds;

        asset.Resolution.x = resolution.x;
        asset.Resolution.y = resolution.y;
        asset.Resolution.z = resolution.z;

        asset.VoxelSize = voxelSize;

        AssetDatabase.CreateAsset(asset, sdfPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
    }
}
