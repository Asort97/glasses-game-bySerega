using UnityEngine;
using DG.Tweening;

public class BossApproachCamera : MonoBehaviour
{
    [SerializeField] private BossLevelDirector bossDirector;
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float approachSpeed = 1f;
    [Header("Z Positions")]
    [SerializeField] private float startZ = -11.2f;
    [SerializeField] private float bossZ = -10f;

    private void OnEnable()
    {
        bossDirector.BossProgressChanged += UpdateCameraPosition;
        UpdateCameraPosition(bossDirector.PassedMinigames, bossDirector.MinigamesPerBoss);
    }

    private void OnDisable()
    {
        bossDirector.BossProgressChanged -= UpdateCameraPosition;
    }

    public void ResetImmediately()
    {
        Vector3 position = mainCameraTransform.localPosition;
        position.z = startZ;
        mainCameraTransform.localPosition = position;
    }

    private void UpdateCameraPosition(int passedMinigames, int minigamesPerBoss)
    {
        float progress = Mathf.Clamp01((float)passedMinigames / minigamesPerBoss);
        Vector3 position = mainCameraTransform.position;
        position.z = Mathf.Lerp(startZ, bossZ, progress);

        // mainCameraTransform.position = position;
        mainCameraTransform.DOLocalMoveZ(position.z, approachSpeed);
    }
}
