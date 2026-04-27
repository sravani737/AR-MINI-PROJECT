using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceCubeOnFilteredPlane : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject cubePrefab;

    [Header("AR")]
    public Camera arCamera;

    [Header("UI")]
    public Slider scaleSlider;

    [Header("Settings")]
    public float cubeHalfHeight = 0.1f;
    public float rotationSpeed = 0.2f;

    private ARRaycastManager raycastManager;
    private GameObject spawnedCube;
    private bool isSelected = false;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        if (arCamera == null)
            arCamera = Camera.main;

        if (scaleSlider == null)
            scaleSlider = FindObjectOfType<Slider>();

        if (scaleSlider != null)
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
    }

    private void Update()
    {
        if (arCamera == null)
            return;

        // 🚫 Ignore UI touch
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileInput();
#endif
    }

    // =========================
    // 🖥️ EDITOR INPUT
    // =========================
    private void HandleEditorInput()
    {
        // PLACE
        if (Input.GetMouseButtonDown(0) && spawnedCube == null)
        {
            TryPlaceCube(Input.mousePosition);
            return;
        }

        if (spawnedCube == null)
            return;

        // SELECT
        if (Input.GetMouseButtonDown(0))
        {
            isSelected = IsTouchOnObject(Input.mousePosition);
        }

        // MOVE (only if selected)
        if (isSelected && Input.GetMouseButton(0))
        {
            MoveOnPlane(Input.mousePosition);
        }

        // ROTATE (right click)
        if (Input.GetMouseButton(1))
        {
            float rot = Input.GetAxis("Mouse X") * 150f * Time.deltaTime;
            spawnedCube.transform.Rotate(0, -rot, 0, Space.World);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSelected = false;
        }
    }

    // =========================
    // 📱 MOBILE INPUT
    // =========================
    private void HandleMobileInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (spawnedCube == null)
        {
            if (touch.phase == TouchPhase.Began)
                TryPlaceCube(touch.position);
            return;
        }

        if (touch.phase == TouchPhase.Began)
        {
            isSelected = IsTouchOnObject(touch.position);
        }

        if (isSelected && touch.phase == TouchPhase.Moved)
        {
            MoveOnPlane(touch.position);

            float rot = touch.deltaPosition.x * rotationSpeed;
            spawnedCube.transform.Rotate(0, -rot, 0, Space.World);
        }

        if (touch.phase == TouchPhase.Ended)
        {
            isSelected = false;
        }
    }

    // =========================
    // 📍 PLACE
    // =========================
    private void TryPlaceCube(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;

        spawnedCube = Instantiate(
            cubePrefab,
            pose.position + Vector3.up * cubeHalfHeight,
            Quaternion.identity
        );

        // 🔥 IMPORTANT: ensure collider exists
        if (spawnedCube.GetComponent<Collider>() == null)
            spawnedCube.AddComponent<BoxCollider>();
    }

    // =========================
    // 📍 MOVE
    // =========================
    private void MoveOnPlane(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        spawnedCube.transform.position =
            hits[0].pose.position + Vector3.up * cubeHalfHeight;
    }

    // =========================
    // 🎯 SELECTION
    // =========================
    private bool IsTouchOnObject(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform.IsChildOf(spawnedCube.transform);
        }

        return false;
    }

    // =========================
    // 🎚️ SCALE
    // =========================
    private void OnScaleChanged(float value)
    {
        if (spawnedCube != null)
        {
            spawnedCube.transform.localScale = Vector3.one * value;
        }
    }
}