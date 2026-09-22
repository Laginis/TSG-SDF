using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private BallSimulation simulation;
    [SerializeField] private SDFVolume sdfVolume;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    private bool stylesCreated;

    private void CreateStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14
        };
    }

    private void OnGUI()
    {
        if (!sdfVolume.IsValid) return;

        if (!stylesCreated)
        {
            CreateStyles();
            stylesCreated = true;
        }

        const float width = 350f;
        const float height = 225f;
        const float padding = 10f;

        Rect area = new(
            10f,
            10f,
            width,
            height
        );

        GUI.Box(area, GUIContent.none, boxStyle);

        GUILayout.BeginArea(
            new Rect(
                area.x + padding,
                area.y + padding,
                area.width - padding * 2f,
                area.height - padding * 2f
            )
        );

        DrawStats();

        GUILayout.Space(8f);

        DrawButtons();

        GUILayout.EndArea();
    }

    private void DrawStats()
    {
        float frameTime = Time.unscaledDeltaTime * 1000f;
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

        GUILayout.Label(
            $"Balls: {simulation.BallCount:N0}",
            labelStyle
        );

        GUILayout.Label(
            $"Frame: {frameTime:F2} ms ({fps:F0} FPS)",
            labelStyle
        );

        GUILayout.Label(
            $"Sim (schedule to join): " +
            $"{simulation.SimulationTimeMs:F2} ms",
            labelStyle
        );

        GUILayout.Label(
            $"Substeps: {simulation.SubSteps}  " +
            $"Radius: {simulation.BallRadius:F3}",
            labelStyle
        );

        GUILayout.Label(
            $"SDF: " +
            $"{sdfVolume.Data.resolution.x} x " +
            $"{sdfVolume.Data.resolution.y} x " +
            $"{sdfVolume.Data.resolution.z}",
            labelStyle
        );

        GUILayout.Label(
            $"Voxel Size: " +
            $"({sdfVolume.Data.voxelSize.x:F3}, " +
            $" {sdfVolume.Data.voxelSize.y:F3}, " +
            $" {sdfVolume.Data.voxelSize.z:F3})",
            labelStyle
        );
    }

    private void DrawButtons()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("/2", buttonStyle))
        {
            simulation.SetBallCount(
                Mathf.Max(1, simulation.BallCount / 2)
            );
        }

        if (GUILayout.Button("x2", buttonStyle))
        {
            simulation.SetBallCount(simulation.BallCount * 2);
        }

        if (GUILayout.Button("Reset", buttonStyle))
        {
            simulation.Reset();
        }

        GUILayout.EndHorizontal();
    }
}