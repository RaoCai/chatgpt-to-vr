using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMenu : MonoBehaviour
{
    public RectTransform menuRectTransform;
    public Vector3 newSize = new Vector3(2f, 2f, 1f);  // Desired scale
    public Vector3 newPosition = new Vector3(0f, 1f, 1.5f);  // Desired position

    public void ResizeMenu()
    {
        if (menuRectTransform == null)
        {
            Debug.LogError("Menu RectTransform is not assigned!");
            return;
        }

        // Log current menu scale and position
        Debug.Log("ResizeMenu method called.");
        Debug.Log("Current Menu Scale: " + menuRectTransform.localScale);
        Debug.Log("Current Menu Position: " + menuRectTransform.position);

        // Set the new scale and position
        menuRectTransform.localScale = new Vector3(0.002f, 0.002f, 0.002f);  // Resetting the scale to default to avoid large jump
        menuRectTransform.localScale = newSize;  // Apply the desired size
        menuRectTransform.position = newPosition;  // Adjust the position

        // Log the new scale and position
        Debug.Log("New Menu Scale: " + menuRectTransform.localScale);
        Debug.Log("New Menu Position: " + menuRectTransform.position);
    }
}