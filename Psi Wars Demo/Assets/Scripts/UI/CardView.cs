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
    public GameObject       depletedOverlay;

    public CardInstance CardInstance { get; private set; }

    private System.Action<CardView> _onClick;

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
        if (depletedOverlay != null) depletedOverlay.SetActive(on);
        if (artworkImage    != null) artworkImage.color = on ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
    }

    public void SetDisoriented(bool on)
    {
        if (disorientedOverlay != null) disorientedOverlay.SetActive(on);
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData _) => _onClick?.Invoke(this);
}
