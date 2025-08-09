using Tickle;
using Tickle.Easings;
using UnityEngine;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject _carPrefab;

    private void Start()
    {
        for (int i = -10; i < 10; i += 3)
            for (int j = -10; j < 10; j += 3)
                SpawnCar(new Vector3(i, 0, j));
    }

    private void SpawnCar(Vector3 position)
    {
        var car= Instantiate(_carPrefab);

        var entryStartPos = new Vector3(-7, 0, 0) + position;
        var neutralPos = Vector3.zero + position;
        var entryBouncePos = new Vector3(-0.25f, 0, 0) + position;
        var shrinkPos = new Vector3(0, -0.1f, 0) + position;
        var startRotation = new Vector3(0, 90, 0);
        var endRotation = startRotation;
        endRotation.y += 360;

        car.transform.position = entryStartPos;
        car.transform.localRotation = Quaternion.Euler(startRotation);

        var carEntry = new TickleChain()
            .Chain(car.transform.LerpPosition(entryStartPos, neutralPos, duration: 0.5f, Ease.OutQuad))
            .Chain(car.transform.LerpPosition(neutralPos, entryBouncePos, duration: 0.25f, Ease.BounceQuad));

        var carShrink = new TickleSet()
            .Join(car.transform.LerpScale(start: 1, end: 0.8f, duration: 1f, Ease.OutQuad))
            .Join(car.transform.LerpPosition(neutralPos, shrinkPos, duration: 1f, Ease.OutQuad));

        var carGrow = new TickleSet()
            .Join(car.transform.LerpScale(start: 0.8f, end: 1f, duration: 0.5f, Ease.OutElastic))
            .Join(car.transform.LerpPosition(shrinkPos, neutralPos, duration: 0.5f, Ease.OutQuad))
            .Join(car.transform.LerpRotation(startRotation, endRotation, duration: 0.2f, Ease.OutQuad));

        new TickleChain()
            .Chain(Tickler.WaitForSeconds(1))
            .Chain(carEntry)
            .Chain(Tickler.WaitForSeconds(0.25f))
            .Chain(carShrink)
            .Chain(carGrow)
            .Start();
    }
}