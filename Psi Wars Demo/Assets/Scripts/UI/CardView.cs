using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Lightweight card component. Fields are assigned in code by PsiWarsGame.SpawnCard().
/// Handles click events, selection highlight, grayscale-on-deplete, and disorientation overlay.
/// </summary>
public class CardView : MonoBehaviour, IPointerClickHandler
{
    // Set by PsiWarsGame.SpawnCard() before Init() is called
    public Image            artworkImage;
    public TextMeshProUGUI  nameText;
    public GameObject       selectedHighlight;
    public GameObject       disorientedOverlay;

    public CardInstance CardInstance { get; private set; }

    private System.Action<CardView> _onClick;
    private Material _grayscaleMat;

    public void Init(CardInstance instance, System.Action<CardView> onClick)
    {
        CardInstance = instance;
        _onClick     = onClick;
        ApplyState();
    }

    public void Refresh() => ApplyState();

    private void ApplyState()
    {
        if (CardInstance == null) return;
        SetDepleted(CardInstance.isDepleted);
        SetDisoriented(CardInstance.isDisoriented);
    }

    // ── State visuals ─────────────────────────────────────────────────────────

    public void SetSelected(bool on)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(on);
    }

    public void SetDepleted(bool on)
    {
        if (artworkImage == null) return;
        if (on)
        {
            if (_grayscaleMat == null)
            {
                var shader = Shader.Find("UI/Grayscale");
                _grayscaleMat = shader != null ? new Material(shader) : null;
            }
            artworkImage.material = _grayscaleMat;
        }
        else
        {
            artworkImage.material = null;
        }
    }

    public void SetDisoriented(bool on)
    {
        if (disorientedOverlay != null) disorientedOverlay.SetActive(on);
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData _) => _onClick?.Invoke(this);
}
