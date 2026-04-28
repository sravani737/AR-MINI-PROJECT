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
    public ARPlaneManager planeManager;

    [Header("UI")]
    public Slider scaleSlider;

    [Header("Settings")]
    public float cubeHalfHeight = 0.1f;
    public float rotationSpeed = 0.2f;
    public float baseModelScale = 0.06f;
    public float minZoom = 0.25f;
    public float maxZoom = 8f;
    public float defaultZoom = 1f;
    public bool disableImportedLights = true;
    public bool hideImportedBackdropPlanes = false;
    public bool stopScanningAfterPlacement = true;
    public bool hidePlanesAfterPlacement = true;

    private ARRaycastManager raycastManager;
    private GameObject spawnedCube;
    private bool isSelected = false;
    private float currentPlaneY;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        if (arCamera == null)
            arCamera = Camera.main;

        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();

        if (scaleSlider == null)
            scaleSlider = FindObjectOfType<Slider>();

        if (scaleSlider != null)
        {
            scaleSlider.minValue = minZoom;
            scaleSlider.maxValue = maxZoom;
            scaleSlider.value = Mathf.Clamp(scaleSlider.value <= 0f ? defaultZoom : scaleSlider.value, minZoom, maxZoom);
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        }
    }

    private void OnDestroy()
    {
        if (scaleSlider != null)
            scaleSlider.onValueChanged.RemoveListener(OnScaleChanged);
    }

    private void Update()
    {
        if (arCamera == null)
            return;

#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleMobileInput();
#endif
    }

    private void HandleEditorInput()
    {
        if (IsPointerOverUI(Input.mousePosition))
            return;

        if (Input.GetMouseButtonDown(0) && spawnedCube == null)
        {
            TryPlaceCube(Input.mousePosition);
            return;
        }

        if (spawnedCube == null)
            return;

        if (Input.GetMouseButtonDown(0))
            isSelected = IsTouchOnObject(Input.mousePosition);

        if (isSelected && Input.GetMouseButton(0))
            MoveOnPlane(Input.mousePosition);

        if (Input.GetMouseButton(1))
        {
            float rot = Input.GetAxis("Mouse X") * 150f * Time.deltaTime;
            spawnedCube.transform.Rotate(0, -rot, 0, Space.World);
        }

        if (Input.GetMouseButtonUp(0))
            isSelected = false;
    }

    private void HandleMobileInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (IsTouchOverUI(touch))
            return;

        if (spawnedCube == null)
        {
            if (touch.phase == TouchPhase.Began)
                TryPlaceCube(touch.position);
            return;
        }

        if (touch.phase == TouchPhase.Began)
            isSelected = IsTouchOnObject(touch.position);

        if (isSelected && touch.phase == TouchPhase.Moved)
        {
            MoveOnPlane(touch.position);

            float rot = touch.deltaPosition.x * rotationSpeed;
            spawnedCube.transform.Rotate(0, -rot, 0, Space.World);
        }

        if (touch.phase == TouchPhase.Ended)
            isSelected = false;
    }

    private void TryPlaceCube(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        Pose pose = hits[0].pose;
        currentPlaneY = pose.position.y;

        spawnedCube = Instantiate(
            cubePrefab,
            pose.position,
            Quaternion.identity
        );

        PrepareSpawnedModelForAR();
        ApplyZoom(scaleSlider != null ? scaleSlider.value : defaultZoom);
        UpdateSelectionCollider();
        AnchorModelBottomToPlane();
        StopScanningAfterPlacement();
    }

    private void MoveOnPlane(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        currentPlaneY = hits[0].pose.position.y;
        spawnedCube.transform.position = hits[0].pose.position;
        AnchorModelBottomToPlane();
    }

    private bool IsTouchOnObject(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.transform.IsChildOf(spawnedCube.transform);

        return false;
    }

    public void OnScaleChanged(float value)
    {
        if (spawnedCube == null)
            return;

        ApplyZoom(value);
        UpdateSelectionCollider();
        AnchorModelBottomToPlane();
    }

    private void ApplyZoom(float value)
    {
        float zoom = Mathf.Clamp(value, minZoom, maxZoom);
        spawnedCube.transform.localScale = Vector3.one * baseModelScale * zoom;
    }

    private void PrepareSpawnedModelForAR()
    {
        if (disableImportedLights)
        {
            Light[] importedLights = spawnedCube.GetComponentsInChildren<Light>(true);
            foreach (Light importedLight in importedLights)
                importedLight.enabled = false;
        }

        Camera[] importedCameras = spawnedCube.GetComponentsInChildren<Camera>(true);
        foreach (Camera importedCamera in importedCameras)
        {
            if (importedCamera != arCamera)
                importedCamera.enabled = false;
        }

        if (!hideImportedBackdropPlanes)
            return;

        Renderer[] renderers = spawnedCube.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (IsImportedBackdropPlane(renderer.transform.name))
                renderer.enabled = false;
        }
    }

    private bool IsImportedBackdropPlane(string objectName)
    {
        return objectName == "Plane"
            || objectName.StartsWith("Plane.")
            || objectName == "Plane001"
            || objectName.StartsWith("Plane001.");
    }

    private void StopScanningAfterPlacement()
    {
        if (!stopScanningAfterPlacement || planeManager == null)
            return;

        planeManager.requestedDetectionMode = PlaneDetectionMode.None;

        if (!hidePlanesAfterPlacement)
            return;

        foreach (ARPlane plane in planeManager.trackables)
            plane.gameObject.SetActive(false);
    }

    private void AnchorModelBottomToPlane()
    {
        if (!TryGetVisibleBounds(out Bounds bounds))
        {
            spawnedCube.transform.position =
                new Vector3(spawnedCube.transform.position.x, currentPlaneY + cubeHalfHeight, spawnedCube.transform.position.z);
            return;
        }

        float yOffset = currentPlaneY - bounds.min.y;
        spawnedCube.transform.position += Vector3.up * yOffset;
    }

    private bool TryGetVisibleBounds(out Bounds bounds)
    {
        Renderer[] renderers = spawnedCube.GetComponentsInChildren<Renderer>(false);
        bounds = new Bounds(spawnedCube.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void UpdateSelectionCollider()
    {
        if (!TryGetVisibleBounds(out Bounds bounds))
            return;

        BoxCollider boxCollider = spawnedCube.GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = spawnedCube.AddComponent<BoxCollider>();

        Vector3 localCenter = spawnedCube.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = spawnedCube.transform.InverseTransformVector(bounds.size);

        boxCollider.center = localCenter;
        boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return IsOverScaleSlider(screenPosition);
    }

    private bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return true;

        return IsOverScaleSlider(touch.position);
    }

    private bool IsOverScaleSlider(Vector2 screenPosition)
    {
        if (scaleSlider == null)
            return false;

        RectTransform sliderRect = scaleSlider.GetComponent<RectTransform>();
        Canvas canvas = scaleSlider.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        return sliderRect != null && RectTransformUtility.RectangleContainsScreenPoint(sliderRect, screenPosition, eventCamera);
    }
}
