using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class TreeMetaInteraction : MonoBehaviour
{
    [SerializeField] private ProceduralModeling.ProceduralTree proceduralTree;
    [SerializeField] private GameObject branchUIPrefab;
    [SerializeField] private Transform uiContainer;
    [SerializeField] private float delayBeforeUI = 2f; // Delay in seconds
    [SerializeField] private Button exportPDFButton;

    private Dictionary<int, GameObject> branchUIs = new Dictionary<int, GameObject>();

    private void Start()
    {
        if (proceduralTree == null)
        {
            proceduralTree = GetComponent<ProceduralModeling.ProceduralTree>();
        }

        SetupPDFExportButton();

        // Start coroutine to delay UI creation
        StartCoroutine(DelayedUICreation());
    }

    private IEnumerator DelayedUICreation()
    {
        // Wait for specified delay
        yield return new WaitForSeconds(delayBeforeUI);

        // Subscribe to events after delay
        proceduralTree.OnBranchCreated += HandleNewBranch;
        
        // Create initial UIs
        CreateBranchUIs();
    }

    private void SetupPDFExportButton()
    {
        if (exportPDFButton != null)
        {
            exportPDFButton.onClick.AddListener(ExportToPDF);
        }
        else
        {
            Debug.LogWarning("Export PDF Button not assigned in TreeMetaInteraction!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when the script is destroyed
        if (proceduralTree != null)
        {
            proceduralTree.OnBranchCreated -= HandleNewBranch;
        }

        // Clean up PDF button listener
        if (exportPDFButton != null)
        {
            exportPDFButton.onClick.RemoveListener(ExportToPDF);
        }
    }

    private void ExportToPDF()
    {
        if (proceduralTree != null)
        {
            try
            {
                proceduralTree.ExportToPDF();
                Debug.Log("PDF Export completed successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error exporting PDF: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("ProceduralTree reference is missing!");
        }
    }

    private void HandleNewBranch(ProceduralModeling.TreeBranch branch)
    {
        if (branch.BranchId != 1) // Skip trunk
        {
            StartCoroutine(DelayedBranchUICreation(branch.BranchId));
        }
    }

    private IEnumerator DelayedBranchUICreation(int branchId)
    {
        yield return new WaitForSeconds(0.5f); // Small delay for new branches
        CreateBranchUI(branchId);
    }

    private void CreateBranchUIs()
    {
        Debug.Log($"Creating UIs for {proceduralTree.branches.Count} branches");
        foreach (var branch in proceduralTree.branches)
        {
            if (branch.BranchId != 1) // Skip trunk
            {
                CreateBranchUI(branch.BranchId);
            }
        }
    }

    private void CreateBranchUI(int branchId)
    {
        if (branchUIPrefab == null)
        {
            Debug.LogError("Branch UI Prefab is not assigned!");
            return;
        }

        if (uiContainer == null)
        {
            Debug.LogError("UI Container is not assigned!");
            return;
        }

        // Check if UI already exists
        if (branchUIs.ContainsKey(branchId))
        {
            return;
        }

        // Create UI
        GameObject branchUI = Instantiate(branchUIPrefab, uiContainer);
        branchUI.name = $"BranchUI_{branchId}";

        // Set up the button
        BranchUIController controller = branchUI.GetComponent<BranchUIController>();
        if (controller != null)
        {
            controller.Initialize(branchId, DeleteBranch);
        }
        else
        {
            Debug.LogError($"BranchUIController component missing on prefab!");
        }

        // Store reference
        branchUIs[branchId] = branchUI;

        // Set initial position
        UpdateBranchUIPosition(branchId);

        Debug.Log($"Created UI for branch {branchId}");
    }

    private void UpdateBranchUIPosition(int branchId)
    {
        if (!branchUIs.TryGetValue(branchId, out GameObject branchUI))
            return;

        var branch = proceduralTree.branches.Find(b => b.BranchId == branchId);
        if (branch == null)
            return;

        // Calculate middle point of the branch
        Vector3 middlePoint = Vector3.Lerp(branch.StartPoint, branch.EndPoint, 0.5f);
        
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(middlePoint);
        
        // Only show UI if branch is in front of camera
        if (screenPos.z > 0)
        {
            // Convert screen position to local canvas position
            RectTransform canvasRect = uiContainer.GetComponent<RectTransform>();
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                new Vector2(screenPos.x, screenPos.y),
                null, // Use null for overlay canvas
                out localPoint
            );

            // Set the local position of the UI element
            RectTransform rectTransform = branchUI.GetComponent<RectTransform>();
            rectTransform.localPosition = localPoint;
            
            branchUI.SetActive(true);
        }
        else
        {
            branchUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Update the position of each branch UI
        foreach (var kvp in branchUIs)
        {
            UpdateBranchUIPosition(kvp.Key);
        }
    }

    void DeleteBranch(int branchId) 
    {
        // Check if the branch is the trunk
        if (branchId == 1)
        {
            Debug.Log("Cannot delete trunk");
            return;
        }

        Debug.Log($"Attempting to delete branch {branchId}");

        // Delete the branch from the tree
        proceduralTree.DeleteBranch(branchId);

        // Clean up UIs
        foreach (var kvp in branchUIs.ToList())
        {
            if (!proceduralTree.BranchPositions.ContainsKey(kvp.Key))
            {
                Destroy(kvp.Value);
                branchUIs.Remove(kvp.Key);
            }
        }
    }
}