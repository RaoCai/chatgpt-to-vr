using UnityEngine;
using UnityEngine.UI;
using ProceduralModeling;

public class ManualTestRunner : MonoBehaviour
{
    public GameObject branchUIPrefab;
    public Canvas uiCanvas;

    void Start()
    {
        // Create a new GameObject and add the necessary components
        GameObject treeObject = new GameObject("TestTree");
        ProceduralTree proceduralTree = treeObject.AddComponent<ProceduralTree>();
        TreeMetaInteraction treeMetaInteraction = treeObject.AddComponent<TreeMetaInteraction>();

        // Assign the UI Canvas and Branch UI Prefab
        treeMetaInteraction.uiCanvas = uiCanvas;
        treeMetaInteraction.branchUIPrefab = branchUIPrefab;

        // Simulate the Start method
        treeMetaInteraction.Start();

        // Check if branch UIs are created
        if (treeMetaInteraction.branchUIs.Count > 0)
        {
            Debug.Log("Test Passed: Branch UIs are created.");
        }
        else
        {
            Debug.LogError("Test Failed: Branch UIs are not created.");
        }

        // Clean up
        Destroy(treeObject);
    }
}