using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TreeVisualiser : MonoBehaviour
{

    public GameObject branchPrefab;
    public GameObject nodePrefab;
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
    }

    private void CreateNodeVisual(TreeNode node, Vector3 position)
    {
        GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity, this.transform);
        nodes[node] = nodeObj;

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

        CreateNodeVisual(newNode, newPosition);
        StartCoroutine(GrowCurvedBranch(parentPosition, newPosition, currentDepth));
    }

    private Vector3 RandomDirectionWithAngleLimit()
    {
        float angle = UnityEngine.Random.Range(-branchSpreadAngle, branchSpreadAngle);
        float y = Mathf.Cos(angle * Mathf.Deg2Rad);
        float x = Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector3(x, y, 0).normalized;
    }

    private IEnumerator GrowCurvedBranch(Vector3 start, Vector3 end, int currentDepth)
    {
        GameObject branch = Instantiate(branchPrefab, this.transform);
        LineRenderer lineRenderer = branch.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 3;

        float elapsedTime = 0f;
        Vector3 midpoint = (start + end) / 2f + Vector3.up * branchCurvature;

        while (elapsedTime < growDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / growDuration);

            Vector3 curvePosition = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * midpoint + Mathf.Pow(t, 2) * end;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, curvePosition);
            lineRenderer.SetPosition(2, end);

            yield return null;
        }
    }

    private void AddFurtherBranches(TreeNode node, Vector3 position, int currentDepth)
    {
        int numBranches = UnityEngine.Random.Range(1, 3);

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
}
