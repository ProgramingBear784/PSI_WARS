using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Self-contained game controller.
/// Add this to ONE empty GameObject in any Unity scene.
/// It destroys Mirror, hides old UI, builds all UI in code, and runs Psi Wars.
/// No Inspector wiring required.
/// </summary>
public class PsiWarsGame : MonoBehaviour
{
    // ── UI built at runtime ───────────────────────────────────────────────────
    private Canvas        _canvas;
    private RectTransform _board;
    private BoardFlipper  _flipper;

    private GameObject _setupPanel, _gamePanel, _msgPanel, _logPanel, _winPanel;

    // Setup
    private int             _selHP = 15;
    private TextMeshProUGUI _setupHPLabel;

    // HUD sidebar
    private TextMeshProUGUI _p0HP, _p1HP, _phaseLabel, _turnLabel, _statusLabel, _deckLabel;
    private Button          _btnNext, _btnAct;
    private TextMeshProUGUI _btnNextLbl, _btnActLbl;

    // Card zones (Content transforms inside ScrollViews)
    private Transform _zoneOpCU, _zoneOpLab, _zoneMyLab, _zoneMyCU, _zoneHand;

    // Overlays
    private TextMeshProUGUI _msgLabel, _logLabel, _winLabel;
    private Button          _btnMsgOk, _btnLogOk, _btnReplay;

    // ── Game state ────────────────────────────────────────────────────────────
    private enum BattleSub { None, DeclareAtk, PassToDefend, DeclareDefend, Done }
    private BattleSub _bSub = BattleSub.None;

    private readonly List<CardInstance>                     _attackers = new List<CardInstance>();
    private readonly Dictionary<CardInstance, CardInstance> _defenders = new Dictionary<CardInstance, CardInstance>();
    private CardInstance _pendingDef;
    private CardView     _selHandCard, _selLabCard;
    private readonly List<CardView> _views = new List<CardView>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Disable old canvas GameObjects entirely so their UI elements can't block input
        foreach (var c in FindObjectsOfType<Canvas>())
            c.gameObject.SetActive(false);
    }

    private void Start()
    {
        EnsureEventSystem();
        EnsureGameManager();
        BuildUI();
        ShowPanel(_setupPanel);
    }

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private static void EnsureEventSystem()
    {
        // Search inactive objects too in case the old EventSystem was deactivated with its canvas
        var existing = FindObjectOfType<EventSystem>(true);
        if (existing != null) { existing.gameObject.SetActive(true); return; }
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // Canvas
        var cvGO = new GameObject("PsiWarsCanvas");
        _canvas = cvGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Board root — rotated by BoardFlipper for the 180° flip
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(_canvas.transform, false);
        _board = boardGO.AddComponent<RectTransform>();
        Stretch(_board);
        _flipper = boardGO.AddComponent<BoardFlipper>();
        _flipper.SetBoardRoot(_board);

        BuildSetupPanel();
        BuildGamePanel();
        BuildMsgPanel();
        BuildLogPanel();
        BuildWinPanel();
    }

    // ── Setup panel ──────────────────────────────────────────────────────────

    private void BuildSetupPanel()
    {
        _setupPanel = MakePanel("Setup", _board, new Color(0.04f, 0.04f, 0.08f));
        Stretch(_setupPanel.GetComponent<RectTransform>());

        var title = MakeText("PSI  WARS", _setupPanel.transform, 80, FontStyles.Bold);
        PlaceAnchored(title, 0.15f, 0.72f, 0.85f, 0.92f);
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.2f, 0.85f, 1f);

        var sub = MakeText("PLANETARY DOMINATION", _setupPanel.transform, 22);
        PlaceAnchored(sub, 0.2f, 0.66f, 0.8f, 0.73f);
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = new Color(0.6f, 0.6f, 0.7f);

        _setupHPLabel = MakeText("Starting Lab HP: 15", _setupPanel.transform, 32);
        PlaceAnchored(_setupHPLabel, 0.25f, 0.56f, 0.75f, 0.64f);
        _setupHPLabel.alignment = TextAlignmentOptions.Center;

        // HP row
        var hpRow = new GameObject("HPRow");
        hpRow.transform.SetParent(_setupPanel.transform, false);
        var hpRT = hpRow.AddComponent<RectTransform>();
        hpRT.anchorMin = new Vector2(0.28f, 0.44f);
        hpRT.anchorMax = new Vector2(0.72f, 0.55f);
        hpRT.offsetMin = hpRT.offsetMax = Vector2.zero;
        var hlg = hpRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        MakeBtn("10 HP", hpRow.transform, () => SetHP(10), new Color(0.2f, 0.3f, 0.5f));
        MakeBtn("15 HP", hpRow.transform, () => SetHP(15), new Color(0.2f, 0.3f, 0.5f));
        MakeBtn("20 HP", hpRow.transform, () => SetHP(20), new Color(0.2f, 0.3f, 0.5f));

        var startBtn = MakeBtn("START GAME", _setupPanel.transform, OnStartGame, new Color(0.1f, 0.55f, 0.1f));
        PlaceAnchored(startBtn, 0.35f, 0.30f, 0.65f, 0.42f);
        startBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 36;
    }

    private void SetHP(int hp) { _selHP = hp; _setupHPLabel.text = $"Starting Lab HP: {hp}"; }

    // ── Game panel ────────────────────────────────────────────────────────────

    private void BuildGamePanel()
    {
        _gamePanel = new GameObject("GamePanel");
        _gamePanel.transform.SetParent(_board, false);
        Stretch(_gamePanel.AddComponent<RectTransform>());
        _gamePanel.SetActive(false);

        // ─ Sidebar (right 13%) ────────────────────────────────────────────────
        var sidebar = MakePanel("Sidebar", _gamePanel.transform, new Color(0.07f, 0.07f, 0.12f));
        var sideRT = sidebar.GetComponent<RectTransform>();
        sideRT.anchorMin = new Vector2(0.87f, 0f); sideRT.anchorMax = Vector2.one;
        sideRT.offsetMin = sideRT.offsetMax = Vector2.zero;
        var vlg = sidebar.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 14, 14);
        vlg.spacing = 8;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        _p0HP = AddSideText(sidebar, "P1: -- HP", 17);
        _p1HP = AddSideText(sidebar, "P2: -- HP", 17);

        _phaseLabel = AddSideText(sidebar, "Phase", 20, FontStyles.Bold);
        _phaseLabel.color = new Color(1f, 0.85f, 0.1f);
        _phaseLabel.alignment = TextAlignmentOptions.Center;
        _phaseLabel.GetComponent<LayoutElement>().preferredHeight = 56;

        _turnLabel = AddSideText(sidebar, "Turn", 15);
        _turnLabel.color = new Color(0.6f, 0.9f, 1f);
        _turnLabel.alignment = TextAlignmentOptions.Center;

        AddSpacer(sidebar, 10);

        _statusLabel = AddSideText(sidebar, "", 13);
        _statusLabel.enableWordWrapping = true;
        _statusLabel.GetComponent<LayoutElement>().preferredHeight = 130;

        AddSpacer(sidebar, 8);

        _btnAct = MakeBtn("Action", sidebar.transform, null, new Color(0.18f, 0.45f, 0.75f));
        _btnAct.GetComponent<LayoutElement>().preferredHeight = 62;
        _btnActLbl = _btnAct.GetComponentInChildren<TextMeshProUGUI>();

        _btnNext = MakeBtn("Next →", sidebar.transform, null, new Color(0.12f, 0.12f, 0.18f));
        _btnNext.GetComponent<LayoutElement>().preferredHeight = 62;
        _btnNextLbl = _btnNext.GetComponentInChildren<TextMeshProUGUI>();

        AddSpacer(sidebar, 6);

        _deckLabel = AddSideText(sidebar, "", 12);
        _deckLabel.enableWordWrapping = true;
        _deckLabel.GetComponent<LayoutElement>().preferredHeight = 80;
        _deckLabel.color = new Color(0.55f, 0.55f, 0.65f);

        // ─ Main area (left 87%) ───────────────────────────────────────────────
        var main = new GameObject("Main");
        main.transform.SetParent(_gamePanel.transform, false);
        var mainRT = main.AddComponent<RectTransform>();
        mainRT.anchorMin = Vector2.zero; mainRT.anchorMax = new Vector2(0.87f, 1f);
        mainRT.offsetMin = mainRT.offsetMax = Vector2.zero;

        // Zone layout (bottom → top): hand 27%, my-CU 8%, my-lab 23%, divider 2%, op-lab 23%, op-CU 8%, top-bar 9%
        _zoneOpCU  = MakeZone("Opp.Creation",  main.transform, 0.91f, 1.00f, "Opponent Creation Units",  new Color(0.06f, 0.06f, 0.10f));
        _zoneOpLab = MakeZone("Opp.Lab",        main.transform, 0.60f, 0.91f, "Opponent Lab",             new Color(0.05f, 0.08f, 0.14f));

        var div = MakePanel("Divider", main.transform, new Color(0.35f, 0.35f, 0.40f));
        var divRT = div.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 0.585f); divRT.anchorMax = new Vector2(1, 0.600f);
        divRT.offsetMin = divRT.offsetMax = Vector2.zero;

        _zoneMyLab = MakeZone("My.Lab",         main.transform, 0.35f, 0.585f,"Your Lab",                 new Color(0.05f, 0.10f, 0.06f));
        _zoneMyCU  = MakeZone("My.Creation",    main.transform, 0.27f, 0.35f, "Your Creation Units",      new Color(0.06f, 0.06f, 0.10f));
        _zoneHand  = MakeZone("Hand",            main.transform, 0.00f, 0.27f, "Your Hand",               new Color(0.09f, 0.07f, 0.05f));
    }

    // ── Message / Log / Win overlays ──────────────────────────────────────────

    private void BuildMsgPanel()
    {
        _msgPanel = MakePanel("Msg", _board, new Color(0, 0, 0, 0.88f));
        Stretch(_msgPanel.GetComponent<RectTransform>());
        _msgPanel.SetActive(false);

        _msgLabel = MakeText("", _msgPanel.transform, 48, FontStyles.Bold);
        PlaceAnchored(_msgLabel, 0.15f, 0.48f, 0.85f, 0.78f);
        _msgLabel.alignment = TextAlignmentOptions.Center;
        _msgLabel.enableWordWrapping = true;

        _btnMsgOk = MakeBtn("CONTINUE", _msgPanel.transform, null, new Color(0.15f, 0.50f, 0.15f));
        PlaceAnchored(_btnMsgOk, 0.37f, 0.30f, 0.63f, 0.44f);
        _btnMsgOk.GetComponentInChildren<TextMeshProUGUI>().fontSize = 30;
    }

    private void BuildLogPanel()
    {
        _logPanel = MakePanel("Log", _board, new Color(0.04f, 0.04f, 0.09f, 0.96f));
        Stretch(_logPanel.GetComponent<RectTransform>());
        _logPanel.SetActive(false);

        var title = MakeText("BATTLE RESULTS", _logPanel.transform, 38, FontStyles.Bold);
        PlaceAnchored(title, 0.1f, 0.87f, 0.9f, 0.97f);
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.7f, 0.1f);

        // Scrollable log area
        var sv   = MakeScrollView(_logPanel.transform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.86f), out var content);
        _logLabel = MakeText("", content, 17);
        var llRT = _logLabel.GetComponent<RectTransform>();
        llRT.anchorMin = Vector2.zero; llRT.anchorMax = Vector2.one;
        llRT.offsetMin = llRT.offsetMax = Vector2.zero;
        _logLabel.alignment    = TextAlignmentOptions.TopLeft;
        _logLabel.enableWordWrapping = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _btnLogOk = MakeBtn("CONTINUE", _logPanel.transform, OnLogContinue, new Color(0.55f, 0.15f, 0.15f));
        PlaceAnchored(_btnLogOk, 0.37f, 0.04f, 0.63f, 0.15f);
        _btnLogOk.GetComponentInChildren<TextMeshProUGUI>().fontSize = 30;
    }

    private void BuildWinPanel()
    {
        _winPanel = MakePanel("Win", _board, new Color(0, 0, 0, 0.92f));
        Stretch(_winPanel.GetComponent<RectTransform>());
        _winPanel.SetActive(false);

        _winLabel = MakeText("", _winPanel.transform, 60, FontStyles.Bold);
        PlaceAnchored(_winLabel, 0.1f, 0.52f, 0.9f, 0.82f);
        _winLabel.alignment = TextAlignmentOptions.Center;
        _winLabel.color = new Color(1f, 0.82f, 0.1f);
        _winLabel.enableWordWrapping = true;

        _btnReplay = MakeBtn("PLAY AGAIN", _winPanel.transform, OnPlayAgain, new Color(0.18f, 0.36f, 0.65f));
        PlaceAnchored(_btnReplay, 0.35f, 0.30f, 0.65f, 0.44f);
        _btnReplay.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GAME FLOW
    // ══════════════════════════════════════════════════════════════════════════

    private void OnStartGame()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[PsiWars] GameManager.Instance is null"); return; }

        Debug.Log($"[PsiWars] Starting game. _gamePanel={((_gamePanel==null)?"NULL":"OK")} _p0HP={((_p0HP==null)?"NULL":"OK")} _btnNext={((_btnNext==null)?"NULL":"OK")}");

        gm.onGameStateChanged.AddListener(RefreshUI);
        gm.onPhaseChanged.AddListener(OnPhaseChanged);
        gm.onTurnChanged.AddListener(OnTurnChanged);
        gm.onGameOver.AddListener(OnGameOver);

        ShowPanel(_gamePanel);
        gm.StartGame(_selHP);
        RefreshUI();
    }

    private void OnPhaseChanged()
    {
        _bSub = BattleSub.None;
        _attackers.Clear(); _defenders.Clear();
        _pendingDef = null; _selHandCard = null; _selLabCard = null;
        RefreshUI();
    }

    private void OnTurnChanged()
    {
        ShowMsg($"Pass the device to\n{GameManager.Instance.CurrentPlayer.playerName}!", () =>
        {
            _flipper.onFlipComplete.RemoveAllListeners();
            _flipper.onFlipComplete.AddListener(RefreshUI);
            _flipper.Flip();
        });
    }

    private void OnGameOver()
    {
        var gm = GameManager.Instance;
        _winLabel.text = $"{gm.Players[gm.WinnerIndex].playerName} WINS!\nPlanetary domination established.";
        _winPanel.SetActive(true);
    }

    private void OnPlayAgain()
    {
        _winPanel.SetActive(false);
        var gm = GameManager.Instance;
        gm.onGameStateChanged.RemoveAllListeners();
        gm.onPhaseChanged.RemoveAllListeners();
        gm.onTurnChanged.RemoveAllListeners();
        gm.onGameOver.RemoveAllListeners();
        ShowPanel(_setupPanel);
    }

    // ── UI Refresh ────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (GameManager.Instance?.Players == null) return;
        if (_p0HP == null || _btnNext == null) return; // game panel not built yet
        UpdateHUD();
        RebuildZones();
        UpdateButtons();
    }

    private void UpdateHUD()
    {
        var gm = GameManager.Instance;
        var cp = gm.CurrentPlayer;
        _p0HP.text       = $"{gm.Players[0].playerName}: {gm.Players[0].labHP}/{gm.Players[0].maxHP} HP";
        _p1HP.text       = $"{gm.Players[1].playerName}: {gm.Players[1].labHP}/{gm.Players[1].maxHP} HP";
        _phaseLabel.text = PhaseStr(gm.CurrentPhase);
        _turnLabel.text  = $"{cp.playerName}  (Turn {gm.TurnNumber})";
        _deckLabel.text  = $"Battle: {cp.battleDeck.Count}\nResource: {cp.resourceDeck.Count}\nDiscard: {cp.discardPile.Count}";
        _statusLabel.text = StatusStr();
    }

    private string PhaseStr(TurnPhase p) => p switch
    {
        TurnPhase.Replenish => "① REPLENISH",
        TurnPhase.Draw      => "② DRAW",
        TurnPhase.Creation  => "③ CREATION",
        TurnPhase.Battle    => "④ BATTLE",
        TurnPhase.Cleanup   => "⑤ CLEANUP",
        _                   => ""
    };

    private string StatusStr()
    {
        var gm = GameManager.Instance;
        return gm.CurrentPhase switch
        {
            TurnPhase.Replenish => "All units replenished.",
            TurnPhase.Draw      => "Cards drawn — review hand, then Continue.",
            TurnPhase.Creation  => "Tap a hand card to select.\nTap Play Card to deploy it.\nTap a lab unit with equip selected\nto attach equipment.",
            TurnPhase.Battle    => _bSub switch
            {
                BattleSub.None        => "Tap Declare Attackers\nor Skip Battle.",
                BattleSub.DeclareAtk  => "Tap your lab units to\nmark attackers.\nThen Confirm Attackers.",
                BattleSub.DeclareDefend => "Tap one of your units,\nthen tap an attacker\nto assign as blocker.",
                _                     => ""
            },
            TurnPhase.Cleanup => "Battle complete. End your turn.",
            _                 => ""
        };
    }

    // ── Card zones ────────────────────────────────────────────────────────────

    private void RebuildZones()
    {
        foreach (var v in _views) if (v) Destroy(v.gameObject);
        _views.Clear();

        var gm = GameManager.Instance;
        var cp = gm.CurrentPlayer;
        var op = gm.OpponentPlayer;

        foreach (var ci in op.creationUnits) SpawnCard(ci, _zoneOpCU,  null);
        foreach (var ci in op.lab)           SpawnCard(ci, _zoneOpLab, OnOpponentLabClick);
        foreach (var ci in cp.lab)           SpawnCard(ci, _zoneMyLab, OnMyLabClick);
        foreach (var ci in cp.creationUnits) SpawnCard(ci, _zoneMyCU,  null);
        foreach (var ci in cp.hand)          SpawnCard(ci, _zoneHand,  OnHandClick);

        // Re-apply battle highlights
        if (gm.CurrentPhase == TurnPhase.Battle)
            foreach (var v in _views)
            {
                if (_attackers.Contains(v.CardInstance))      v.SetSelected(true);
                if (_defenders.ContainsValue(v.CardInstance)) v.SetSelected(true);
            }
    }

    private void SpawnCard(CardInstance ci, Transform parent, System.Action<CardView> onClick)
    {
        var go = new GameObject(ci.data.cardName);
        go.transform.SetParent(parent, false);

        // Root panel — colored by unit type
        var bg = go.AddComponent<Image>();
        bg.color = CardColor(ci.data);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 110; le.preferredHeight = 155;
        le.minWidth = 110;       le.minHeight = 155;

        // Artwork (top 60%)
        var artGO  = Sub("Art", go.transform);
        var artRT  = artGO.AddComponent<RectTransform>();
        artRT.anchorMin = new Vector2(0.02f, 0.36f); artRT.anchorMax = new Vector2(0.98f, 0.98f);
        artRT.offsetMin = artRT.offsetMax = Vector2.zero;
        var artImg = artGO.AddComponent<Image>();
        artImg.preserveAspect = true;
        if (ci.data.artwork != null) artImg.sprite = ci.data.artwork;
        else artImg.color = new Color(0, 0, 0, 0.25f);

        // Name bar
        var nameRT = SubText("Name", go.transform, ci.data.cardName, 10, FontStyles.Bold);
        nameRT.anchorMin = new Vector2(0, 0.22f); nameRT.anchorMax = new Vector2(1, 0.37f);
        nameRT.offsetMin = new Vector2(2, 0); nameRT.offsetMax = new Vector2(-2, 0);
        nameRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        nameRT.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Stats line
        string stats = StatsStr(ci);
        if (!string.IsNullOrEmpty(stats))
        {
            var sRT = SubText("Stats", go.transform, stats, 9);
            sRT.anchorMin = new Vector2(0, 0.10f); sRT.anchorMax = new Vector2(1, 0.23f);
            sRT.offsetMin = new Vector2(2, 0); sRT.offsetMax = new Vector2(-2, 0);
            sRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            sRT.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.9f, 0.3f);
        }

        // Cost (top-left)
        string cost = CostStr(ci.data);
        if (!string.IsNullOrEmpty(cost))
        {
            var cRT = SubText("Cost", go.transform, cost, 8);
            cRT.anchorMin = new Vector2(0, 0.88f); cRT.anchorMax = new Vector2(1, 1f);
            cRT.offsetMin = new Vector2(3, 0); cRT.offsetMax = new Vector2(-2, 0);
            cRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        }

        // Type strip (bottom)
        var tRT = SubText("Type", go.transform, TypeStr(ci.data), 8);
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = new Vector2(1, 0.11f);
        tRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        tRT.GetComponent<TextMeshProUGUI>().color = new Color(0.75f, 0.75f, 0.75f);

        // Equipment label
        if (ci.equippedItem != null)
        {
            var eqRT = SubText("EQ", go.transform, $"[{ci.equippedItem.data.cardName}]", 7);
            eqRT.anchorMin = new Vector2(0, 0f); eqRT.anchorMax = new Vector2(1, 0.12f);
            eqRT.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            eqRT.GetComponent<TextMeshProUGUI>().color = new Color(0.9f, 0.5f, 1f);
        }

        // Selected highlight (yellow border)
        var selGO  = Sub("Sel", go.transform);
        var selImg = selGO.AddComponent<Image>();
        selImg.color = new Color(1f, 1f, 0f, 0.75f);
        var selRT2 = selGO.GetComponent<RectTransform>();
        selRT2.anchorMin = Vector2.zero; selRT2.anchorMax = Vector2.one;
        selRT2.offsetMin = new Vector2(-4,-4); selRT2.offsetMax = new Vector2(4,4);
        selImg.raycastTarget = false;
        selGO.SetActive(false);
        selGO.transform.SetAsFirstSibling(); // Draw behind card content

        // Disoriented overlay (red tint)
        var dioGO  = Sub("Diso", go.transform);
        var dioImg = dioGO.AddComponent<Image>();
        dioImg.color = new Color(1f, 0.1f, 0.1f, 0.38f);
        var dioRT2 = dioGO.GetComponent<RectTransform>();
        dioRT2.anchorMin = Vector2.zero; dioRT2.anchorMax = Vector2.one;
        dioRT2.offsetMin = dioRT2.offsetMax = Vector2.zero;
        dioImg.raycastTarget = false;
        dioGO.SetActive(ci.isDisoriented);

        // CardView
        var cv = go.AddComponent<CardView>();
        cv.artworkImage      = artImg;
        cv.selectedHighlight = selGO;
        cv.disorientedOverlay = dioGO;
        cv.Init(ci, onClick ?? (_ => { }));
        _views.Add(cv);
    }

    // ── Click handlers ────────────────────────────────────────────────────────

    private void OnHandClick(CardView cv)
    {
        if (GameManager.Instance.CurrentPhase != TurnPhase.Creation) return;
        if (_selHandCard != null) _selHandCard.SetSelected(false);
        _selHandCard = (cv == _selHandCard) ? null : cv;
        _selHandCard?.SetSelected(true);
        UpdateButtons();
    }

    private void OnMyLabClick(CardView cv)
    {
        var gm = GameManager.Instance;
        var ci = cv.CardInstance;

        if (gm.CurrentPhase == TurnPhase.Creation)
        {
            if (_selHandCard != null && _selHandCard.CardInstance.data.cardType == CardType.Equipment)
            {
                bool ok = gm.TryAttachEquipment(_selHandCard.CardInstance, ci);
                _statusLabel.text = ok ? $"Equipped {_selHandCard.CardInstance.data.cardName}!" : "Cannot attach there.";
                _selHandCard?.SetSelected(false);
                _selHandCard = null;
                RebuildZones(); UpdateButtons();
                return;
            }
        }

        if (gm.CurrentPhase == TurnPhase.Battle)
        {
            if (_bSub == BattleSub.DeclareAtk && ci.data.cardType == CardType.BattleUnit)
            {
                if (_attackers.Contains(ci)) { _attackers.Remove(ci); cv.SetSelected(false); }
                else                         { _attackers.Add(ci);   cv.SetSelected(true); }
                UpdateButtons();
            }
            else if (_bSub == BattleSub.DeclareDefend)
            {
                _selLabCard?.SetSelected(false);
                _selLabCard = cv; cv.SetSelected(true);
                _pendingDef = ci;
                _statusLabel.text = "Now tap an attacker\nto block it.";
            }
        }
    }

    private void OnOpponentLabClick(CardView cv)
    {
        if (_bSub != BattleSub.DeclareDefend) return;
        var atk = cv.CardInstance;
        if (!_attackers.Contains(atk))  { _statusLabel.text = "That unit isn't attacking."; return; }
        if (_pendingDef == null) { _statusLabel.text = "Select one of your units first."; return; }

        _defenders[atk] = _pendingDef;
        cv.SetSelected(true);
        _selLabCard?.SetSelected(false);
        _selLabCard = null; _pendingDef = null;
        _statusLabel.text = "Assigned! Select another\nor Confirm Defense.";
        UpdateButtons();
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void UpdateButtons()
    {
        var gm    = GameManager.Instance;
        var phase = gm.CurrentPhase;

        // ─ Next Phase button ──────────────────────────────────────────────────
        _btnNext.onClick.RemoveAllListeners();
        bool showNext = phase != TurnPhase.Battle || _bSub == BattleSub.None;
        _btnNext.gameObject.SetActive(showNext);

        switch (phase)
        {
            case TurnPhase.Replenish:
            case TurnPhase.Draw:
                _btnNextLbl.text = "Continue →";
                _btnNext.onClick.AddListener(() => gm.AdvancePhase());
                break;
            case TurnPhase.Creation:
                _btnNextLbl.text = "Go to Battle →";
                _btnNext.onClick.AddListener(() => gm.AdvancePhase());
                break;
            case TurnPhase.Battle:
                _btnNextLbl.text = "Skip Battle →";
                _btnNext.onClick.AddListener(() => { _bSub = BattleSub.None; gm.AdvancePhase(); });
                break;
            case TurnPhase.Cleanup:
                _btnNextLbl.text = "End Turn →";
                _btnNext.onClick.AddListener(() => gm.AdvancePhase());
                break;
        }

        // ─ Action button ──────────────────────────────────────────────────────
        _btnAct.onClick.RemoveAllListeners();
        _btnAct.gameObject.SetActive(true);

        if (phase == TurnPhase.Creation)
        {
            _btnActLbl.text   = "Play Card";
            bool canPlay      = _selHandCard != null && gm.CurrentPlayer.CanAfford(_selHandCard.CardInstance.data);
            _btnAct.interactable = canPlay;
            _btnAct.onClick.AddListener(OnPlayCard);
        }
        else if (phase == TurnPhase.Battle)
        {
            switch (_bSub)
            {
                case BattleSub.None:
                    _btnActLbl.text      = "Declare Attackers";
                    _btnAct.interactable = gm.CurrentPlayer.lab.FindAll(c => c.data.cardType == CardType.BattleUnit).Count > 0;
                    _btnAct.onClick.AddListener(EnterDeclareAtk);
                    break;
                case BattleSub.DeclareAtk:
                    _btnActLbl.text      = "Confirm Attackers";
                    _btnAct.interactable = true;
                    _btnAct.onClick.AddListener(ConfirmAttackers);
                    break;
                case BattleSub.DeclareDefend:
                    _btnActLbl.text      = "Confirm Defense";
                    _btnAct.interactable = true;
                    _btnAct.onClick.AddListener(ConfirmDefenders);
                    break;
                default:
                    _btnAct.gameObject.SetActive(false);
                    break;
            }
        }
        else
        {
            _btnAct.gameObject.SetActive(false);
        }
    }

    // ── Game actions ──────────────────────────────────────────────────────────

    private void OnPlayCard()
    {
        if (_selHandCard == null) return;
        if (!GameManager.Instance.TryPlayCard(_selHandCard.CardInstance))
        {
            _statusLabel.text = "Not enough\nCreation Units!";
            return;
        }
        _selHandCard = null;
        RebuildZones(); UpdateButtons();
    }

    private void EnterDeclareAtk()
    {
        _bSub = BattleSub.DeclareAtk;
        _attackers.Clear();
        _btnNext.gameObject.SetActive(false);
        UpdateButtons();
        _statusLabel.text = "Tap lab units to\nmark as attackers.";
    }

    private void ConfirmAttackers()
    {
        if (_attackers.Count == 0) { _bSub = BattleSub.None; GameManager.Instance.AdvancePhase(); return; }

        _bSub = BattleSub.PassToDefend;
        ShowMsg($"Pass the device to\n{GameManager.Instance.OpponentPlayer.playerName}\nto assign defenders.", () =>
        {
            _flipper.onFlipComplete.RemoveAllListeners();
            _flipper.onFlipComplete.AddListener(() =>
            {
                _bSub = BattleSub.DeclareDefend;
                _defenders.Clear();
                RebuildZones(); UpdateButtons();
            });
            _flipper.Flip();
        });
    }

    private void ConfirmDefenders()
    {
        _bSub = BattleSub.Done;
        _btnNext.gameObject.SetActive(false);
        _btnAct.gameObject.SetActive(false);
        RunBattleResolution();
    }

    private void RunBattleResolution()
    {
        var results = new List<BattleManager.BattleResult>();
        var log     = new StringBuilder();
        var gm      = GameManager.Instance;

        foreach (var kv in _defenders)
        {
            var r = BattleManager.ResolveVs(kv.Key, kv.Value);
            results.Add(r);
            log.AppendLine($"⚔  {kv.Key.data.cardName}  vs  {kv.Value.data.cardName}");
            log.AppendLine(r.log);
            log.AppendLine();
        }

        foreach (var atk in _attackers)
        {
            if (_defenders.ContainsKey(atk)) continue;
            var r = BattleManager.ResolveUnblocked(atk, gm.OpponentPlayer);
            results.Add(r);
            log.AppendLine($"➤  {atk.data.cardName}  (UNBLOCKED)");
            log.AppendLine(r.log);
            log.AppendLine();
        }

        BattleManager.ApplyResults(results, gm.CurrentPlayer, gm.OpponentPlayer);
        ShowLog(log.ToString());
    }

    private void OnLogContinue()
    {
        _logPanel.SetActive(false);
        _bSub = BattleSub.None;
        _attackers.Clear(); _defenders.Clear();
        var gm = GameManager.Instance;
        if (!gm.CheckWinCondition())
            gm.AdvancePhase();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowPanel(GameObject panel)
    {
        if (_setupPanel != null) _setupPanel.SetActive(panel == _setupPanel);
        if (_gamePanel  != null) _gamePanel.SetActive(panel  == _gamePanel);
    }

    private void ShowMsg(string msg, System.Action onOk)
    {
        _msgLabel.text = msg;
        _msgPanel.SetActive(true);
        _btnMsgOk.onClick.RemoveAllListeners();
        _btnMsgOk.onClick.AddListener(() => { _msgPanel.SetActive(false); onOk?.Invoke(); });
    }

    private void ShowLog(string text)
    {
        _logLabel.text = text;
        _logPanel.SetActive(true);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI MakeText(string text, Transform parent, float size, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(text.Length > 20 ? "Text" : text);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.color = Color.white;
        go.AddComponent<LayoutElement>();
        return tmp;
    }

    private static Button MakeBtn(string label, Transform parent, System.Action onClick, Color bg)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = bg;
        var btn = go.AddComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        go.AddComponent<LayoutElement>();

        var tGO = new GameObject("Lbl");
        tGO.transform.SetParent(go.transform, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 20; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    private static void PlaceAnchored(Component c, float ax, float ay, float bx, float by)
    {
        var rt = c.GetComponent<RectTransform>();
        if (rt == null) rt = c.gameObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI AddSideText(GameObject parent, string text, float size, FontStyles style = FontStyles.Normal)
    {
        var tmp = MakeText(text, parent.transform, size, style);
        tmp.GetComponent<LayoutElement>().preferredHeight = size * 2.2f;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static void AddSpacer(GameObject parent, float height)
    {
        var s = new GameObject("Spacer").AddComponent<LayoutElement>();
        s.transform.SetParent(parent.transform, false);
        s.preferredHeight = height;
    }

    private Transform MakeZone(string name, Transform parent, float yMin, float yMax, string label, Color bg)
    {
        var container = new GameObject(name);
        container.transform.SetParent(parent, false);
        var rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, yMin); rt.anchorMax = new Vector2(1, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        container.AddComponent<Image>().color = bg;

        var lbl = MakeText(label, container.transform, 11);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.01f, 0f); lRT.anchorMax = new Vector2(0.3f, 0.35f);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        lbl.alignment = TextAlignmentOptions.BottomLeft;
        lbl.color = new Color(0.45f, 0.45f, 0.55f, 0.9f);

        // Horizontal scroll view
        var sv = new GameObject("Scroll"); sv.transform.SetParent(container.transform, false);
        var svRT = sv.AddComponent<RectTransform>();
        svRT.anchorMin = Vector2.zero; svRT.anchorMax = Vector2.one;
        svRT.offsetMin = new Vector2(2, 2); svRT.offsetMax = new Vector2(-2, -2);
        var sr = sv.AddComponent<ScrollRect>();
        sr.vertical = false; sr.horizontal = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var vp = new GameObject("Vp"); vp.transform.SetParent(sv.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        vp.AddComponent<Image>().color = Color.clear;
        sr.viewport = vpRT;

        var content = new GameObject("Content"); content.transform.SetParent(vp.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = new Vector2(0, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(6, 6, 6, 6);
        hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false; hlg.childForceExpandWidth = false;
        content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = cRT;

        return content.transform;
    }

    private static GameObject MakeScrollView(Transform parent, Vector2 ancMin, Vector2 ancMax, out Transform content)
    {
        var sv = new GameObject("SV"); sv.transform.SetParent(parent, false);
        var svRT = sv.AddComponent<RectTransform>();
        svRT.anchorMin = ancMin; svRT.anchorMax = ancMax;
        svRT.offsetMin = svRT.offsetMax = Vector2.zero;
        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false;

        var vp = new GameObject("Vp"); vp.transform.SetParent(sv.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        vp.AddComponent<Image>().color = Color.clear;
        sr.viewport = vpRT;

        var c = new GameObject("Content"); c.transform.SetParent(vp.transform, false);
        var cRT = c.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = Vector2.one;
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        c.AddComponent<ContentSizeFitter>();
        sr.content = cRT;
        content = c.transform;
        return sv;
    }

    // ── Card sub-element helpers ───────────────────────────────────────────────

    private static GameObject Sub(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static RectTransform SubText(string name, Transform parent, string text, float size, FontStyles style = FontStyles.Normal)
    {
        var go = Sub(name, parent);
        var rt = go.GetComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style; tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return rt;
    }

    // ── Card label helpers ────────────────────────────────────────────────────

    private static string StatsStr(CardInstance ci)
    {
        if (ci.data.cardType == CardType.BattleUnit)
            return $"C:{ci.CyberStrength}  P:{ci.PsionicStrength}  Ph:{ci.PhysicalStrength}";
        if (ci.data.cardType == CardType.Equipment)
            return $"+C:{ci.data.equipCyberBonus} +P:{ci.data.equipPsionicBonus} +Ph:{ci.data.equipPhysicalBonus}";
        return "";
    }

    private static string CostStr(CardData d)
    {
        var p = new List<string>();
        if (d.costDigitalSplicing   > 0) p.Add($"G:{d.costDigitalSplicing}");
        if (d.costNeurogenesis      > 0) p.Add($"B:{d.costNeurogenesis}");
        if (d.costBioAcceleration   > 0) p.Add($"R:{d.costBioAcceleration}");
        if (d.costMaterialAnimation > 0) p.Add($"M:{d.costMaterialAnimation}");
        if (d.costDestroy           > 0) p.Add($"W:{d.costDestroy}");
        return string.Join(" ", p);
    }

    private static string TypeStr(CardData d)
    {
        if (d.cardType == CardType.BattleUnit) return d.unitType.ToString().ToUpper();
        if (d.cardType == CardType.Equipment)  return "EQUIPMENT";
        if (d.cardType == CardType.Power)      return "POWER";
        return d.creationUnitType.ToString().ToUpper();
    }

    private static Color CardColor(CardData d)
    {
        if (d.cardType == CardType.CreationUnit)
            return d.creationUnitType switch
            {
                CreationUnitType.DigitalSplicing  => new Color(0.10f, 0.38f, 0.10f),
                CreationUnitType.Neurogenesis      => new Color(0.08f, 0.20f, 0.48f),
                CreationUnitType.BioAcceleration   => new Color(0.48f, 0.08f, 0.08f),
                CreationUnitType.MaterialAnimation => new Color(0.35f, 0.08f, 0.38f),
                _                                  => new Color(0.18f, 0.18f, 0.22f)
            };
        return d.unitType switch
        {
            UnitType.Creature => new Color(0.15f, 0.32f, 0.15f),
            UnitType.Robot    => new Color(0.12f, 0.22f, 0.42f),
            UnitType.Cyborg   => new Color(0.32f, 0.15f, 0.38f),
            _                 => d.cardType == CardType.Equipment
                                    ? new Color(0.38f, 0.20f, 0.32f)
                                    : new Color(0.18f, 0.18f, 0.22f)
        };
    }
}
