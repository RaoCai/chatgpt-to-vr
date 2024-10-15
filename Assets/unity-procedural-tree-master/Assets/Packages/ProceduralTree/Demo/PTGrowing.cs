using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralModeling;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using static ProceduralModeling.ProceduralTree;

namespace ProceduralModeling
{

	[RequireComponent(typeof(MeshRenderer), typeof(ProceduralTree))]
	public class PTGrowing : MonoBehaviour
	{

        private TreeBranch currentGrowingBranch;
		public ProceduralTree tree;
		public float growDuration = 5.0f;
		private Coroutine growCoroutine;
		private int targetBranches = 0;
        private int currentGrowthStage;
        private int maxGrowthStages = 5;
        public Button searchButton;
        public TMP_InputField searchInputField;
        private bool isGrowing = false;

        void Start()
        {
            if (tree == null)
            {
                tree = GetComponent<ProceduralTree>();
                if (tree == null)
                {
                    Debug.LogError("ProceduralTree component not found!");
                }
            }

            if(searchButton != null)
            {
                searchButton.onClick.AddListener(StartSearch);
                Debug.Log("Search button listener set up.");
            }
        }

		void Update()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard != null)
			{
                for (int i = 1; i <= 5; i++)
                {
                    if (keyboard[Key.Digit1 + i - 1].wasPressedThisFrame)
                    {
                        Debug.Log($"Key {i} pressed");
                        GrowSingleBranch();
                        break;
                    }
                }

                if (keyboard[Key.R].wasPressedThisFrame)
                {
                    Debug.Log("R key pressed");
                    ResetTree();
                }

                if (keyboard[Key.Enter].wasPressedThisFrame)
                {
                    Debug.Log("Enter key pressed");
                    StartSearch();
                }
            }
		}

        void ResetTree()
        {
            if (growCoroutine != null)
            {
                StopCoroutine(growCoroutine);
            }
            tree.ResetTree();
            currentGrowthStage = 0;
            Debug.Log("Tree reset to initial state.");
        }

		async void StartSearch()
        {
            if (isGrowing)
            {
                Debug.Log("Already growing a branch. Ignoring this request.");
                return;
            }

            isGrowing = true;
            string searchQuery = searchInputField != null ? searchInputField.text : "default query";
            Debug.Log($"Starting search with query: {searchQuery}");

            if (tree == null)
            {
                Debug.LogError("ProceduralTree is null!");
                isGrowing = false;
                return;
            }

            Debug.Log("Calling GrowBranchFromSearch");
            currentGrowingBranch = await tree.GrowBranchFromSearch(searchQuery);
            Debug.Log("GrowBranchFromSearch completed");

            if (currentGrowingBranch != null)
            {
                if (growCoroutine != null)
                {
                    StopCoroutine(growCoroutine);
                }
                growCoroutine = StartCoroutine(GrowBranchOverTime());
            }
            else
            {
                Debug.LogWarning("Failed to grow new branch");
                isGrowing = false;
            }
        }

        IEnumerator GrowBranchOverTime()
        {
            if (tree == null || currentGrowingBranch == null)
            {
                Debug.LogError("ProceduralTree or currentGrowingBranch is null!");
                isGrowing = false;
                yield break;
            }

            Debug.Log("Starting GrowBranchOverTime coroutine");
            float elapsedTime = 0f;
            while (elapsedTime < growDuration)
            {
                elapsedTime += Time.deltaTime;
                float growthProgress = Mathf.Clamp01(elapsedTime / growDuration);
                Debug.Log($"Growing branch: {growthProgress * 100}%");
                currentGrowingBranch.UpdateGrowth(growthProgress);
                tree.UpdateMesh();
                yield return null;
            }
            Debug.Log("Branch fully grown");
            tree.UpdateMesh();
            currentGrowingBranch = null;
            isGrowing = false;
        }

		void GrowSingleBranch()
        {
            if (growCoroutine != null)
            {
                StopCoroutine(growCoroutine);
            }
            growCoroutine = StartCoroutine(GrowSingleBranchOverTime());
        }

        IEnumerator GrowSingleBranchOverTime()
        {
            if (tree == null)
            {
                Debug.LogError("ProceduralTree is null!");
                yield break;
            }

            Debug.Log("Growing a single branch");
            tree.AddNewBranch();
            float elapsedTime = 0f;
            while (elapsedTime < growDuration)
            {
                elapsedTime += Time.deltaTime;
                float growthProgress = Mathf.Clamp01(elapsedTime / growDuration);
                tree.UpdateMesh();
                //tree.GrowBranch(growthProgress);
                yield return null;
            }
            Debug.Log("Branch fully grown");
        }
	}
}

