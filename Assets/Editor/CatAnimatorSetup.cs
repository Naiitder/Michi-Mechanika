using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CatAnimatorSetup
{
    const string ControllerPath = "Assets/Art/Models/Character/Player/CatAnimController.controller";
    const string ClimbClipsPath = "Assets/Art/Animations/Movement/PlayerCliffAnimations.fbx";
    const string ClipPrefix = "Armature|";

    static readonly string[] FloorStates = { "Idle", "Jump_L", "Jump_R", "Jump_L_To_Idle", "Jump_R_To_Idle" };

    static readonly (string state, string clip)[] ClimbStates =
    {
        ("Climb_Idle", "Climb_Idle"),
        ("Climb_Up", "Climb_Up"),
        ("Climb_Down", "Climb_Down"),
        ("Climb_Left", "Climb_Left"),
        ("Climb_Right", "Climb_Right"),
        ("Climb_Up_To_Idle", "ClimbUp_to_Idle"),
        ("Climb_Down_To_Idle", "ClimbDown_to_Idle"),
        ("Climb_Left_To_Idle", "ClimbLeft_to_Idle"),
        ("Climb_Right_To_Idle", "ClimbRight_to_Idle"),
        ("Floor_To_ClimbUp", "FloorToClimb_Up"),
        ("Floor_To_ClimbDown", "FloorToClimb_Down"),
        ("Climb_To_Floor_Up", "ClimbToFloor_Up"),
        ("Climb_To_Floor_Down", "ClimbToFloor_Down"),
    };

    static readonly (string from, string to, float exitTime, float duration)[] Transitions =
    {
        ("Jump_L", "Jump_L_To_Idle", 0.75f, 0.25f),
        ("Jump_R", "Jump_R_To_Idle", 0.75f, 0.25f),
        ("Jump_L_To_Idle", "Idle", 0.75f, 0.25f),
        ("Jump_R_To_Idle", "Idle", 0.75f, 0.25f),
        ("Climb_Up", "Climb_Up_To_Idle", 0.85f, 0.1f),
        ("Climb_Down", "Climb_Down_To_Idle", 0.85f, 0.1f),
        ("Climb_Left", "Climb_Left_To_Idle", 0.85f, 0.1f),
        ("Climb_Right", "Climb_Right_To_Idle", 0.85f, 0.1f),
        ("Climb_Up_To_Idle", "Climb_Idle", 0.8f, 0.15f),
        ("Climb_Down_To_Idle", "Climb_Idle", 0.8f, 0.15f),
        ("Climb_Left_To_Idle", "Climb_Idle", 0.8f, 0.15f),
        ("Climb_Right_To_Idle", "Climb_Idle", 0.8f, 0.15f),
        ("Floor_To_ClimbUp", "Climb_Idle", 0.85f, 0.2f),
        ("Floor_To_ClimbDown", "Climb_Idle", 0.85f, 0.2f),
        ("Climb_To_Floor_Up", "Idle", 0.85f, 0.15f),
        ("Climb_To_Floor_Down", "Idle", 0.85f, 0.15f),
    };

    static readonly string[] KeptParameters = { "isDead", "attack" };

    [MenuItem("Tools/Michi/Rebuild Cat Animator")]
    public static void Rebuild()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("CatAnimatorSetup: no se encuentra " + ControllerPath);
            return;
        }

        EnsureClipImported("Climb_Idle", 0, 240, true);
        Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(ClimbClipsPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .GroupBy(c => c.name)
            .ToDictionary(g => g.Key, g => g.First());

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        List<AnimatorStateMachine> machines = new List<AnimatorStateMachine>();
        CollectMachines(root, machines);

        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();
        foreach (AnimatorStateMachine machine in machines)
            foreach (ChildAnimatorState child in machine.states)
                if (!states.ContainsKey(child.state.name)) states.Add(child.state.name, child.state);

        AnimatorStateMachine floorMachine = machines.FirstOrDefault(m => m.name == "Floor") ?? root;
        AnimatorStateMachine climbMachine = machines.FirstOrDefault(m => m.name == "Climb") ?? root;

        List<string> problems = new List<string>();
        foreach (string name in FloorStates)
            if (!states.ContainsKey(name)) problems.Add("falta el estado " + name);

        foreach ((string state, string clip) in ClimbStates)
        {
            if (!states.TryGetValue(state, out AnimatorState animatorState))
            {
                animatorState = climbMachine.AddState(state);
                states.Add(state, animatorState);
            }
            if (clips.TryGetValue(ClipPrefix + clip, out AnimationClip animationClip)) animatorState.motion = animationClip;
            else problems.Add("falta el clip " + clip + " en " + ClimbClipsPath);
            animatorState.speed = 1f;
        }

        foreach (AnimatorState state in states.Values)
        {
            foreach (AnimatorStateTransition transition in state.transitions.ToArray()) state.RemoveTransition(transition);
            state.behaviours = new StateMachineBehaviour[0];
            EditorUtility.SetDirty(state);
        }

        foreach ((string from, string to, float exitTime, float duration) in Transitions)
        {
            if (!states.ContainsKey(from) || !states.ContainsKey(to)) continue;
            AnimatorStateTransition transition = states[from].AddTransition(states[to]);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
        }

        foreach (AnimatorStateMachine machine in machines)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
            {
                bool keep = machine == root && transition.destinationState != null && transition.destinationState.name == "Dead";
                if (!keep) machine.RemoveAnyStateTransition(transition);
            }
            foreach (AnimatorTransition transition in machine.entryTransitions.ToArray()) machine.RemoveEntryTransition(transition);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
                machine.SetStateMachineTransitions(child.stateMachine, new AnimatorTransition[0]);
            EditorUtility.SetDirty(machine);
        }

        if (states.ContainsKey("Idle"))
        {
            root.defaultState = states["Idle"];
            if (floorMachine != root) floorMachine.defaultState = states["Idle"];
        }
        if (climbMachine != root && states.ContainsKey("Climb_Idle")) climbMachine.defaultState = states["Climb_Idle"];

        foreach (AnimatorControllerParameter parameter in controller.parameters.ToArray())
            if (!KeptParameters.Contains(parameter.name)) controller.RemoveParameter(parameter);

        int removed = RemoveOrphans(controller, machines);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        int transitionCount = states.Values.Sum(s => s.transitions.Length);
        string summary = "CatAnimatorSetup: " + states.Count + " estados, " + transitionCount + " transiciones, "
                         + controller.parameters.Length + " parametros, " + removed + " objetos huerfanos eliminados.";
        if (problems.Count > 0) Debug.LogWarning(summary + " Avisos: " + string.Join("; ", problems));
        else Debug.Log(summary);
    }

    static void CollectMachines(AnimatorStateMachine machine, List<AnimatorStateMachine> result)
    {
        result.Add(machine);
        foreach (ChildAnimatorStateMachine child in machine.stateMachines) CollectMachines(child.stateMachine, result);
    }

    static int RemoveOrphans(AnimatorController controller, List<AnimatorStateMachine> machines)
    {
        HashSet<Object> used = new HashSet<Object> { controller };
        foreach (AnimatorStateMachine machine in machines)
        {
            used.Add(machine);
            foreach (Object o in machine.anyStateTransitions) used.Add(o);
            foreach (Object o in machine.entryTransitions) used.Add(o);
            foreach (Object o in machine.behaviours) used.Add(o);
            foreach (ChildAnimatorState child in machine.states)
            {
                used.Add(child.state);
                foreach (Object o in child.state.transitions) used.Add(o);
                foreach (Object o in child.state.behaviours) used.Add(o);
                if (child.state.motion is BlendTree tree) used.Add(tree);
            }
        }

        int removed = 0;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
        {
            if (asset == null || used.Contains(asset)) continue;
            bool animatorObject = asset is AnimatorState || asset is AnimatorStateMachine || asset is AnimatorTransitionBase
                                  || asset is StateMachineBehaviour || asset is BlendTree;
            if (!animatorObject) continue;
            Object.DestroyImmediate(asset, true);
            removed++;
        }
        return removed;
    }

    static void EnsureClipImported(string clip, int firstFrame, int lastFrame, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(ClimbClipsPath) as ModelImporter;
        if (importer == null) return;

        string takeName = ClipPrefix + clip;
        List<ModelImporterClipAnimation> current = importer.clipAnimations.ToList();
        if (current.Count == 0 || current.Any(c => c.name == takeName || c.takeName == takeName)) return;
        if (!importer.importedTakeInfos.Any(t => t.name == takeName)) return;

        ModelImporterClipAnimation template = current[0];
        ModelImporterClipAnimation added = new ModelImporterClipAnimation
        {
            name = takeName,
            takeName = takeName,
            firstFrame = firstFrame,
            lastFrame = lastFrame,
            loopTime = loop,
            wrapMode = template.wrapMode,
            lockRootRotation = template.lockRootRotation,
            lockRootHeightY = template.lockRootHeightY,
            lockRootPositionXZ = template.lockRootPositionXZ,
            keepOriginalOrientation = template.keepOriginalOrientation,
            keepOriginalPositionY = template.keepOriginalPositionY,
            keepOriginalPositionXZ = template.keepOriginalPositionXZ,
            heightFromFeet = template.heightFromFeet,
            maskType = template.maskType,
            maskSource = template.maskSource,
        };
        current.Add(added);
        importer.clipAnimations = current.ToArray();
        importer.SaveAndReimport();
    }
}
