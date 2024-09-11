using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NodeInteractionFeedback : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Renderer nodeRenderer;
    private Color originalColor;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        nodeRenderer = GetComponent<Renderer>();
        originalColor = nodeRenderer.material.color;

        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        nodeRenderer.material.color = Color.red;
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        nodeRenderer.material.color = originalColor;
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        nodeRenderer.material.color = Color.yellow;
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        nodeRenderer.material.color = originalColor;
    }
}