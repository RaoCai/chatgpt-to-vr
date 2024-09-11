using UnityEngine;
using System.Collections.Generic;

public class MindMapGenerator : MonoBehaviour
{
    public float nodeSpacing = 2f;
    public float levelSpacing = 3f;
    public Vector3 rootPosition = new Vector3(0, -5, 0);
    public float nodeScale = 0.5f;
    public float textScale = 0.1f;
    public int fontSize = 14;

    [Header("Node Rotation")]
    public float nodeRotationX = 0f;
    public float nodeRotationY = 0f;
    public float nodeRotationZ = 0f;

    [Header("Text Rotation")]
    public float textRotationX = 0f;
    public float textRotationY = 0f;
    public float textRotationZ = 0f;

    private MindMapNode root;

    void Start()
    {
        CreateMindMap();
        VisualizeMindMap();
    }

    void CreateMindMap()
    {
        root = new MindMapNode("Root");

        MindMapNode child1 = new MindMapNode("Child 1");
        MindMapNode child2 = new MindMapNode("Child 2");
        MindMapNode child3 = new MindMapNode("Child 3");

        root.AddChild(child1);
        root.AddChild(child2);
        root.AddChild(child3);

        child1.AddChild(new MindMapNode("Grandchild 1.1"));
        child1.AddChild(new MindMapNode("Grandchild 1.2"));

        child2.AddChild(new MindMapNode("Grandchild 2.1"));

        child3.AddChild(new MindMapNode("Grandchild 3.1"));
        child3.AddChild(new MindMapNode("Grandchild 3.2"));
        child3.AddChild(new MindMapNode("Grandchild 3.3"));
    }

    void VisualizeMindMap()
    {
        PositionNode(root, rootPosition, 0);
        CreateNodeObjects(root);
    }

    void PositionNode(MindMapNode node, Vector3 position, int level)
    {
        node.position = position;

        float totalWidth = (node.children.Count - 1) * nodeSpacing;
        float startX = position.x - totalWidth / 2;

        for (int i = 0; i < node.children.Count; i++)
        {
            float xOffset = startX + i * nodeSpacing;
            Vector3 childPosition = new Vector3(xOffset, position.y + levelSpacing, 0);
            PositionNode(node.children[i], childPosition, level + 1);
        }
    }

    void CreateNodeObjects(MindMapNode node)
    {
        GameObject nodeObject = CreateNodeObject(node.content, node.position);

        foreach (MindMapNode child in node.children)
        {
            CreateNodeObjects(child);
            DrawLine(node.position, child.position);
        }
    }

    GameObject CreateNodeObject(string content, Vector3 position)
    {
        GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nodeObject.transform.position = position;
        nodeObject.transform.localScale = Vector3.one * nodeScale;
        nodeObject.transform.rotation = Quaternion.Euler(nodeRotationX, nodeRotationY, nodeRotationZ);

        GameObject textObject = new GameObject("NodeText");
        textObject.transform.SetParent(nodeObject.transform);
        textObject.transform.localPosition = new Vector3(0, 0, -nodeScale);
        textObject.transform.localScale = Vector3.one * textScale;
        textObject.transform.localRotation = Quaternion.Euler(textRotationX, textRotationY, textRotationZ);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = content;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        return nodeObject;
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("Line");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
