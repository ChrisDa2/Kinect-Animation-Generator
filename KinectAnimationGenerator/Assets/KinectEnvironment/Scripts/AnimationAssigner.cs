using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

[ExecuteAlways]
public class AnimationAssigner : MonoBehaviour
{
    public GameObject ninjaKinect;

    void OnEnable()
    {
        AssignAllAnimations();
    }

    [ContextMenu("Assign Animations Now")]
    public void AssignAllAnimations()
    {
        // FBX file contains animation clip with same name as the file
        AssignClipToNinjaFBX(ninjaKinect, "Assets/KinectEnvironment/GeneratedAnimations/BakedAnimation.fbx", "AnimContKinect");
    }

    private void AssignClipToNinjaFBX(GameObject ninja, string fbxPath, string controllerName)
    {
        if (ninja == null) return;

        Animator animator = ninja.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"{ninja.name} has no Animator component.");
            return;
        }

        // Load all sub-assets inside the FBX file
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip clip = null;
        string clipName = Path.GetFileNameWithoutExtension(fbxPath);

        // First, try to find a clip whose name matches the FBX file name
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip c && c.name == clipName)
            {
                clip = c;
                break;
            }
        }

        // If not found, take the first AnimationClip we encounter
        if (clip == null)
        {
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip c)
                {
                    clip = c;
                    break;
                }
            }
        }

        if (clip == null)
        {
            Debug.LogError($"No AnimationClip found in FBX file: {fbxPath}");
            return;
        }

        AssignClipToAnimator(animator, clip, controllerName);
    }

    private void AssignClipToAnimator(Animator animator, AnimationClip clip, string controllerName)
    {
        // Make the clip loop
        clip.wrapMode = WrapMode.Loop;

        // Optionally disable root motion on the Animator
        //animator.applyRootMotion = false;

        // Create a new AnimatorController
        AnimatorController controller = new AnimatorController();
        controller.name = controllerName;

        // Ensure at least one layer exists
        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        // Get the first layer and its state machine
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        // Add a state with the clip and set it as default
        AnimatorState state = stateMachine.AddState("Animation");
        state.motion = clip;
        stateMachine.defaultState = state;

        // Assign the controller to the Animator
        animator.runtimeAnimatorController = controller;

        Debug.Log($"Assigned {clip.name} to {animator.gameObject.name} (controller: {controllerName})");
    }
}