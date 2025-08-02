using Tickle;
using Tickle.Lerp;
using UnityEngine;
using UnityEngine.InputSystem;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _prefab;

    private ITickle[][] _tickleChain;

    private void Start()
    {
        _tickleChain = SpawnCube(Vector3.zero);
        _tickleChain.Start();
    }

    private TickleChain SpawnCube(Vector3 startPosition)
    {
        var cube = Instantiate(_prefab).transform;

        var endPosition = new Vector3(0, 3, 0) + startPosition;
        var startRotation = new Vector3(0, 0, 0);
        var endRotation = new Vector3(0, 270, 0);
        return new ITickle[][]
        {
            new ITickle[]
            {
                cube.LerpPosition(startPosition, endPosition, 2, Ease.Type.JumpQuad),
                cube.LerpRotation(startRotation, endRotation, 2, Ease.Type.JumpQuad)
            },
        };
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _tickleChain.Stop();
            _tickleChain.Start();
        }
    }
}