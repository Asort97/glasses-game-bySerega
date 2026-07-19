using System.Collections.Generic;
using UnityEngine;

public class DoodlerPlatformSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject platformTemplate;
    [SerializeField] private Transform platformParent;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private GameObject leftWallTemplate;
    [SerializeField] private GameObject rightWallTemplate;
    [SerializeField] private LensHealthSystem rightLensHealth;

    [Header("Platforms")]
    [SerializeField] private float startHeight = -1.16f;
    [SerializeField] private float heightStep = 2f;
    [SerializeField] private Vector2 horizontalRange = new Vector2(-2f, 2f);
    [SerializeField] private int platformsAhead = 6;

    [Header("Walls")]
    [SerializeField] private float wallHeightStep = 6f;

    [Header("Camera")]
    [SerializeField] private float cameraMoveSpeed = 8f;
    [SerializeField] private float landedPlatformBottomPadding = 0.1f;

    private readonly List<GameObject> _spawnedPlatforms = new List<GameObject>();
    private readonly List<GameObject> _spawnedWalls = new List<GameObject>();
    private int _highestSpawnedIndex;
    private int _highestLandedIndex;
    private int _highestSpawnedWallIndex;
    private int _selectedNumber = 1;
    private int _lastPlatformNumber;
    private int _twoPlatformsAgoNumber;
    private DoodlerPlatform _templatePlatform;
    private float _cameraStartY;
    private float _cameraTargetY;
    private bool _fallHandled;

    private void Awake()
    {
        if (platformParent == null)
            platformParent = transform;

        if (gameCamera == null)
            return;

        _cameraStartY = gameCamera.transform.position.y;
        _cameraTargetY = _cameraStartY;
    }

    private void OnEnable()
    {
        ResetPlatforms();
    }

    private void OnDisable()
    {
        ClearSpawnedPlatforms();
        ClearSpawnedWalls();
    }

    private void LateUpdate()
    {
        if (gameCamera == null)
            return;

        Vector3 position = gameCamera.transform.position;
        position.y = Mathf.MoveTowards(
            position.y,
            _cameraTargetY,
            cameraMoveSpeed * Time.deltaTime);
        gameCamera.transform.position = position;
    }

    public void LandedOn(DoodlerPlatform platform)
    {
        if (platform == null || platform.Index <= _highestLandedIndex)
            return;

        _highestLandedIndex = platform.Index;
        _cameraTargetY = platform.transform.position.y
            + gameCamera.orthographicSize
            - landedPlatformBottomPadding;

        int requiredHighestIndex = _highestLandedIndex + platformsAhead;
        while (_highestSpawnedIndex < requiredHighestIndex)
            SpawnNextPlatform();

        EnsureWallsForPlatform(_highestSpawnedIndex);
    }

    public void SetSelectedNumber(int selectedNumber)
    {
        _selectedNumber = Mathf.Clamp(selectedNumber, 1, 4);

        if (_templatePlatform != null)
            _templatePlatform.SetUsable(_templatePlatform.RequiredNumber == _selectedNumber);

        foreach (GameObject platform in _spawnedPlatforms)
        {
            if (platform == null)
                continue;

            DoodlerPlatform marker = platform.GetComponent<DoodlerPlatform>();
            if (marker != null)
                marker.SetUsable(marker.RequiredNumber == _selectedNumber);
        }
    }

    public bool TryHandleDoodlerFall(float doodlerY)
    {
        if (_fallHandled || gameCamera == null)
            return _fallHandled;

        float cameraBottom = gameCamera.transform.position.y - gameCamera.orthographicSize;
        if (doodlerY >= cameraBottom)
            return false;

        _fallHandled = true;
        rightLensHealth.OnLose();
        return true;
    }

    private void ResetPlatforms()
    {
        if (platformTemplate == null || gameCamera == null)
            return;

        ClearSpawnedPlatforms();
        ClearSpawnedWalls();

        _highestLandedIndex = 0;
        _highestSpawnedIndex = 0;
        _highestSpawnedWallIndex = 0;
        _lastPlatformNumber = 0;
        _twoPlatformsAgoNumber = 0;
        _fallHandled = false;
        _cameraTargetY = _cameraStartY;

        platformTemplate.transform.SetParent(platformParent, false);
        platformTemplate.transform.localPosition = CreateLocalPosition(0);
        platformTemplate.SetActive(true);
        ConfigurePlatform(platformTemplate, 0);

        for (int i = 1; i <= platformsAhead; i++)
            SpawnNextPlatform();

        PrepareWallTemplates();
        EnsureWallsForPlatform(_highestSpawnedIndex);
    }

    private void SpawnNextPlatform()
    {
        _highestSpawnedIndex++;

        GameObject platform = Instantiate(platformTemplate, platformParent);
        platform.name = $"Platform {_highestSpawnedIndex}";
        platform.transform.localPosition = CreateLocalPosition(_highestSpawnedIndex);
        ConfigurePlatform(platform, _highestSpawnedIndex);
        platform.SetActive(true);

        _spawnedPlatforms.Add(platform);
    }

    private void PrepareWallTemplates()
    {
        if (leftWallTemplate != null)
        {
            leftWallTemplate.transform.SetParent(platformParent, false);
            leftWallTemplate.SetActive(true);
        }

        if (rightWallTemplate != null)
        {
            rightWallTemplate.transform.SetParent(platformParent, false);
            rightWallTemplate.SetActive(true);
        }
    }

    private void EnsureWallsForPlatform(int platformIndex)
    {
        if (leftWallTemplate == null || rightWallTemplate == null)
            return;

        float highestPlatformY = startHeight + heightStep * platformIndex;
        float firstWallY = leftWallTemplate.transform.localPosition.y;
        int requiredWallIndex = Mathf.Max(
            0,
            Mathf.CeilToInt((highestPlatformY - firstWallY) / wallHeightStep));

        while (_highestSpawnedWallIndex < requiredWallIndex)
            SpawnNextWallPair();
    }

    private void SpawnNextWallPair()
    {
        _highestSpawnedWallIndex++;
        SpawnWall(leftWallTemplate, $"Left Wall {_highestSpawnedWallIndex}");
        SpawnWall(rightWallTemplate, $"Right Wall {_highestSpawnedWallIndex}");
    }

    private void SpawnWall(GameObject template, string wallName)
    {
        GameObject wall = Instantiate(template, platformParent);
        wall.name = wallName;

        Vector3 position = template.transform.localPosition;
        position.y += wallHeightStep * _highestSpawnedWallIndex;
        wall.transform.localPosition = position;
        wall.SetActive(true);

        _spawnedWalls.Add(wall);
    }

    private Vector3 CreateLocalPosition(int index)
    {
        Vector3 position = platformTemplate.transform.localPosition;
        position.x = Random.Range(horizontalRange.x, horizontalRange.y);
        position.y = startHeight + heightStep * index;
        return position;
    }

    private void ConfigurePlatform(GameObject platform, int index)
    {
        DoodlerPlatform marker = platform.GetComponent<DoodlerPlatform>();
        if (marker == null)
        {
            Debug.LogError($"{platform.name} needs a DoodlerPlatform component.", platform);
            return;
        }

        marker.Initialize(
            index,
            _selectedNumber,
            _lastPlatformNumber,
            _twoPlatformsAgoNumber,
            horizontalRange);
        _twoPlatformsAgoNumber = _lastPlatformNumber;
        _lastPlatformNumber = marker.RequiredNumber;

        if (platform == platformTemplate)
            _templatePlatform = marker;
    }

    private void ClearSpawnedPlatforms()
    {
        foreach (GameObject platform in _spawnedPlatforms)
        {
            if (platform != null)
                Destroy(platform);
        }

        _spawnedPlatforms.Clear();
    }

    private void ClearSpawnedWalls()
    {
        foreach (GameObject wall in _spawnedWalls)
        {
            if (wall != null)
                Destroy(wall);
        }

        _spawnedWalls.Clear();
    }
}
