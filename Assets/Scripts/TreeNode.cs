using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode 
{
    public string data;
    public List<TreeNode> children;
    public TreeNode parent;

    public TreeNode(string data)
    {
        this.data = data;
        this.children = new List<TreeNode>();
        this.parent = null;
    }

    public void AddChild(string childData)
    {
        TreeNode childNode = new TreeNode(data); 
        children.Add(childNode);
        childNode.parent = this;
    }
}
