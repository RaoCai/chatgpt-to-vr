using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using ProceduralModeling;

public class PTGrowing : MonoBehaviour
{
    [SerializeField] private float growthSpeed = 1f;
    [SerializeField] private float branchDelay = 0.2f;
    [SerializeField] private float maxTreeHeight = 10f;
    [SerializeField] private Texture2D barkTexture;
    private InputAction growBranchesAction;

    private float currentGrowthProgress = 0f;
    private Material growthMaterial;
    private MeshRenderer meshRenderer;
    private ProceduralTree tree;
    private Dictionary<int, float> branchGrowth = new Dictionary<int, float>();
    private bool isTreeInitialized = false;
    private int totalBranchesGrown = 0;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        tree = GetComponent<ProceduralTree>();

        Shader growthShader = Shader.Find("Custom/TreeGrowthShader");
        if (growthShader != null)
        {
            SetupMaterial(growthShader);
        }
        else
        {
            Debug.LogError("Could not find TreeGrowthShader!");
        }

        growBranchesAction = new InputAction(type: InputActionType.PassThrough, binding: "<Keyboard>/anyKey");
        growBranchesAction.performed += context => HandleInput(context);
        growBranchesAction.Enable();
    }

    private void OnDestroy()
    {
        growBranchesAction.Disable();
    }

    private void Start()
    {
        StartCoroutine(InitializeTreeAndGrow());
    }

    private void Update()
    {
        if (currentGrowthProgress < 1f)
        {
            currentGrowthProgress += Time.deltaTime / growthSpeed;
            currentGrowthProgress = Mathf.Clamp01(currentGrowthProgress);
            growthMaterial.SetFloat("_GrowthProgress", currentGrowthProgress);
        }
    }

    private void HandleInput(InputAction.CallbackContext context)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 1; i <= 9; i++)
        {
            var key = keyboard[(Key)((int)Key.Digit1 + (i - 1))];
            if (key != null && key.wasPressedThisFrame)
            {
                Debug.Log($"Key {i} pressed. Growing {i} branches.");
                //tree.GrowBranchesFromInitial(i, branchGrowth);
                GrowNewBranches(i);
                break;
            }
        }
    }
    private void GrowNewBranches(int count)
    {
        int newBranchesCount = tree.GrowBranchesFromInitial(count, branchGrowth);
        totalBranchesGrown += newBranchesCount;
        currentGrowthProgress = 0f;
        growthMaterial.SetFloat("_GrowthProgress", currentGrowthProgress);
        StartCoroutine(GrowBranchesInBatches(newBranchesCount));
    }

    private IEnumerator InitializeTreeAndGrow()
    {
        yield return new WaitUntil(() => tree.branches != null && tree.branches.Count > 0);
        isTreeInitialized = true;

        SetupGrowthMaterial();

        if (growthMaterial != null)
        {
            StartCoroutine(GrowBranchesInBatches(20));
        }
    }

    private void SetupMaterial(Shader shader)
    {
        growthMaterial = new Material(shader);
        growthMaterial.SetFloat("_Glossiness", 0.5f);
        growthMaterial.SetFloat("_Metallic", 0.0f);
        growthMaterial.SetColor("_Color", Color.white);
        meshRenderer.material = growthMaterial;
        if (barkTexture != null)
        {
            growthMaterial.SetTexture("_MainTex", barkTexture);
        }
        else
        {
            Debug.LogWarning("Bark texture is not assigned!");
        }
    }

    private void SetupGrowthMaterial()
    {
        if (!isTreeInitialized)
        {
            Debug.LogError("Tree is not initialized yet!");
            return;
        }

        branchGrowth.Clear();
        foreach (var branch in tree.branches)
        {
            branchGrowth[branch.BranchId] = 0f;
        }

        UpdateBranchDataTexture();
    }

    private void UpdateBranchDataTexture()
    {
        int branchCount = tree.branches.Count;

        if (barkTexture == null || barkTexture.width != branchCount)
        {
            barkTexture = new Texture2D(branchCount, 1, TextureFormat.RGFloat, false);
        }

        Color[] data = new Color[branchCount];

        for (int i = 0; i < branchCount; i++)
        {
            TreeBranch branch = tree.branches[i];
            float growth = branchGrowth.ContainsKey(branch.BranchId) ? branchGrowth[branch.BranchId] : 0f;
            float normalizedHeight = branch.EndPoint.y / maxTreeHeight;

            data[i] = new Color(growth, normalizedHeight, 0, 0);
        }

        barkTexture.SetPixels(data);
        barkTexture.Apply();
        growthMaterial.SetTexture("_BranchData", barkTexture);
    }

    private IEnumerator GrowBranchesInBatches(int batchSize)
    {
        int branchCount = tree.branches.Count;
        for (int i = 0; i < branchCount; i += batchSize)
        {
            for (int j = i; j < i + batchSize && j < branchCount; j++)
            {
                TreeBranch branch = tree.branches[j];
                StartCoroutine(GrowBranch(branch.BranchId));
            }
            yield return new WaitForSeconds(branchDelay);
        }
    }

    private IEnumerator GrowBranch(int branchId)
    {
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * growthSpeed;
            if (branchGrowth.ContainsKey(branchId))
            {
                branchGrowth[branchId] = Mathf.Clamp01(progress);
            }
            else
            {
                Debug.LogWarning($"Branch ID {branchId} not found in branchGrowth dictionary.");
            }
            UpdateBranchDataTexture();
            yield return null;
        }
    }
}