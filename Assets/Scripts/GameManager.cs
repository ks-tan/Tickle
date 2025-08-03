using Tickle;
using Tickle.Easings;
using UnityEngine;
using UnityEngine.InputSystem;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _prefab;

    private TickleChain[] _tickleChains;

    private void Start()
    {
        var numCubes = 10;
        _tickleChains = new TickleChain[numCubes];
        for(int i = 0; i < numCubes; i++)
        {
            var tickleChain = new TickleChain();
            tickleChain.Chain(Tickler.WaitForSeconds(0.1f * i));
            tickleChain.Chain(SpawnCube(new Vector3(i, 0, 0)));
            tickleChain.Start();
            _tickleChains[i] = tickleChain;
        }
    }

    private TickleSet SpawnCube(Vector3 startPosition)
    {
        var endPosition = new Vector3(0, 3, 0) + startPosition;
        var startRotation = new Vector3(0, 0, 0);
        var endRotation = new Vector3(0, 270, 0);

        var cube = Instantiate(_prefab).transform;
        cube.position = startPosition;
        cube.rotation = Quaternion.Euler(startRotation);

        var tickleSet = new TickleSet()
            .Join(cube.LerpPosition(startPosition, endPosition, 1, Ease.JumpQuad))
            .Join(cube.LerpRotation(startRotation, endRotation, 1, Ease.JumpQuad));
        return tickleSet;
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach(var tickleChain in _tickleChains)
            {
                tickleChain.Stop();
                tickleChain.Start();
            }
        }
    }
}