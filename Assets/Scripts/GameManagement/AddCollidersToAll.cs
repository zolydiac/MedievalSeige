using UnityEngine;

public class AddCollidersToAll : MonoBehaviour
{
    // Run this once by clicking the button in Inspector
    [ContextMenu("Add Mesh Colliders to All GameObjects")]
    void AddMeshCollidersToAll()
    {
        // Find all GameObjects with MeshRenderer (visible objects)
        // Using new Unity 2022+ method - faster and no warnings!
        MeshRenderer[] allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        int count = 0;

        foreach (MeshRenderer renderer in allRenderers)
        {
            GameObject obj = renderer.gameObject;

            // Skip if already has a collider
            if (obj.GetComponent<Collider>() != null)
            {
                Debug.Log(obj.name + " already has collider - skipping");
                continue;
            }

            // Skip player and camera
            if (obj.CompareTag("Player") || obj.CompareTag("MainCamera"))
            {
                Debug.Log(obj.name + " is Player/Camera - skipping");
                continue;
            }

            // Add Mesh Collider
            MeshCollider collider = obj.AddComponent<MeshCollider>();
            collider.convex = true; // Make it work with physics

            count++;
            Debug.Log("Added collider to: " + obj.name);
        }

        Debug.Log("✅ DONE! Added " + count + " colliders");
    }

    // BONUS: Remove all colliders if you need to start over
    [ContextMenu("Remove All Mesh Colliders")]
    void RemoveAllMeshColliders()
    {
        MeshCollider[] allColliders = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);

        int count = 0;
        foreach (MeshCollider collider in allColliders)
        {
            DestroyImmediate(collider);
            count++;
        }

        Debug.Log("✅ Removed " + count + " Mesh Colliders");
    }
}