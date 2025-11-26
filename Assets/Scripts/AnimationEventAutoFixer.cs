using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationEventAutoFixer : EditorWindow
{
    [MenuItem("Tools/Auto-Fix Animation Events")]
    public static void FixAnimationEvents()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        int fixedEvents = 0;
        int totalEvents = 0;
        int affectedClips = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
                continue;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Length == 0)
                continue;

            List<AnimationEvent> validEvents = new List<AnimationEvent>();
            bool removedAny = false;

            foreach (AnimationEvent evt in events)
            {
                totalEvents++;

                // event is invalid if function name is empty or whitespace
                if (string.IsNullOrEmpty(evt.functionName) || evt.functionName.Trim().Length == 0)
                {
                    Debug.LogWarning($"<color=yellow>[Auto Removed Empty Event]</color> Clip: <b>{clip.name}</b> | Path: {path}");
                    removedAny = true;
                    fixedEvents++;
                    continue;
                }

                validEvents.Add(evt);
            }

            if (removedAny)
            {
                AnimationUtility.SetAnimationEvents(clip, validEvents.ToArray());
                EditorUtility.SetDirty(clip);
                affectedClips++;
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"<color=green>=== Auto-Fix Complete ===</color>\n" +
            $"Animation Clips Scanned: {guids.Length}\n" +
            $"Total Events Found: {totalEvents}\n" +
            $"Events Auto-Removed: {fixedEvents}\n" +
            $"Clips Updated: {affectedClips}"
        );
    }
}
