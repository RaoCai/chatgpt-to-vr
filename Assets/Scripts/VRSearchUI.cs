using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRSearchUI : MonoBehaviour
{

    public TMP_InputField searchText;
    public TreeVisualiser treeVisualiser;
    private TreeNode currentNode;
    public XRRayInteractor rayInteractor;
    private XRGrabInteractable selectedNode;

    // Start is called before the first frame update
    void Start()
    {
        currentNode = null;
    }

    public void OnSearchButtonPressed()
    {
        string searchQuery = searchText.text;
        if (!string.IsNullOrEmpty(searchQuery))
        {
            AddSearchResultToTree(searchQuery);
        }
    }

    private void AddSearchResultToTree(string result)
    {
        if (currentNode == null)
        {
            currentNode = new TreeNode(result);
        }

        int currentDepth = GetNodeDepth(currentNode);
        treeVisualiser.AddBranch(currentNode, result, currentDepth);
    }

    private int GetNodeDepth(TreeNode node)
    {
        int depth = 0;
        while (node != null)
        {
            depth++;
            node = node.parent;
        }
        return depth;
    }

    public void OnSelectNodeInVR()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject selectedObject = hit.collider.gameObject;

            if (selectedObject.CompareTag("TreeNode"))
            {
                TreeNode selectedTreeNode = FindTreeNodeFromObject(selectedObject);
                if (selectedTreeNode != null)
                {
                    currentNode = selectedTreeNode;
                }
            }
        }
    }

    private TreeNode FindTreeNodeFromObject(GameObject selectedObject)
    {
        foreach (var pair in treeVisualiser.nodes)
        {
            if (pair.Value == selectedObject)
            {
                return pair.Key;
            }
        }
        return null;
    }

    public void OnDeselectedModeInVR()
    {
        currentNode = null;
    }
}
