using NUnit.Framework;
using System.Collections.Generic;
using Tickle;
using Tickle.Easings;
using UnityEngine;

public class SampleTest1 : MonoBehaviour
{
    [SerializeField] private GameObject _carPrefab;
    public int prefabCount;
    private bool running = false;

    [Space]
    [Header("Animation Settings")]
    public float animationDuration;
    public enum AnimationSequence { Chain, Set }
    public AnimationSequence animationSequence;
    [Header("Chain = (Scale -> Position -> Rotation)")]

    //public float TimeScale;
    [Space(20)]
    [Header("Start from current Scale to End Scale")]
    public float endScale;
    public Ease scaleEaseType;

    [Space]
    [Header("Additional Position relative to current Position")]
    public Vector3 endPos;
    public Ease posEaseType;

    [Space]
    [Header("Additional Rotation relative to current Rotation")]
    public Vector3 endRotation;
    public Ease rotEaseType;

    [Header("Changes to this color")]
    public Color color;
    private Color refColor;
    private List<Material> carMaterials = new List<Material>();
    public Ease colorEaseType;

    private List<GameObject> spawnedCars = new List<GameObject>();
    private TickleChain[] animations = new TickleChain[] { };

    private void Start()
    {
        if (prefabCount <= 0 || _carPrefab == null) return;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(prefabCount));
        int rows = Mathf.CeilToInt((float)prefabCount / cols);

        int spawned = 0;
        for (int r = 0; r < rows && spawned < prefabCount; r++)
        {
            for (int c = 0; c < cols && spawned < prefabCount; c++)
            {
                // Center the grid so it expands evenly around (0,0,0)
                float x = (c - (cols - 1) * 0.5f) * 3;
                float z = (r - (rows - 1) * 0.5f) * 3;
                SpawnCar(new Vector3(x, 0, z));
                spawned++;
            }
        }
    }

    private void Update()
    {
        //Time.timeScale = TimeScale;
        PressPlay();

        for (int i = 0; i < spawnedCars.Count; i++)
        {
            carMaterials[i].color = refColor;
        }
    }

    private void PressPlay()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        bool wantRunning = !running;

        for (int i = 0; i < spawnedCars.Count; i++)
        {
            int idx = i;

            if (animations[idx] == null)
            {
                Debug.Log("Started Tickle");
                animations[idx] = BuildChainFor(spawnedCars[idx])
                .OnComplete(() => {
                    Debug.Log($"Chain complete, clearing {idx}");
                    animations[idx] = null;
                }); animations[idx].Start();
            }
            else
            {
                Debug.Log("Pause Or Resume Pressed");
                if (wantRunning) animations[idx].Resume();
                else animations[idx].Pause();
            }
        }
        running = wantRunning;
    }
    private TickleChain BuildChainFor(GameObject car)
    {
        var set = new TickleSet()
            .Join(ScaleAnimation(car))
            .Join(PositionAnimation(car))
            .Join(RotationAnimation(car))
            .Join(ColorAnimation(car));

        if (animationSequence == AnimationSequence.Set)
            return new TickleChain().Chain(set);

        return new TickleChain()
            .Chain(ScaleAnimation(car))
            .Chain(PositionAnimation(car))
            .Chain(RotationAnimation(car))
            .Chain(ColorAnimation(car));
    }

    private TickleSet ScaleAnimation(GameObject car)
    {
        var startScale = car.transform.localScale.x;
        return new TickleSet()
            .Join(car.transform.LerpScale(startScale, endScale, animationDuration, scaleEaseType));
    }

    private TickleSet PositionAnimation(GameObject car)
    {
        var startPos = car.transform.position;
        var end = startPos + endPos;
        return new TickleSet()
            .Join(car.transform.LerpPosition(startPos, end, animationDuration, posEaseType));
    }

    private TickleSet RotationAnimation(GameObject car)
    {
        var startRotation = car.transform.localEulerAngles;
        var endRot = startRotation + endRotation;
        return new TickleSet()
            .Join(car.transform.LerpRotation(startRotation, endRot, animationDuration, rotEaseType));
    }

    private TickleSet ColorAnimation(GameObject car)
    {
        var material = car.GetComponent<MeshRenderer>()?.material;
        refColor = material.color;
        var endColor = color;
        return new TickleSet()
            .Join(refColor.a.Lerp(refColor.a, endColor.a, animationDuration, colorEaseType))
            .Join(refColor.r.Lerp(refColor.r, endColor.r, animationDuration, colorEaseType))
            .Join(refColor.g.Lerp(refColor.g, endColor.g, animationDuration, colorEaseType))
            .Join(refColor.b.Lerp(refColor.b, endColor.b, animationDuration, colorEaseType));
    }

    private void SpawnCar(Vector3 position)
    {
        var car = Instantiate(_carPrefab);
        spawnedCars.Add(car);
        animations = new TickleChain[spawnedCars.Count];
        carMaterials.Add(car.GetComponent<MeshRenderer>()?.material);

        var neutralPos = Vector3.zero + position;
        var startRotation = new Vector3(0, 90, 0);

        car.transform.position = neutralPos;
        car.transform.localRotation = Quaternion.Euler(startRotation);
    }
}
