using UnityEngine;
using PrimeTween;

public class Factory : MonoBehaviour
{
    [Header("Setup"), SerializeField] private Transform _packOrigin;
    [SerializeField] private GameObject _packPrefab;
    private int _packCount = 10;
    private float _packRadius = 2.2f;

    [Header("Controls"), SerializeField] private float _sensitivity = 150f;
    private float _yaw;
    private Tween _alignTween;

    void Start()
    {
        SpawnPacks();  
    }

    private void Update()
    {
        // TODO: Convert all logic to use InputActions
        if (Input.GetMouseButton(0))
        {
            _yaw += Input.GetAxis("Mouse X") * _sensitivity * Time.deltaTime;
            _packOrigin.rotation = Quaternion.Euler(0, -_yaw, 0);

            if (_alignTween.isAlive)
            {
                _alignTween.Stop();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            AlignPacks();
        }
    }

    private void SpawnPacks()
    {
        // Spawn packs in a circle around the origin
        for (int i = 0; i < _packCount; i++)
        {
            float angle = i * Mathf.PI * 2 / _packCount;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _packRadius;
            Quaternion rot = Quaternion.LookRotation(-pos);
            Instantiate(_packPrefab, pos, rot, _packOrigin);
        }

        AlignPacks();
    }

    private void AlignPacks()
    {
        // Align the packs to the nearest angle
        float angle = 360 / _packCount;
        float eulerAngleY = _packOrigin.eulerAngles.y;
        float remainder = Mathf.Abs(eulerAngleY % angle);
        float offset = angle / 2;

        // Decide which direction to round to
        if (remainder > offset)
        {
            eulerAngleY = Mathf.Ceil(eulerAngleY / angle) * angle - offset;
        }
        else
        {
            eulerAngleY = Mathf.Floor(eulerAngleY / angle) * angle + offset;
        }

        float transitionTime = 0.75f;
        _alignTween = Tween.Rotation(_packOrigin, Quaternion.Euler(0, eulerAngleY, 0), transitionTime, Ease.OutCirc);
    }
}
