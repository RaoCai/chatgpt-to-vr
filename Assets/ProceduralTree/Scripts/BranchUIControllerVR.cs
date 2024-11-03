using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace ProceduralModeling
{
    public class BranchUIControllerVR : MonoBehaviour
    {
        [SerializeField] private TextMeshPro branchLabel;
        [SerializeField] private Transform deleteButton;
        [SerializeField] private MeshRenderer buttonRenderer;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material hoverMaterial;
        
        private int branchId;
        private System.Action<int> onDeleteCallback;
        private Vector3 branchPosition;

        public void InitializeVR(int id, System.Action<int> deleteCallback, Vector3 position)
        {
            branchId = id;
            onDeleteCallback = deleteCallback;
            branchPosition = position;

            if (branchLabel != null)
            {
                branchLabel.text = $"{id}";
                branchLabel.transform.position = position + Vector3.up * 0.1f;
            }

            if (deleteButton != null)
            {
                deleteButton.position = position + Vector3.right * 0.2f;
                deleteButton.localScale = Vector3.one * 0.1f;
            }

            SetupVRInteractions();
            
            if (buttonRenderer != null && defaultMaterial != null)
                buttonRenderer.material = defaultMaterial;
        }

        private void SetupVRInteractions()
        {
            if (deleteButton == null) return;

            var deleteInteractable = deleteButton.gameObject.AddComponent<XRSimpleInteractable>();
            
            deleteInteractable.hoverEntered.AddListener((args) => {
                if (buttonRenderer != null && hoverMaterial != null)
                    buttonRenderer.material = hoverMaterial;
            });

            deleteInteractable.hoverExited.AddListener((args) => {
                if (buttonRenderer != null && defaultMaterial != null)
                    buttonRenderer.material = defaultMaterial;
            });

            deleteInteractable.selectEntered.AddListener((args) => {
                if (onDeleteCallback != null)
                    onDeleteCallback(branchId);
            });
        }

        private void Update()
        {
            // Make UI elements face the camera
            if (Camera.main != null)
            {
                if (branchLabel != null)
                    branchLabel.transform.LookAt(Camera.main.transform);
                
                if (deleteButton != null)
                    deleteButton.LookAt(Camera.main.transform);
            }
        }
    }
}