using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MindMapNode : MonoBehaviour
{
    public GameObject nodePrefab;
    public LineRenderer lineRenderer;
    public List<MindMapNode> childNodes = new List<MindMapNode>();

    public void CreateNode(Vector3 position)
    {
        GameObject newNode = Instantiate(nodePrefab, position, Quanternion.identity);
        newNode.transform.SetParent(this.transform); 
        childNodes.Add(newNode.GetComponent<MindMapNode>());
        DrawConnection(newNode.transform.position);
    }

    public void DrawConnection(Vector3 targetPosition)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, targetPosition);
    }
}
