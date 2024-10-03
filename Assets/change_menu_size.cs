using UnityEngine;
using UnityEngine.UI; 

public class ChangeCanvasSize : MonoBehaviour
{
    public Canvas myCanvas;  
    public Button myButton;  

    // to define new size
    public Vector2 newSize;

    void Start()
    {
        // event listener
        myButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        RectTransform canvasRectTransform = myCanvas.GetComponent<RectTransform>();

        canvasRectTransform.sizeDelta = newSize;

    }
}
