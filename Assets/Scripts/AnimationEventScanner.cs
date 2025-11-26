using UnityEngine;
using UnityEditor;

public class AnimationEventScanner : EditorWindow
{
    [MenuItem("Tools/Scan Animation Events")]
    public static void ScanEvents()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        int totalClips = 0;
        int issuesFound = 0;

        Debug.Log("<color=cyan>=== Animation Event Scanner Started ===</color>");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null) continue;

            totalClips++;
            var events = AnimationUtility.GetAnimationEvents(clip);

            foreach (var evt in events)
            {
                // Missing function name
                if (string.IsNullOrEmpty(evt.functionName))
                {
                    Debug.LogWarning(
                        $"<color=yellow>[Missing Function]</color> " +
                        $"Clip: <b>{clip.name}</b> | Path: {path}"
                    );
                    issuesFound++;
                }
                else
                {
                    // Check if the function name exists in ANY MonoBehaviour
                    bool foundMethod = false;

                    var allBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();

                    foreach (var mb in allBehaviours)
                    {
                        if (mb == null) continue;
                        var method = mb.GetType().GetMethod(evt.functionName);
                        if (method != null)
                        {
                            foundMethod = true;
                            break;
                        }
                    }

                    if (!foundMethod)
                    {
                        Debug.LogWarning(
                            $"<color=orange>[Missing Method]</color> " +
                            $"Clip: <b>{clip.name}</b> | Event: {evt.functionName} | Path: {path}"
                        );
                        issuesFound++;
                    }
                }
            }
        }

        Debug.Log(
            $"<color=green>=== Scan Complete ===</color>\n" +
            $"Clips scanned: {totalClips}\n" +
            $"Issues found: {issuesFound}"
        );
    }
}
