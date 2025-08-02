using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionMessage = "Hello from TestInteractable!";
    
    public void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}: {interactionMessage}");
        ChangeColor();
    }

    private void ChangeColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Random.ColorHSV();
    }
}
