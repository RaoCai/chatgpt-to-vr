using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeVisualiserTest : MonoBehaviour
{
    public Material branchMaterial;  // Material with a growth shader
    public float growDuration = 2.0f;
    public int maxBranchDepth = 5;
    public float minBranchLength = 1.0f;
    public float maxBranchLength = 5.0f;
    public float branchSpreadAngle = 30.0f;

    private TreeNode rootNode;
    private Mesh treeMesh;
    private List<Vector3> vertices;
    private List<int> triangles;

    void Start()
    {
        rootNode = new TreeNode("Root Node");
        vertices = new List<Vector3>();
        triangles = new List<int>();
        CreateInitialMesh();
        StartCoroutine(GrowTreeAutomatically(rootNode, 0));
    }

    private void CreateInitialMesh()
    {
        // Initialize a new mesh
        treeMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = treeMesh;

        // Create initial vertex for the root
        vertices.Add(Vector3.zero); // Root vertex
        UpdateMesh();
    }

    public void AddBranch(TreeNode parentNode, string newNodeData, int currentDepth)
    {
        if (currentDepth >= maxBranchDepth) return;

        parentNode.AddChild(newNodeData); // Add a new child node
        TreeNode newNode = parentNode.children[parentNode.children.Count - 1];

        Vector3 direction = RandomDirectionWithAngleLimit();
        float branchLength = GetBranchLength(currentDepth);
        Vector3 newPosition = vertices[vertices.Count - 1] + direction * branchLength; // Use the last vertex position

        // Add new vertex and triangle
        vertices.Add(newPosition);
        triangles.Add(vertices.Count - 2); // Connect to previous vertex
        triangles.Add(0); // Connect to root (0 index)
        triangles.Add(vertices.Count - 1); // Connect to new vertex

        // Update the mesh with the new vertex and triangle
        UpdateMesh();
    }

    private Vector3 RandomDirectionWithAngleLimit()
    {
        float angle = Random.Range(-branchSpreadAngle, branchSpreadAngle);
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0).normalized;
    }

    private IEnumerator GrowTreeAutomatically(TreeNode currentNode, int currentDepth)
    {
        while (currentDepth < maxBranchDepth)
        {
            AddBranch(currentNode, "Branch Depth " + currentDepth, currentDepth);
            yield return StartCoroutine(IGrowing(growDuration)); // Yielding for growth effect
            TreeNode newNode = currentNode.children[currentNode.children.Count - 1];
            currentNode = newNode;
            currentDepth++;
        }
    }

    // Growing Coroutine
    private IEnumerator IGrowing(float duration)
    {
        var time = 0f;
        Material growingMaterial = branchMaterial; // Use the specified material for growing effect
        const string kGrowingKey = "_Growth"; // Adjust this key based on your shader property for growth effect

        // Set initial growth value to 0
        growingMaterial.SetFloat(kGrowingKey, 0f);

        while (time < duration)
        {
            // Update the growth value based on elapsed time
            growingMaterial.SetFloat(kGrowingKey, time / duration);

            // Increment time
            time += Time.deltaTime;

            // Yield until the next frame
            yield return null;
        }

        // Ensure growth is set to full at the end of the duration
        growingMaterial.SetFloat(kGrowingKey, 1f);
    }

    private void UpdateMesh()
    {
        treeMesh.Clear();
        treeMesh.vertices = vertices.ToArray();
        treeMesh.triangles = triangles.ToArray();
        treeMesh.RecalculateNormals();
        treeMesh.RecalculateBounds();

        // Assign the material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && branchMaterial != null)
        {
            renderer.material = branchMaterial;
        }
    }

    private float GetBranchLength(int currentDepth)
    {
        float depthFactor = 1.0f - (float)currentDepth / maxBranchDepth;
        return Mathf.Lerp(minBranchLength, maxBranchLength, depthFactor);
    }
}
