using UnityEngine;
using PrimeTween;
using UniRx;
using System.Collections.Generic;

public class PullSimulator : MonoBehaviour
{
    [Header("Setup"), SerializeField] private Database _database;
    [SerializeField] private Transform _packOrigin;
    [SerializeField] private Pack _packPrefab;
    private int _packCount = 10;
    private float _packRadius = 2.2f;

    [Header("Controls"), SerializeField] private float _sensitivity = 150f;
    private float _yaw;
    private Tween _alignTween;

    [Header("Pull")] 
    [SerializeField] private WorldCard _cardPrefab;
    private List<Pack> _packs = new List<Pack>();
    [SerializeField] private List<Card> _cards = new List<Card>();
    private Pack _closestPack;

    private void Awake()
    {
        MainEventHandler.ListenForEventStream<PackChooseEvent>().Subscribe(OnPackChooseEvent).AddTo(this);
        MainEventHandler.ListenForEventStream<PackOpenEvent>().Subscribe(OnPackOpenEvent).AddTo(this);
    }

    void Start()
    {
        SpawnPacks();  
    }

    private void Update()
    {
        if (_packs.Count > 0)
        {
            RotatePacks();
        }
    }

    private void OnPackChooseEvent(PackChooseEvent packOpenEvent)
    {
        Pack chosenPack = packOpenEvent.Pack;

        if (chosenPack != _closestPack) return;

        // Destroy all packs except the selected pack
        foreach (Pack pack in _packs)
        {
            if (pack != chosenPack)
            {
                Destroy(pack.gameObject);
            }
        }
        
        _packs.Clear();

        chosenPack.StopFloating();
        Tween.Position(chosenPack.transform, new Vector3(0, 0, -2.25f), 0.25f, Ease.OutQuint);
        Tween.Rotation(chosenPack.transform, Quaternion.identity, 0.25f, Ease.OutQuint);

        Tween.Position(Camera.main.transform, new Vector3(0, 0.5f, -3.25f), 1f, Ease.OutQuint);
    }

    private void OnPackOpenEvent(PackOpenEvent packOpenEvent)
    {
        // Open pack and show cards
        foreach (Card card in _cards)
        {
            WorldCard worldCard = Instantiate(_cardPrefab, packOpenEvent.Pack.transform);
            worldCard.Initialize(card);
        }
    }

    private void SpawnPacks()
    {
        _cards = DrawCards();

        for (int i = 0; i < _packCount; i++)
        {
            // Spawn packs in a circle around the origin
            float angle = i * Mathf.PI * 2 / _packCount;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _packRadius;
            Quaternion rot = Quaternion.LookRotation(-pos);
            Pack pack = Instantiate(_packPrefab, pos, rot, _packOrigin);

            // Simulate float animation with offset
            pack.Float(i * 0.15f);

            // Keep reference 
            _packs.Add(pack);
        }

        AlignPacks();
    }

    private void RotatePacks()
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

        FindClosestPack();
    }

    private void FindClosestPack()
    {
        Pack closestPack = null;
        
        // Find the closest pack to the camera
        foreach (Pack pack in _packs)
        {
            if (closestPack == null || Vector3.Distance(pack.transform.position, Camera.main.transform.position) < Vector3.Distance(closestPack.transform.position, Camera.main.transform.position))
            {
                closestPack = pack;
            }
        }

        _closestPack = closestPack;

        Debug.Log("Closest pack: " + _closestPack.name);
    }

    private List<Card> DrawCards()
    {
        List<Card> cards = new List<Card>();
        float randomChance;
        ERarity rarity;

        for (int i = 0; i < 5; i++)
        {
            randomChance = Random.Range(0f, 1f);

            if (randomChance < 0.005f)
            {
                rarity = ERarity.CrownRare;
            }
            else if (randomChance < 0.3f) // 0.01f + (i * 0.005)) // 3% on last pull
            {
                rarity = ERarity.UltraRare;
            }
            else if (randomChance < 0.2f + (i * 0.04)) 
            {
                rarity = ERarity.Rare;
            }
            else
            {
                rarity = ERarity.Common;
            }

            cards.Add(_database.GetCard(rarity));
        }

        return cards;
    }
}
