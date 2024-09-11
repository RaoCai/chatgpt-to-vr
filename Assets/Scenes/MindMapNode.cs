using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MindMapNode : MonoBehaviour
{
    public string content;
    public List<MindMapNode> children;
    public Vector3 position;

    public MindMapNode(string content)
    {
        this.content = content;
        this.children = new List<MindMapNode>();
        this.position = Vector3.zero;
    }

    public void AddChild(MindMapNode child)
    {
        children.Add(child);
    }
}