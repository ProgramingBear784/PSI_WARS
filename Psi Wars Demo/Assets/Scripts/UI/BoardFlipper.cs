using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Rotates the board 180° around Z to visually pass the device to the other player.
/// After the animation resets to 0° and fires onFlipComplete so the UI can
/// re-populate for the new active player right-side-up.
/// </summary>
public class BoardFlipper : MonoBehaviour
{
    private RectTransform _boardRoot;
    [SerializeField] private float flipDuration = 0.6f;

    public UnityEvent onFlipComplete = new UnityEvent();

    /// <summary>Called by PsiWarsGame to wire up the root without Inspector setup.</summary>
    public void SetBoardRoot(RectTransform rt) => _boardRoot = rt;

    public void Flip() => StartCoroutine(DoFlip());

    private IEnumerator DoFlip()
    {
        if (_boardRoot == null) { onFlipComplete.Invoke(); yield break; }

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flipDuration);
            _boardRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 180f, t));
            yield return null;
        }

        _boardRoot.localEulerAngles = Vector3.zero;
        onFlipComplete.Invoke();
    }
}
