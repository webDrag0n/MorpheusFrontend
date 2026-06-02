using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to automatically configure ArticulationBody drives for G1 robot visualization.
/// This sets appropriate stiffness, damping, and force limits for smooth joint following.
///
/// Usage: Tools → Configure G1 Articulation Drives
/// </summary>
public class ConfigureG1Drives : EditorWindow
{
    private float stiffness = 10000f;
    private float damping = 1000f;
    private bool useMaxForceLimit = true;
    private float customForceLimit = 1000f;

    [MenuItem("Tools/Configure G1 Articulation Drives")]
    static void ShowWindow()
    {
        GetWindow<ConfigureG1Drives>("Configure G1 Drives");
    }

    void OnGUI()
    {
        GUILayout.Label("Articulation Drive Configuration", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        stiffness = EditorGUILayout.FloatField("Stiffness", stiffness);
        damping = EditorGUILayout.FloatField("Damping", damping);

        EditorGUILayout.Space();

        useMaxForceLimit = EditorGUILayout.Toggle("Use Max Force Limit", useMaxForceLimit);
        if (!useMaxForceLimit)
        {
            customForceLimit = EditorGUILayout.FloatField("Custom Force Limit", customForceLimit);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Configure All Articulation Bodies"))
        {
            ConfigureAllDrives();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This will configure all ArticulationBody components in the scene with the specified drive parameters. " +
            "Make sure your robot is in the scene before running this.",
            MessageType.Info
        );
    }

    void ConfigureAllDrives()
    {
        var bodies = FindObjectsOfType<ArticulationBody>();
        int configured = 0;
        float forceLimit = useMaxForceLimit ? float.MaxValue : customForceLimit;

        foreach (var body in bodies)
        {
            if (body.jointType == ArticulationJointType.RevoluteJoint)
            {
                bool modified = false;

                // Configure X drive (twist)
                if (body.twistLock == ArticulationDofLock.LimitedMotion)
                {
                    var drive = body.xDrive;
                    drive.stiffness = stiffness;
                    drive.damping = damping;
                    drive.forceLimit = forceLimit;
                    body.xDrive = drive;
                    modified = true;
                }

                // Configure Y drive (swing Y)
                if (body.swingYLock == ArticulationDofLock.LimitedMotion)
                {
                    var drive = body.yDrive;
                    drive.stiffness = stiffness;
                    drive.damping = damping;
                    drive.forceLimit = forceLimit;
                    body.yDrive = drive;
                    modified = true;
                }

                // Configure Z drive (swing Z)
                if (body.swingZLock == ArticulationDofLock.LimitedMotion)
                {
                    var drive = body.zDrive;
                    drive.stiffness = stiffness;
                    drive.damping = damping;
                    drive.forceLimit = forceLimit;
                    body.zDrive = drive;
                    modified = true;
                }

                if (modified)
                {
                    configured++;
                    EditorUtility.SetDirty(body);
                }
            }
        }

        Debug.Log($"<color=green>Successfully configured {configured} articulation body drives</color>");
        Debug.Log($"Settings: Stiffness={stiffness}, Damping={damping}, ForceLimit={forceLimit}");
    }
}
