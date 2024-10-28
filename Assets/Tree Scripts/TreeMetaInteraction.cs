using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProceduralModeling {

    [RequireComponent(typeof(ProceduralTree))]
    public class TreeMetaInteraction : MonoBehaviour {
        public GameObject branchUIPrefab;
        public Canvas uiCanvas;

        private ProceduralTree proceduralTree;
        internal Dictionary<int, GameObject> branchUIs = new Dictionary<int, GameObject>();

        public void Start() 
        {
            proceduralTree = GetComponent<ProceduralTree>();
            if (proceduralTree == null)
            {
                Debug.LogError("ProceduralTree component is missing.");
                return;
            }

            if (branchUIPrefab == null || uiCanvas == null)
            {
                Debug.LogError("Branch UI Prefab or UI Canvas is not assigned.");
                return;
            }
            CreateBranchUIs();
        }

        void CreateBranchUIs() 
        {
            if (proceduralTree.BranchPositions == null)
            {
                Debug.LogError("BranchPositions is not initialized.");
                return;
            }

            foreach (var branchPosition in proceduralTree.BranchPositions) {
                CreateBranchUI(branchPosition.Key, branchPosition.Value);
            }
        }

        void CreateBranchUI(int branchId, Vector3 position) 
        {
            GameObject branchUI = Instantiate(branchUIPrefab, uiCanvas.transform);
            RectTransform rectTransform = branchUI.GetComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.anchoredPosition = Camera.main.WorldToScreenPoint(position);

            BranchUIController controller = branchUI.GetComponent<BranchUIController>();
            controller.Initialize(branchId, DeleteBranch);

            branchUIs[branchId] = branchUI;
        }

        void DeleteBranch(int branchId) 
        {
            Debug.Log($"Attempting to delete branch {branchId}");

            if (branchUIs.TryGetValue(branchId, out GameObject branchUI)) {
                Debug.Log($"Branch UI found for branch {branchId}, destroying UI.");
                Destroy(branchUI);
                branchUIs.Remove(branchId);
            }
            else
            {
                Debug.LogWarning($"No UI found for branch {branchId}. It may have already been deleted.");
            }

            if (proceduralTree.BranchPositions.ContainsKey(branchId))
            {
                Debug.Log($"Branch {branchId} found in BranchPositions, proceeding with deletion.");
                proceduralTree.DeleteBranch(branchId);
            }
            else
            {
                Debug.LogWarning($"Branch {branchId} not found in BranchPositions. It may have already been deleted.");
            }

            Debug.Log("Clearing all branch UIs and rebuilding the tree.");
            foreach (var ui in branchUIs.Values) {
                Destroy(ui);
            }

            proceduralTree.Rebuild();
            CreateBranchUIs();
        }

        void Update() 
        {
            foreach (var branchUI in branchUIs) {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(proceduralTree.BranchPositions[branchUI.Key]);
                branchUI.Value.GetComponent<RectTransform>().anchoredPosition = screenPos;
            }
        }
    }
}