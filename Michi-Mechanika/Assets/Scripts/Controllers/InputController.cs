using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController instance;
    
    InputActions inputActions;
    
    private Vector2 pointerStartPos;
    private Vector2 pointerEndPos;
    private bool isDragging = false;
    
    private bool hasClicked = false;
    private bool hasDragged = false;

    public Vector2 ClickPosition { get; private set; }
    public Vector2 DragStart { get; private set; }
    public Vector2 DragEnd { get; private set; }
    public Vector2 DragDirection => (DragEnd - DragStart).normalized;

    public bool HasClicked {get{ return hasClicked; } set {hasClicked = value;}}
    public bool HasDragged {get{ return hasDragged; } set {hasDragged = value;}}

    public float dragThreshold = 10f;
    
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputActions();
            
            inputActions.Pointer.PointerClick.started += OnPointerDown;
            inputActions.Pointer.PointerClick.canceled += OnPointerUp;
        }
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
    
    private void OnPointerDown(InputAction.CallbackContext context)
    {
        pointerStartPos = inputActions.Pointer.PointerPosition.ReadValue<Vector2>();
        DragStart = pointerStartPos;
        isDragging = true;
    }

    private void OnPointerUp(InputAction.CallbackContext context)
    {
        pointerEndPos = inputActions.Pointer.PointerPosition.ReadValue<Vector2>();
        DragEnd = pointerEndPos;

        if (!isDragging) return;

        float dragDistance = Vector2.Distance(pointerStartPos, pointerEndPos);

        if (dragDistance < dragThreshold)
        {
            ClickPosition = pointerEndPos;
            hasClicked = true;
        }
        else
        {
            hasDragged = true;
        }

        isDragging = false;
    }
}
