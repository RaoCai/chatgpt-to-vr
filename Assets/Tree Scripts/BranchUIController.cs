using UnityEngine;
using UnityEngine.UI;

public class BranchUIController : MonoBehaviour
{
    public int branchId;
    private Button deleteButton;

    void Awake()
    {
        deleteButton = GetComponent<Button>();
    }

    public void Initialize(int id, System.Action<int> onDeleteCallback)
    {
        branchId = id;
        deleteButton.onClick.AddListener(() => onDeleteCallback(branchId));
    }
}