using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceCubeOnFilteredPlane : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private float cubeHalfHeight = 0.1f;
    [SerializeField] private float groundTolerance = 0.12f;
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float editorRotationMultiplier = 6f;
    [SerializeField] private float editorMoveSpeed = 2.5f;
    [SerializeField] private float editorRotateSpeed = 120f;
    [SerializeField] private bool relaxGroundFilterInEditor = true;
    [SerializeField] private bool enableDebugLogs = true;

    private ARRaycastManager raycastManager;
    private GameObject spawnedCube;
    private bool isSelected;
    private float lastFailureLogTime = -10f;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Caches required scene references before the first frame so placement can work immediately.
    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileInput();
#endif
    }

    // Handles mouse and keyboard controls used while testing placement and manipulation in the Unity Editor.
    private void HandleEditorInput()
    {
        if (!TryGetEditorPointer(out Vector2 pointerPosition, out Vector2 deltaPosition, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame))
            return;

        if (spawnedCube == null)
        {
            if (pressedThisFrame)
                TryPlaceCube(pointerPosition);

            return;
        }

        if (pressedThisFrame)
            isSelected = IsTouchOnThisObject(pointerPosition);

        if (isSelected && isPressed)
        {
            MoveOnDetectedPlane(pointerPosition);

            float rotationAmount = deltaPosition.x * rotationSpeed * editorRotationMultiplier;
            spawnedCube.transform.Rotate(0f, -rotationAmount, 0f, Space.World);
        }

        if (releasedThisFrame)
            isSelected = false;

        HandleEditorKeyboardControls();
    }

    // Handles runtime touch input on a real mobile device and routes it to placement or manipulation.
    private void HandleMobileInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            TouchControl primaryTouch = Touchscreen.current.primaryTouch;
            if (primaryTouch == null || !primaryTouch.press.isPressed)
                return;

            Vector2 touchPosition = primaryTouch.position.ReadValue();

            if (spawnedCube == null)
            {
                if (primaryTouch.press.wasPressedThisFrame)
                    TryPlaceCube(touchPosition);

                return;
            }

            HandleSimulatedTouch(
                touchPosition,
                primaryTouch.delta.ReadValue(),
                primaryTouch.press.wasPressedThisFrame,
                primaryTouch.press.wasReleasedThisFrame,
                primaryTouch.press.isPressed);
            return;
        }
#endif

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        // The first valid tap places the cube on a detected floor plane.
        if (spawnedCube == null)
        {
            if (touch.phase == UnityEngine.TouchPhase.Began)
                TryPlaceCube(touch.position);

            return;
        }

        // Once placed, the cube can be selected and manipulated with one finger.
        HandleSingleTouch(touch);
    }

    // Applies movement and rotation using touch-style state values read from the new Input System.
    private void HandleSimulatedTouch(Vector2 screenPosition, Vector2 deltaPosition, bool pressedThisFrame, bool releasedThisFrame, bool isPressed)
    {
        if (pressedThisFrame)
            isSelected = IsTouchOnThisObject(screenPosition);

        if (!isSelected)
            return;

        if (isPressed)
        {
            MoveOnDetectedPlane(screenPosition);

            float rotationAmount = deltaPosition.x * rotationSpeed;
            spawnedCube.transform.Rotate(0f, -rotationAmount, 0f, Space.World);
        }

        if (releasedThisFrame)
            isSelected = false;
    }

    // Reads the current editor pointer state from the new Input System or the legacy Input API as a fallback.
    private bool TryGetEditorPointer(out Vector2 pointerPosition, out Vector2 deltaPosition, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            deltaPosition = Mouse.current.delta.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            isPressed = Mouse.current.leftButton.isPressed;
            releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            return true;
        }

        if (Touchscreen.current != null)
        {
            TouchControl primaryTouch = Touchscreen.current.primaryTouch;
            pointerPosition = primaryTouch.position.ReadValue();
            deltaPosition = primaryTouch.delta.ReadValue();
            pressedThisFrame = primaryTouch.press.wasPressedThisFrame;
            isPressed = primaryTouch.press.isPressed;
            releasedThisFrame = primaryTouch.press.wasReleasedThisFrame;
            return true;
        }
#endif

        pointerPosition = Input.mousePosition;
        deltaPosition = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        pressedThisFrame = Input.GetMouseButtonDown(0);
        isPressed = Input.GetMouseButton(0);
        releasedThisFrame = Input.GetMouseButtonUp(0);
        return true;
    }

    // Attempts to place the prefab on a detected AR plane using the provided screen position.
    private void TryPlaceCube(Vector2 screenPosition)
    {
        if (cubePrefab == null)
        {
            LogPlacementIssue("Cube prefab is not assigned.");
            return;
        }

        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            LogPlacementIssue($"No AR plane hit at screen position {screenPosition}.");
            return;
        }

        ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;
        if (hitPlane == null)
        {
            if (CanPlaceUsingEditorFallback())
            {
                PlaceCubeAtPose(hits[0].pose, "Placed cube using editor fallback pose because no ARPlane was resolved.");
                return;
            }

            LogPlacementIssue("Raycast hit a trackable, but no ARPlane was found from the trackable id.");
            return;
        }

        if (!CanUsePlaneForPlacement(hitPlane))
        {
            LogPlacementIssue(
                $"Plane rejected. alignment={hitPlane.alignment}, planeY={hitPlane.transform.position.y:F3}, groundTolerance={groundTolerance:F3}");
            return;
        }

        PlaceCubeAtPose(hits[0].pose, $"Placed cube on plane '{hitPlane.trackableId}' at {GetPlacementPosition(hits[0].pose.position)}.");
    }

    // Handles selection, dragging, and rotation for the placed object when using mobile touch input.
    private void HandleSingleTouch(Touch touch)
    {
        // Only begin moving/rotating if the touch started on the spawned cube.
            if (touch.phase == UnityEngine.TouchPhase.Began)
            isSelected = IsTouchOnThisObject(touch.position);

        if (!isSelected)
            return;

        if (touch.phase == UnityEngine.TouchPhase.Moved)
        {
            // Keep the cube aligned to the detected ground plane while dragging.
            MoveOnDetectedPlane(touch.position);

            // Horizontal swipe distance controls yaw rotation around the Y axis.
            float rotationAmount = touch.deltaPosition.x * rotationSpeed;
            spawnedCube.transform.Rotate(0f, -rotationAmount, 0f, Space.World);
        }

        if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
            isSelected = false;
    }

    // Checks whether the current pointer/touch is hitting the spawned object or one of its child transforms.
    private bool IsTouchOnThisObject(Vector2 screenPosition)
    {
        if (spawnedCube == null || arCamera == null)
            return false;

        // Touch selection depends on the prefab having a collider.
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform.IsChildOf(spawnedCube.transform);
    }

    // Moves the placed object along the currently detected plane while preserving its height offset.
    private void MoveOnDetectedPlane(Vector2 screenPosition)
    {
        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            return;

        ARPlane hitPlane = planeManager != null ? planeManager.GetPlane(hits[0].trackableId) : null;
        if (hitPlane == null)
        {
            if (CanPlaceUsingEditorFallback())
            {
                spawnedCube.transform.position = GetPlacementPosition(hits[0].pose.position);
                return;
            }

            return;
        }

        if (!CanUsePlaneForPlacement(hitPlane))
            return;

        spawnedCube.transform.position = GetPlacementPosition(hits[0].pose.position);
    }

    // Offsets the object upward so the cube rests on top of the plane rather than intersecting it.
    private Vector3 GetPlacementPosition(Vector3 planePosition)
    {
        // Raise the cube so it sits on top of the plane instead of intersecting it.
        return planePosition + new Vector3(0f, cubeHalfHeight, 0f);
    }

    // Verifies that the target plane is close enough to the lowest horizontal plane to count as the floor.
    private bool IsNearGroundPlane(ARPlane targetPlane)
    {
        if (planeManager == null || targetPlane.alignment != PlaneAlignment.HorizontalUp)
            return false;

        bool foundGroundCandidate = false;
        float groundY = float.MaxValue;

        // Use the lowest upward-facing horizontal plane as the floor reference.
        foreach (ARPlane plane in planeManager.trackables)
        {
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp)
                continue;

            float y = plane.transform.position.y;
            if (!foundGroundCandidate || y < groundY)
            {
                groundY = y;
                foundGroundCandidate = true;
            }
        }

        if (!foundGroundCandidate)
            return false;

        return Mathf.Abs(targetPlane.transform.position.y - groundY) <= groundTolerance;
    }

    // Chooses the placement validation rule, using a looser rule in the Editor for faster simulation testing.
    private bool CanUsePlaneForPlacement(ARPlane targetPlane)
    {
#if UNITY_EDITOR
        if (relaxGroundFilterInEditor)
            return targetPlane != null && targetPlane.alignment == PlaneAlignment.HorizontalUp;
#endif
        return IsNearGroundPlane(targetPlane);
    }

    // Allows editor-only placement when XR Simulation returns a valid pose but no resolved ARPlane component.
    private bool CanPlaceUsingEditorFallback()
    {
#if UNITY_EDITOR
        return relaxGroundFilterInEditor;
#else
        return false;
#endif
    }

    // Instantiates the object or updates its position using the supplied AR pose from a raycast result.
    private void PlaceCubeAtPose(Pose pose, string logMessage)
    {
        Vector3 spawnPosition = GetPlacementPosition(pose.position);

        if (spawnedCube == null)
            spawnedCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
        else
            spawnedCube.transform.position = spawnPosition;

        LogPlacementIssue(logMessage);
    }

    // Applies editor-only keyboard shortcuts so the placed object can be moved and rotated during laptop testing.
    private void HandleEditorKeyboardControls()
    {
#if ENABLE_INPUT_SYSTEM
        if (spawnedCube == null || Keyboard.current == null)
            return;

        Vector3 moveInput = Vector3.zero;
        Keyboard keyboard = Keyboard.current;

        if (keyboard.wKey.isPressed)
            moveInput += Vector3.forward;
        if (keyboard.sKey.isPressed)
            moveInput += Vector3.back;
        if (keyboard.aKey.isPressed)
            moveInput += Vector3.left;
        if (keyboard.dKey.isPressed)
            moveInput += Vector3.right;

        if (keyboard.slashKey.isPressed)
            moveInput += Vector3.forward;
        if (keyboard.commaKey.isPressed)
            moveInput += Vector3.left;
        if (keyboard.periodKey.isPressed)
            moveInput += Vector3.right;

        if (moveInput != Vector3.zero)
        {
            Vector3 forward = arCamera != null ? arCamera.transform.forward : Vector3.forward;
            Vector3 right = arCamera != null ? arCamera.transform.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 worldMove = (forward * moveInput.z + right * moveInput.x) * (editorMoveSpeed * Time.deltaTime);
            spawnedCube.transform.position += worldMove;
        }

        float rotationInput = 0f;
        float verticalInput = 0f;

        if (keyboard.qKey.isPressed)
            rotationInput -= 1f;
        if (keyboard.eKey.isPressed)
            rotationInput += 1f;
        if (keyboard.minusKey.isPressed)
            rotationInput -= 1f;
        if (keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed)
            rotationInput += 1f;

        if (keyboard.numpadMinusKey.isPressed)
            rotationInput -= 1f;

        if (keyboard.slashKey.wasPressedThisFrame)
            LogPlacementIssue("Editor shortcut '/' is mapped to forward movement.");

        if (keyboard.quoteKey.isPressed)
            verticalInput += 1f;
        if (keyboard.semicolonKey.isPressed)
            verticalInput -= 1f;

        if (rotationInput != 0f)
        {
            float rotationAmount = rotationInput * editorRotateSpeed * Time.deltaTime;
            spawnedCube.transform.Rotate(0f, rotationAmount, 0f, Space.World);
        }

        if (verticalInput != 0f)
        {
            Vector3 verticalMove = Vector3.up * (verticalInput * editorMoveSpeed * Time.deltaTime);
            spawnedCube.transform.position += verticalMove;
        }
#endif
    }

    // Writes throttled debug messages to the Console so placement issues can be diagnosed without spam.
    private void LogPlacementIssue(string message)
    {
        if (!enableDebugLogs)
            return;

        // Throttle noisy logs while the user drags or keeps clicking.
        if (Time.unscaledTime - lastFailureLogTime < 0.25f && !message.StartsWith("Placed cube"))
            return;

        lastFailureLogTime = Time.unscaledTime;
        Debug.Log($"[PlaceCubeOnFilteredPlane] {message}", this);
    }
}
