using Tickle;
using Tickle.Lerp;
using UnityEngine;
using UnityEngine.InputSystem;

public unsafe class GameManager : MonoBehaviour
{
    [SerializeField] private Transform _cube;

    private ITickle[][] _tickleChain;

    private void Start()
    {
        var startPosition = new Vector3(0, 0, 0);
        var endPosition = new Vector3(0, 3, 0);
        var startRotation = new Vector3(0, 0, 0);
        var endRotation = new Vector3(0, 270, 0);
        _tickleChain = new ITickle[][]
        {
            new ITickle[]
            {
                _cube.LerpPosition(startPosition, endPosition, 2, Ease.Type.OutCubic),
                _cube.LerpRotation(startRotation, endRotation, 2, Ease.Type.OutCubic)
            },
            new ITickle[]
            {
                _cube.LerpPosition(endPosition, startPosition, 2, Ease.Type.OutCubic),
                _cube.LerpRotation(endRotation, startRotation, 2, Ease.Type.OutCubic)
            }
        }
        .Start();

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