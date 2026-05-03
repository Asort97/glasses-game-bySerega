using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private float fallSpeed     = 5f;
    [SerializeField] private float destroyBelowY = -7f;

    private Transform                _leftSpawn;
    private Transform                _rightSpawn;
    private readonly List<Transform> _active = new List<Transform>();

    private int  _sameWallCount = 0;
    private bool _lastWasLeft;

    private void Awake()
    {
        _leftSpawn  = transform.Find("leftWallSpawnpoint");
        _rightSpawn = transform.Find("rightWallSpawnpoint");

        if (_leftSpawn  == null) Debug.LogError("[SpikeSpawner] leftWallSpawnpoint not found!");
        if (_rightSpawn == null) Debug.LogError("[SpikeSpawner] rightWallSpawnpoint not found!");
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var t = _active[i];
            if (t == null) { _active.RemoveAt(i); continue; }

            t.position += Vector3.down * fallSpeed * Time.deltaTime;

            if (t.position.y < destroyBelowY)
            {
                Destroy(t.gameObject);
                _active.RemoveAt(i);
            }
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            // Если уже 3 шипа подряд на одной стене — обязательно другая
            bool spawnLeft;
            if (_sameWallCount >= 3)
                spawnLeft = !_lastWasLeft;
            else
                spawnLeft = Random.value > 0.5f;

            // Обновляем счётчик ПОСЛЕ выбора стены
            if (spawnLeft == _lastWasLeft)
                _sameWallCount++;
            else
                _sameWallCount = 1;
            _lastWasLeft = spawnLeft;

            // Спавним ОДИН шип
            var point = spawnLeft ? _leftSpawn : _rightSpawn;
            if (point != null)
            {
                var go = Instantiate(spikePrefab, point.position, Quaternion.identity, transform);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = spawnLeft;
                _active.Add(go.transform);
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
