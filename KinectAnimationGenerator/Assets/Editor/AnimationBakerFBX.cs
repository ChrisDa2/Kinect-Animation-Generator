using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Formats.Fbx.Exporter;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AnimationBakerFBX : EditorWindow
{
    private string jsonPath = "Assets/KinectEnvironment/KinectCaptures/kinect_recording_data.json";
    private string clipName = "BakedAnimation";
    private GameObject characterPrefab;
    private string outputPath = "Assets/KinectEnvironment/GeneratedAnimations/";

    [MenuItem("Tools/Bake Avatar Animation -> FBX Export")]
    public static void ShowWindow() => GetWindow<AnimationBakerFBX>("Bake FBX with Animation");

    private void OnGUI()
    {
        GUILayout.Label("Bake: root.pos = avatarRoot.pos (no Y offset)\nroot.rot = avatarRoot.rot\nBake all bones as transform curves", EditorStyles.boldLabel);
        jsonPath = EditorGUILayout.TextField("JSON file path", jsonPath);
        clipName = EditorGUILayout.TextField("Clip name", clipName);
        characterPrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab (Humanoid)", characterPrefab, typeof(GameObject), false);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (GUILayout.Button("Create FBX with Animation")) BakeAndExportFBX();
    }

    private void BakeAndExportFBX()
    {
        if (!File.Exists(jsonPath))
        {
            EditorUtility.DisplayDialog("Error", "JSON not found", "OK");
            return;
        }
        if (characterPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign prefab", "OK");
            return;
        }

        AnimationClip clip = BakeAnimationClip();
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to create animation clip.", "OK");
            return;
        }

        // Delete muscle curves from the clip to avoid conflicts with the humanoid rig
        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (binding.type == typeof(Animator))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        // Create temporary character instance to apply the animation and export
        GameObject tempChar = Instantiate(characterPrefab);
        tempChar.name = clipName + "_Temp";

        Animator animator = tempChar.GetComponent<Animator>();
        if (animator == null)
        {
            DestroyImmediate(tempChar);
            EditorUtility.DisplayDialog("Error", "Prefab does not have an Animator component.", "OK");
            return;
        }

        AnimatorController controller = CreateTemporaryController(tempChar, clip);
        animator.runtimeAnimatorController = controller;
        animator.Update(0f);

        string fbxPath = Path.Combine(outputPath, clipName + ".fbx").Replace("\\", "/");
        ExportModelOptions exportOptions = new ExportModelOptions();
        exportOptions.ModelAnimIncludeOption = Include.ModelAndAnim;
        exportOptions.ExportFormat = ExportFormat.Binary;

        // Export and clean up temp files
        bool success = ExportToFBX(tempChar, fbxPath, exportOptions);

        DestroyImmediate(tempChar);
        string controllerPath = AssetDatabase.GetAssetPath(controller);
        if (!string.IsNullOrEmpty(controllerPath))
            AssetDatabase.DeleteAsset(controllerPath);
        string clipPath = AssetDatabase.GetAssetPath(clip);
        if (!string.IsNullOrEmpty(clipPath))
            AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!success)
        {
            EditorUtility.DisplayDialog("Error", "FBX export failed. Check the console for details.", "OK");
            return;
        }

        ConfigureImportedFBX(fbxPath);

        EditorUtility.DisplayDialog("Success",
            $"FBX exported to:\n{fbxPath}\n\n" +
            $"Frame rate: {clip.frameRate} fps\n" +
            $"Duration: {clip.length} s\n" +
            $"Keyframes: {Mathf.RoundToInt(clip.length * clip.frameRate)} frames\n\n" +
            "The FBX has been imported and configured with:\n" +
            "- Rig: Humanoid\n" +
            "- Root Transform Rotation: Based Upon Original\n" +
            "- Root Transform Position (Y): Bake Into Pose (Feet)\n" +
            "- Root Transform Position (XZ): Bake Into Pose (Hips)\n\n" +
            "Drag it into your scene.",
            "OK");
    }

    private bool ExportToFBX(GameObject root, string filePath, ExportModelOptions options)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            ModelExporter.ExportObject(filePath, root, options);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("FBX export error: " + e.Message);
            return false;
        }
    }

    private void ConfigureImportedFBX(string fbxPath)
    {
        AssetDatabase.Refresh();

        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("Could not find ModelImporter for: " + fbxPath);
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;

        ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
        if (clipAnimations == null || clipAnimations.Length == 0)
            clipAnimations = importer.defaultClipAnimations;

        for (int i = 0; i < clipAnimations.Length; i++)
        {
            var clip = clipAnimations[i];

            // Bake Y movement into pose, based on feet
            clip.lockRootHeightY = true;
            clip.heightFromFeet = true;

            // Bake XZ movement into pose, based on hips (center of mass)
            clip.lockRootPositionXZ = true;
            importer.motionNodeName = "Hips";

            // Keep rotation as root motion
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;

            // Ensure looping and mirroring
            clip.loopTime = true;
            clip.mirror = true;
        }

        importer.clipAnimations = clipAnimations;
        importer.SaveAndReimport();
    }

    private AnimatorController CreateTemporaryController(GameObject character, AnimationClip clip)
    {
        string tempPath = Path.Combine(outputPath, "_temp_controller.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(tempPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = new ChildAnimatorState[0];
        AnimatorState state = stateMachine.AddState(clipName);
        state.motion = clip;
        stateMachine.defaultState = state;
        AssetDatabase.SaveAssets();
        return controller;
    }

    private float Sanitize(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0f;
        return value;
    }

    private AnimationClip BakeAnimationClip()
    {
        string jsonContent = File.ReadAllText(jsonPath);
        RecordingData recording = JsonUtility.FromJson<RecordingData>(jsonContent);
        if (recording?.frames == null || recording.frames.Count == 0)
        {
            Debug.LogError("No frames.");
            return null;
        }

        recording.frames.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

        int frameCount = recording.frames.Count;
        float firstTimestamp = recording.frames[0].timestamp;
        float lastTimestamp = recording.frames[frameCount - 1].timestamp;
        float totalDuration = lastTimestamp - firstTimestamp;

        // Frame rate for export (30 fps)
        float exportFrameRate = 30f;
        int exportFrameCount = Mathf.FloorToInt(totalDuration * exportFrameRate) + 1;

        // Almacenar todos los valores de cada frame original para interpolar
        List<Vector3> rootPositions = new List<Vector3>();
        List<Quaternion> rootRotations = new List<Quaternion>();
        List<Dictionary<string, Vector3>> bonePositions = new List<Dictionary<string, Vector3>>();
        List<Dictionary<string, Quaternion>> boneRotations = new List<Dictionary<string, Quaternion>>();

        // Read data from original frames
        for (int i = 0; i < frameCount; i++)
        {
            var frame = recording.frames[i];
            Vector3 rootPos = frame.avatarRoot?.position ?? Vector3.zero;
            Quaternion rootRot = frame.avatarRoot?.rotation ?? Quaternion.identity;
            rootPositions.Add(rootPos);
            rootRotations.Add(rootRot);

            var bonePos = new Dictionary<string, Vector3>();
            var boneRot = new Dictionary<string, Quaternion>();
            if (frame.avatarJoints != null)
            {
                foreach (var joint in frame.avatarJoints)
                {
                    bonePos[joint.name] = joint.localPosition;
                    boneRot[joint.name] = joint.localRotation;
                }
            }
            bonePositions.Add(bonePos);
            boneRotations.Add(boneRot);
        }

        // Instanciate the character prefab temporarily to get bone paths
        GameObject tempChar = Instantiate(characterPrefab);
        tempChar.hideFlags = HideFlags.HideAndDontSave;
        Animator animator = tempChar.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isValid)
        {
            DestroyImmediate(tempChar);
            EditorUtility.DisplayDialog("Error", "Prefab must have a valid Humanoid Avatar.", "OK");
            return null;
        }

        Dictionary<string, Transform> boneMap = BuildBoneMap(tempChar);
        if (!boneMap.ContainsKey("HipCenter"))
        {
            DestroyImmediate(tempChar);
            EditorUtility.DisplayDialog("Error", "HipCenter bone not found.", "OK");
            return null;
        }

        Dictionary<string, string> bonePaths = new Dictionary<string, string>();
        foreach (var kvp in boneMap)
        {
            string path = GetRelativePath(tempChar.transform, kvp.Value);
            if (!string.IsNullOrEmpty(path))
                bonePaths[kvp.Key] = path;
        }

        // Curve lists for root
        List<Keyframe> rootPosX = new List<Keyframe>();
        List<Keyframe> rootPosY = new List<Keyframe>();
        List<Keyframe> rootPosZ = new List<Keyframe>();
        List<Keyframe> rootRotX = new List<Keyframe>();
        List<Keyframe> rootRotY = new List<Keyframe>();
        List<Keyframe> rootRotZ = new List<Keyframe>();
        List<Keyframe> rootRotW = new List<Keyframe>();

        Dictionary<string, Dictionary<string, List<Keyframe>>> boneCurves = new Dictionary<string, Dictionary<string, List<Keyframe>>>();
        foreach (var path in bonePaths.Values)
        {
            if (string.IsNullOrEmpty(path)) continue;
            boneCurves[path] = new Dictionary<string, List<Keyframe>>();
            boneCurves[path]["localPosition.x"] = new List<Keyframe>();
            boneCurves[path]["localPosition.y"] = new List<Keyframe>();
            boneCurves[path]["localPosition.z"] = new List<Keyframe>();
            boneCurves[path]["localRotation.x"] = new List<Keyframe>();
            boneCurves[path]["localRotation.y"] = new List<Keyframe>();
            boneCurves[path]["localRotation.z"] = new List<Keyframe>();
            boneCurves[path]["localRotation.w"] = new List<Keyframe>();
        }

        // calculate time interval for each exported frame
        float exportDeltaTime = 1f / exportFrameRate;

        // Generate keyframes for each exported frame
        for (int frameIdx = 0; frameIdx < exportFrameCount; frameIdx++)
        {
            float time = frameIdx * exportDeltaTime;
            // Ensure the last frame aligns with the total duration
            if (frameIdx == exportFrameCount - 1)
                time = totalDuration;

            // Find the two original frames surrounding the current time
            int startIdx = 0;
            for (int i = 0; i < frameCount - 1; i++)
            {
                float t0 = (recording.frames[i].timestamp - firstTimestamp);
                float t1 = (recording.frames[i + 1].timestamp - firstTimestamp);
                if (time >= t0 && time <= t1)
                {
                    startIdx = i;
                    break;
                }
                if (i == frameCount - 2)
                    startIdx = i;
            }

            float tStart = (recording.frames[startIdx].timestamp - firstTimestamp);
            float tEnd = (recording.frames[startIdx + 1].timestamp - firstTimestamp);
            float t = (tEnd - tStart) > 0.0001f ? (time - tStart) / (tEnd - tStart) : 0f;
            t = Mathf.Clamp01(t);

            // Interpolate root position
            Vector3 startRootPos = rootPositions[startIdx];
            Vector3 endRootPos = rootPositions[startIdx + 1];
            Vector3 rootPos = Vector3.Lerp(startRootPos, endRootPos, t);

            // Interpolate root rotation
            Quaternion startRootRot = rootRotations[startIdx];
            Quaternion endRootRot = rootRotations[startIdx + 1];
            Quaternion rootRot = Quaternion.Slerp(startRootRot, endRootRot, t);

            // Interpolate bone positions and rotations
            var startBonePos = bonePositions[startIdx];
            var endBonePos = bonePositions[startIdx + 1];
            var startBoneRot = boneRotations[startIdx];
            var endBoneRot = boneRotations[startIdx + 1];

            // Apply interpolated values to the temporary character for this frame
            foreach (var kvp in boneMap)
            {
                string boneName = kvp.Key;
                Transform bone = kvp.Value;

                Vector3 pos;
                Quaternion rot;
                if (startBonePos.ContainsKey(boneName) && endBonePos.ContainsKey(boneName))
                {
                    pos = Vector3.Lerp(startBonePos[boneName], endBonePos[boneName], t);
                    rot = Quaternion.Slerp(startBoneRot[boneName], endBoneRot[boneName], t);
                }
                else
                {
                    pos = Vector3.zero;
                    rot = Quaternion.identity;
                }

                bone.localPosition = pos;
                bone.localRotation = rot;
            }

            // Add root keyframes
            rootPosX.Add(new Keyframe(time, rootPos.x));
            rootPosY.Add(new Keyframe(time, rootPos.y));
            rootPosZ.Add(new Keyframe(time, rootPos.z));
            rootRotX.Add(new Keyframe(time, rootRot.x));
            rootRotY.Add(new Keyframe(time, rootRot.y));
            rootRotZ.Add(new Keyframe(time, rootRot.z));
            rootRotW.Add(new Keyframe(time, rootRot.w));

            // Add bone keyframes
            foreach (var kvp in boneMap)
            {
                string boneName = kvp.Key;
                Transform bone = kvp.Value;
                string path = bonePaths.ContainsKey(boneName) ? bonePaths[boneName] : null;
                if (string.IsNullOrEmpty(path)) continue;

                Vector3 pos = bone.localPosition;
                Quaternion rot = bone.localRotation;

                boneCurves[path]["localPosition.x"].Add(new Keyframe(time, pos.x));
                boneCurves[path]["localPosition.y"].Add(new Keyframe(time, pos.y));
                boneCurves[path]["localPosition.z"].Add(new Keyframe(time, pos.z));
                boneCurves[path]["localRotation.x"].Add(new Keyframe(time, rot.x));
                boneCurves[path]["localRotation.y"].Add(new Keyframe(time, rot.y));
                boneCurves[path]["localRotation.z"].Add(new Keyframe(time, rot.z));
                boneCurves[path]["localRotation.w"].Add(new Keyframe(time, rot.w));
            }
        }

        // Create the animation clip and assign curves
        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            legacy = false,
            frameRate = exportFrameRate,
            wrapMode = WrapMode.Loop
        };
        SerializedObject serializedClip = new SerializedObject(clip);
        serializedClip.FindProperty("m_AnimationClipSettings").FindPropertyRelative("m_LoopTime").boolValue = true;
        serializedClip.ApplyModifiedProperties();

        // Assign root curves
        clip.SetCurve("", typeof(Transform), "localPosition.x", new AnimationCurve(rootPosX.ToArray()));
        clip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(rootPosY.ToArray()));
        clip.SetCurve("", typeof(Transform), "localPosition.z", new AnimationCurve(rootPosZ.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.x", new AnimationCurve(rootRotX.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.y", new AnimationCurve(rootRotY.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.z", new AnimationCurve(rootRotZ.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.w", new AnimationCurve(rootRotW.ToArray()));

        foreach (var pathEntry in boneCurves)
        {
            string path = pathEntry.Key;
            foreach (var propEntry in pathEntry.Value)
            {
                string property = propEntry.Key;
                List<Keyframe> list = propEntry.Value;
                if (list.Count > 0)
                {
                    clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(list.ToArray()));
                }
            }
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = totalDuration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        DestroyImmediate(tempChar);
        return clip;
    }

    private string GetRelativePath(Transform root, Transform child)
    {
        if (root == child) return "";
        List<string> path = new List<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            path.Insert(0, current.name);
            current = current.parent;
        }
        return current == null ? null : string.Join("/", path);
    }

    private Dictionary<string, Transform> BuildBoneMap(GameObject character)
    {
        var map = new Dictionary<string, Transform>();
        Animator animator = character.GetComponent<Animator>();
        if (animator == null || animator.avatar == null) return map;

        var boneMapping = new Dictionary<string, HumanBodyBones>
        {
            { "HipCenter", HumanBodyBones.Hips },
            { "Spine", HumanBodyBones.Spine },
            { "ShoulderCenter", HumanBodyBones.Chest },
            { "Head", HumanBodyBones.Head },
            { "ShoulderLeft", HumanBodyBones.LeftShoulder },
            { "ElbowLeft", HumanBodyBones.LeftUpperArm },
            { "WristLeft", HumanBodyBones.LeftLowerArm },
            { "HandLeft", HumanBodyBones.LeftHand },
            { "ShoulderRight", HumanBodyBones.RightShoulder },
            { "ElbowRight", HumanBodyBones.RightUpperArm },
            { "WristRight", HumanBodyBones.RightLowerArm },
            { "HandRight", HumanBodyBones.RightHand },
            { "HipLeft", HumanBodyBones.LeftUpperLeg },
            { "KneeLeft", HumanBodyBones.LeftLowerLeg },
            { "AnkleLeft", HumanBodyBones.LeftFoot },
            { "FootLeft", HumanBodyBones.LeftToes },
            { "HipRight", HumanBodyBones.RightUpperLeg },
            { "KneeRight", HumanBodyBones.RightLowerLeg },
            { "AnkleRight", HumanBodyBones.RightFoot },
            { "FootRight", HumanBodyBones.RightToes }
        };

        foreach (var kvp in boneMapping)
        {
            Transform bone = animator.GetBoneTransform(kvp.Value);
            if (bone != null) map[kvp.Key] = bone;
            else Debug.LogWarning($"Bone not found: {kvp.Key}");
        }
        return map;
    }

    [System.Serializable] public class RecordingData { public int playerId; public float captureInterval; public List<FrameData> frames; }
    [System.Serializable] public class FrameData { public int frameNumber; public float timestamp; public string rgbImagePath; public string depthImagePath; public List<RawKinectJoint> rawJoints; public List<CubemanJoint> cubemanJoints; public List<AvatarJoint> avatarJoints; public AvatarRootTransform avatarRoot; }
    [System.Serializable] public class AvatarJoint { public string name; public Vector3 localPosition; public Quaternion localRotation; }
    [System.Serializable] public class AvatarRootTransform { public Vector3 position; public Quaternion rotation; }
    [System.Serializable] public class RawKinectJoint { public string name; public Vector3 position; public Quaternion rotation; public bool tracked; }
    [System.Serializable] public class CubemanJoint { }
}