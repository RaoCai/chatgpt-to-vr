using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeVisualiser : MonoBehaviour
{
    public GameObject branchPrefab;
    public GameObject nodePrefab;

    public Material branchMaterial;
    public Material nodeMaterial;

    public float growDuration = 2.0f;
    public int maxBranchDepth = 5;
    public float minBranchLength = 1.0f;
    public float maxBranchLength = 5.0f;
    public float branchSpreadAngle = 30.0f;
    public float branchCurvature = 0.5f;

    public TreeNode rootNode;
    public Dictionary<TreeNode, GameObject> nodes = new Dictionary<TreeNode, GameObject>();

    void Start()
    {
        rootNode = new TreeNode("Root Node");
        CreateNodeVisual(rootNode, Vector3.zero);
        StartCoroutine(GrowTreeAutomatically(rootNode, 0));
    }

    private void CreateNodeVisual(TreeNode node, Vector3 position)
    {
        GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity, this.transform);
        nodes[node] = nodeObj;

        MeshRenderer renderer = nodeObj.GetComponent<MeshRenderer>();
        if (renderer != null && nodeMaterial != null)
        {
            renderer.material = nodeMaterial; // Changed to nodeMaterial
        }

        SphereCollider collider = nodeObj.AddComponent<SphereCollider>();
        collider.radius = 0.1f;
    }

    private void OnNodeSelected(TreeNode selectedNode)
    {
        Debug.Log($"Node selected: {selectedNode.data}");
    }

    public void AddBranch(TreeNode parentNode, string newNodeData, int currentDepth)
    {
        if (currentDepth >= maxBranchDepth) return;

        parentNode.AddChild(newNodeData);
        TreeNode newNode = parentNode.children[parentNode.children.Count - 1];

        GameObject parentObject = nodes[parentNode];
        Vector3 parentPosition = parentObject.transform.position;

        Vector3 direction = RandomDirectionWithAngleLimit();
        float branchLength = GetBranchLength(currentDepth);
        Vector3 newPosition = parentPosition + direction * branchLength;

        StartCoroutine(GrowBranchWithMesh(parentPosition, newPosition, currentDepth, newNode));
    }

    private Vector3 RandomDirectionWithAngleLimit()
    {
        float angle = Random.Range(-branchSpreadAngle, branchSpreadAngle);
        float y = Mathf.Cos(angle * Mathf.Deg2Rad);
        float x = Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector3(x, y, 0).normalized;
    }

    private IEnumerator GrowBranchWithMesh(Vector3 start, Vector3 end, int currentDepth, TreeNode newNode)
    {
        GameObject branch = Instantiate(branchPrefab, start, Quaternion.identity, this.transform);

        MeshRenderer renderer = branch.GetComponent<MeshRenderer>();
        if (renderer != null && branchMaterial != null)
        {
            renderer.material = branchMaterial;
        }

        branch.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        branch.transform.LookAt(end);

        float elapsedTime = 0f;
        while (elapsedTime < growDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / growDuration);

            branch.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(0.1f, Vector3.Distance(start, end) / 2.0f, 0.1f), t);

            yield return null;
        }

        branch.transform.position = Vector3.Lerp(start, end, 0.5f);
        branch.transform.localScale = new Vector3(0.1f, Vector3.Distance(start, end) / 2.0f, 0.1f);
        nodes[newNode] = branch; // Save the branch as the new node
    }

    private void AddFurtherBranches(TreeNode node, Vector3 position, int currentDepth)
    {
        int numBranches = Random.Range(1, 3);
        for (int i = 0; i < numBranches; i++)
        {
            AddBranch(node, "Sub-branch " + i, currentDepth);
        }
    }

    private float GetBranchLength(int currentDepth)
    {
        float depthFactor = 1.0f - (float)currentDepth / maxBranchDepth;
        return Mathf.Lerp(minBranchLength, maxBranchLength, depthFactor);
    }

    private IEnumerator GrowTreeAutomatically(TreeNode currentNode, int currentDepth)
    {
        while (currentDepth < maxBranchDepth)
        {
            AddBranch(currentNode, "Branch Depth " + currentDepth, currentDepth);
            yield return StartCoroutine(IGrowing(growDuration));
            TreeNode newNode = currentNode.children[currentNode.children.Count - 1];
            currentNode = newNode;
            currentDepth++;
        }
    }

    // Growing Coroutine
    private IEnumerator IGrowing(float duration)
    {
        var time = 0f;
        Material growingMaterial = branchMaterial;
        const string kGrowingKey = "_Growth";

        growingMaterial.SetFloat(kGrowingKey, 0f);

        while (time < duration)
        {
            growingMaterial.SetFloat(kGrowingKey, time / duration);

            time += Time.deltaTime;

            yield return null;
        }

        growingMaterial.SetFloat(kGrowingKey, 1f);
    }
}
