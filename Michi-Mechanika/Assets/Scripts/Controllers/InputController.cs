using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController instance;
    
    public InputActions inputActions;
    
    private Vector2 pointerStartPos;
    private Vector2 pointerEndPos;
    private bool isDragging = false;
    
    private bool hasPaused = false;

    public float dragThreshold = 10f;
    
    public Vector2 CameraDragDelta => inputActions.Camera.Drag.ReadValue<Vector2>();
    
    private readonly Queue<BufferedAction> inputBuffer = new Queue<BufferedAction>();
    public int BufferCount => inputBuffer.Count;
    
    public bool HasPaused { get {return hasPaused;} set {hasPaused = value; } }
    public bool IsCameraDragging { get; private set; }

    
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

            inputActions.Pointer.PointerClick.started  += OnPointerDown;
            inputActions.Pointer.PointerClick.canceled += OnPointerUp;
            inputActions.UserActions.Pause.started     += OnPauseInputStart;

            inputActions.Camera.Press.started  += ctx => IsCameraDragging = true;
            inputActions.Camera.Press.canceled += ctx => IsCameraDragging = false;
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
        isDragging = true;
    }

    private void OnPointerUp(InputAction.CallbackContext context)
    {
        pointerEndPos = inputActions.Pointer.PointerPosition.ReadValue<Vector2>();

        if (!isDragging) return;

        float dragDistance = Vector2.Distance(pointerStartPos, pointerEndPos);

        if (dragDistance < dragThreshold)
        {
            EnqueueClick(pointerEndPos);
        }
        else
        {
            EnqueueDrag(pointerStartPos,pointerEndPos);
        }

        isDragging = false;
    }
    
    void OnPauseInputStart(InputAction.CallbackContext context)
    {
        hasPaused = !hasPaused; 
    }
    
    private void EnqueueClick(Vector2 screenPos)
    {
        inputBuffer.Enqueue(new BufferedAction
        {
            Type = BufferedActionType.Click,
            ClickScreenPos = screenPos
        });
    }

    private void EnqueueDrag(Vector2 start, Vector2 end)
    {
        inputBuffer.Enqueue(new BufferedAction
        {
            Type = BufferedActionType.DragMove,
            DragStart = start,
            DragEnd   = end
        });
    }

    public bool TryDequeueAction(out BufferedAction action)
    {
        if (inputBuffer.Count > 0)
        {
            action = inputBuffer.Dequeue();
            return true;
        }

        action = null;
        return false;
    }

    public void ClearBuffer()
    {
        inputBuffer.Clear();
    }
    
}
public enum BufferedActionType
{
    Click,
    DragMove
}

public class BufferedAction
{
    public BufferedActionType Type;
    public Vector2 ClickScreenPos;   
    public Vector2 DragStart;        
    public Vector2 DragEnd;
}