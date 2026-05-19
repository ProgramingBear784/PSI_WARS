using System.Collections;
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
    private Button          _btnNext, _btnAct, _btnViewHand;
    private TextMeshProUGUI _btnNextLbl, _btnActLbl, _btnViewHandLbl;

    // Card zones — Content transforms (for spawning cards into)
    private Transform _zoneOpCU, _zoneOpLab, _zoneMyLab, _zoneMyCU, _zoneHand;
    // Zone container RectTransforms (for phase-adaptive sizing)
    private RectTransform _zoneOpCUCon, _zoneOpLabCon, _zoneMyLabCon, _zoneMyCUCon, _zoneHandCon, _dividerRT;
    private bool _handExpanded;

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

        // ─ Sidebar (right 22%) ────────────────────────────────────────────────
        var sidebar = MakePanel("Sidebar", _gamePanel.transform, new Color(0.06f, 0.06f, 0.11f));
        var sideRT = sidebar.GetComponent<RectTransform>();
        sideRT.anchorMin = new Vector2(0.78f, 0f); sideRT.anchorMax = Vector2.one;
        sideRT.offsetMin = sideRT.offsetMax = Vector2.zero;
        var vlg = sidebar.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 16, 16);
        vlg.spacing = 6;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Phase — large and coloured
        _phaseLabel = AddSideText(sidebar, "Phase", 24, FontStyles.Bold);
        _phaseLabel.color = new Color(1f, 0.85f, 0.1f);
        _phaseLabel.alignment = TextAlignmentOptions.Center;
        _phaseLabel.GetComponent<LayoutElement>().preferredHeight = 60;

        // Turn
        _turnLabel = AddSideText(sidebar, "Turn", 14);
        _turnLabel.color = new Color(0.6f, 0.9f, 1f);
        _turnLabel.alignment = TextAlignmentOptions.Center;

        AddSpacer(sidebar, 6);

        // HP — each player on their own row
        _p0HP = AddSideText(sidebar, "P1  --/-- HP", 14);
        _p0HP.color = new Color(0.4f, 1f, 0.5f);
        _p1HP = AddSideText(sidebar, "P2  --/-- HP", 14);
        _p1HP.color = new Color(1f, 0.5f, 0.5f);

        AddSpacer(sidebar, 6);

        // Status instructions
        _statusLabel = AddSideText(sidebar, "", 12);
        _statusLabel.enableWordWrapping = true;
        _statusLabel.color = new Color(0.82f, 0.82f, 0.82f);
        _statusLabel.GetComponent<LayoutElement>().preferredHeight = 120;

        AddSpacer(sidebar, 10);

        // Action button (Play Card / Declare Attackers / etc.)
        _btnAct = MakeBtn("Action", sidebar.transform, null, new Color(0.18f, 0.45f, 0.75f));
        _btnAct.GetComponent<LayoutElement>().preferredHeight = 60;
        _btnActLbl = _btnAct.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        // Next phase button (Go to Battle / Skip Battle)
        _btnNext = MakeBtn("Next", sidebar.transform, null, new Color(0.15f, 0.15f, 0.22f));
        _btnNext.GetComponent<LayoutElement>().preferredHeight = 60;
        _btnNextLbl = _btnNext.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        // View Hand toggle — only shown during Battle phase
        _btnViewHand = MakeBtn("▲ View Hand", sidebar.transform, ToggleHandView,
                               new Color(0.28f, 0.20f, 0.08f));
        _btnViewHand.GetComponent<LayoutElement>().preferredHeight = 48;
        _btnViewHandLbl = _btnViewHand.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        _btnViewHand.gameObject.SetActive(false);

        AddSpacer(sidebar, 8);

        _deckLabel = AddSideText(sidebar, "", 12);
        _deckLabel.enableWordWrapping = true;
        _deckLabel.GetComponent<LayoutElement>().preferredHeight = 72;
        _deckLabel.color = new Color(0.50f, 0.50f, 0.62f);

        // ─ Main area (left 78%) ───────────────────────────────────────────────
        var main = new GameObject("Main");
        main.transform.SetParent(_gamePanel.transform, false);
        var mainRT = main.AddComponent<RectTransform>();
        mainRT.anchorMin = Vector2.zero; mainRT.anchorMax = new Vector2(0.78f, 1f);
        mainRT.offsetMin = mainRT.offsetMax = Vector2.zero;

        // Zones — no fixed anchors; ApplyPhaseLayout() positions them at runtime
        _zoneOpCUCon  = MakeZoneContainer("Opp.CU",  main.transform, "Opponent Creation Units",
                                           new Color(0.06f, 0.05f, 0.10f), out _zoneOpCU);
        _zoneOpLabCon = MakeZoneContainer("Opp.Lab", main.transform, "Opponent Lab",
                                           new Color(0.04f, 0.07f, 0.15f), out _zoneOpLab);

        var divGO = MakePanel("Divider", main.transform, new Color(0.38f, 0.38f, 0.44f));
        _dividerRT = divGO.GetComponent<RectTransform>();

        _zoneMyLabCon = MakeZoneContainer("My.Lab",  main.transform, "Your Lab",
                                           new Color(0.04f, 0.11f, 0.05f), out _zoneMyLab);
        _zoneMyCUCon  = MakeZoneContainer("My.CU",   main.transform, "Your Creation Units",
                                           new Color(0.07f, 0.06f, 0.12f), out _zoneMyCU);
        _zoneHandCon  = MakeZoneContainer("Hand",    main.transform, "Your Hand",
                                           new Color(0.10f, 0.08f, 0.05f), out _zoneHand);

        ApplyPhaseLayout(TurnPhase.Creation); // default layout until game starts

        _gamePanel.SetActive(false);
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
        _handExpanded = false;
        RefreshUI();
        StartCoroutine(AutoAdvanceIfReady());
    }

    private void OnTurnChanged()
    {
        ShowMsg($"Pass the device to\n{GameManager.Instance.CurrentPlayer.playerName}!", () =>
        {
            _flipper.onFlipComplete.RemoveAllListeners();
            _flipper.onFlipComplete.AddListener(() =>
            {
                RefreshUI();
                StartCoroutine(AutoAdvanceIfReady());
            });
            _flipper.Flip();
        });
    }

    private IEnumerator AutoAdvanceIfReady()
    {
        yield return new WaitForSeconds(0.4f);
        var gm = GameManager.Instance;
        if (gm == null || gm.Players == null) yield break;
        if (_msgPanel  != null && _msgPanel.activeSelf)  yield break;
        if (_winPanel  != null && _winPanel.activeSelf)  yield break;
        var phase = gm.CurrentPhase;
        if (phase == TurnPhase.Replenish || phase == TurnPhase.Draw || phase == TurnPhase.Cleanup)
            gm.AdvancePhase();
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
        ApplyPhaseLayout(GameManager.Instance.CurrentPhase);
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
            TurnPhase.Replenish => "Replenishing...",
            TurnPhase.Draw      => "Drawing cards...",
            TurnPhase.Creation  => "Play Card: CU (free) or Battle Unit.\nEquip: select equip in hand → tap lab unit.\nStack: tap a Cyborg in lab → tap a non-Cyborg.",
            TurnPhase.Battle    => _bSub switch
            {
                BattleSub.None        => "Tap Declare Attackers\nor Skip Battle.",
                BattleSub.DeclareAtk  => "Tap your lab units to\nmark attackers.\nThen Confirm Attackers.",
                BattleSub.DeclareDefend => "Tap YOUR unit (bottom)\nto select a blocker,\nthen tap the attacker (top)\nto assign it.",
                _                     => ""
            },
            TurnPhase.Cleanup => "Cleaning up...",
            _                 => ""
        };
    }

    // ── Card zones ────────────────────────────────────────────────────────────

    private void RebuildZones()
    {
        ClearZone(_zoneOpCU); ClearZone(_zoneOpLab);
        ClearZone(_zoneMyLab); ClearZone(_zoneMyCU); ClearZone(_zoneHand);
        _views.Clear();

        var gm = GameManager.Instance;
        var cp = gm.CurrentPlayer;
        var op = gm.OpponentPlayer;

        bool defending = _bSub == BattleSub.DeclareDefend;
        var bottom = defending ? op : cp;
        var top    = defending ? cp : op;

        // Opponent CU zone — group same-type CUs
        foreach (var grp in GroupByType(top.creationUnits).Values)    SpawnCUStack(grp, _zoneOpCU);

        // Opponent Lab — render stacks as a single combined card
        foreach (var ci in top.lab)
        {
            if (ci.stackedWith != null && ci.data.unitType != UnitType.Cyborg) continue; // rendered with Cyborg
            System.Action<CardView> h = defending ? OnAttackerLabClick : OnOpponentLabClick;
            if (ci.stackedWith != null) SpawnStackedUnit(ci, ci.stackedWith, _zoneOpLab, h);
            else                        SpawnCard(ci, _zoneOpLab, h);
        }

        // My Lab
        foreach (var ci in bottom.lab)
        {
            if (ci.stackedWith != null && ci.data.unitType != UnitType.Cyborg) continue;
            System.Action<CardView> h = defending ? OnDefenderLabClick : OnMyLabClick;
            if (ci.stackedWith != null) SpawnStackedUnit(ci, ci.stackedWith, _zoneMyLab, h);
            else                        SpawnCard(ci, _zoneMyLab, h);
        }

        // My CU zone — group same-type CUs
        foreach (var grp in GroupByType(bottom.creationUnits).Values) SpawnCUStack(grp, _zoneMyCU);

        // Hand — dim cards the player can't afford during Creation phase
        System.Action<CardView> handH = defending ? null : OnHandClick;
        bool creationPhase = gm.CurrentPhase == TurnPhase.Creation;
        foreach (var ci in bottom.hand)
        {
            bool affordable = ci.data.cardType == CardType.CreationUnit ||
                              bottom.CanAfford(ci.data);
            SpawnCard(ci, _zoneHand, handH, creationPhase && !affordable);
        }

        // Re-apply battle highlights
        if (gm.CurrentPhase == TurnPhase.Battle)
            foreach (var v in _views)
            {
                if (_attackers.Contains(v.CardInstance))      v.SetSelected(true);
                if (_defenders.ContainsKey(v.CardInstance))   v.SetSelected(true);
                if (_defenders.ContainsValue(v.CardInstance)) v.SetSelected(true);
            }
    }

    private void ClearZone(Transform zone)
    {
        if (zone == null) return;
        for (int i = zone.childCount - 1; i >= 0; i--)
            Destroy(zone.GetChild(i).gameObject);
    }

    private static Dictionary<CreationUnitType, List<CardInstance>> GroupByType(List<CardInstance> cus)
    {
        var d = new Dictionary<CreationUnitType, List<CardInstance>>();
        foreach (var ci in cus)
        {
            if (!d.ContainsKey(ci.data.creationUnitType)) d[ci.data.creationUnitType] = new List<CardInstance>();
            d[ci.data.creationUnitType].Add(ci);
        }
        return d;
    }

    private void SpawnCard(CardInstance ci, Transform parent, System.Action<CardView> onClick, bool dimmed = false)
    {
        var go = new GameObject(ci.data.cardName);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var bg = go.AddComponent<Image>();
        bg.color = CardColor(ci.data);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 130; le.preferredHeight = 170;
        le.minWidth = 110;       le.minHeight = 140;

        bool isCU    = ci.data.cardType == CardType.CreationUnit;
        string stats = isCU ? "" : StatsStr(ci);
        string cost  = isCU ? "" : CostStr(ci.data);
        bool hasStat   = !string.IsNullOrEmpty(stats);
        bool hasCost   = !string.IsNullOrEmpty(cost);
        bool hasBottom = hasCost || ci.equippedItem != null;

        // ── Type band (top 20%, always visible when zone is thin) ─────────────
        var bandGO = new GameObject("Band"); bandGO.transform.SetParent(go.transform, false);
        var bandRT = bandGO.AddComponent<RectTransform>();
        bandRT.anchorMin = new Vector2(0, 0.80f); bandRT.anchorMax = Vector2.one;
        bandRT.offsetMin = bandRT.offsetMax = Vector2.zero;
        bandGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.50f);

        var typeGO = new GameObject("Type"); typeGO.transform.SetParent(bandGO.transform, false);
        var typeRT = typeGO.AddComponent<RectTransform>();
        typeRT.anchorMin = Vector2.zero; typeRT.anchorMax = Vector2.one;
        typeRT.offsetMin = new Vector2(3, 1); typeRT.offsetMax = new Vector2(-3, -1);
        var typeTMP = typeGO.AddComponent<TextMeshProUGUI>();
        typeTMP.text = TypeStr(ci.data); typeTMP.fontSize = 10; typeTMP.fontStyle = FontStyles.Bold;
        typeTMP.color = new Color(1f, 0.88f, 0.4f);
        typeTMP.alignment = TextAlignmentOptions.Center;
        typeTMP.enableWordWrapping = true;

        // ── Card name (largest text, adapts to available space) ───────────────
        float nameYMin = isCU ? 0.03f : (hasStat ? 0.45f : (hasBottom ? 0.22f : 0.03f));
        var nameGO = new GameObject("Name"); nameGO.transform.SetParent(go.transform, false);
        var nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, nameYMin); nameRT.anchorMax = new Vector2(1, 0.80f);
        nameRT.offsetMin = new Vector2(4, 2); nameRT.offsetMax = new Vector2(-4, -2);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = ci.data.cardName; nameTMP.fontSize = 16; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.enableWordWrapping = true;

        // ── Stats row (battle units + equipment) ──────────────────────────────
        if (hasStat)
        {
            float statYMin = hasBottom ? 0.22f : 0.03f;
            var sGO = new GameObject("Stats"); sGO.transform.SetParent(go.transform, false);
            var sRT = sGO.AddComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, statYMin); sRT.anchorMax = new Vector2(1, 0.45f);
            sRT.offsetMin = new Vector2(4, 0); sRT.offsetMax = new Vector2(-4, 0);
            var sTMP = sGO.AddComponent<TextMeshProUGUI>();
            sTMP.text = stats; sTMP.fontSize = 13;
            sTMP.color = new Color(1f, 0.90f, 0.25f);
            sTMP.alignment = TextAlignmentOptions.Center;
            sTMP.enableWordWrapping = true;
        }

        // ── Cost row (skipped when item is equipped — equipment label takes this slot) ───
        if (hasCost && ci.equippedItem == null)
        {
            var cGO = new GameObject("Cost"); cGO.transform.SetParent(go.transform, false);
            var cRT = cGO.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0.03f); cRT.anchorMax = new Vector2(1, 0.22f);
            cRT.offsetMin = new Vector2(4, 0); cRT.offsetMax = new Vector2(-4, 0);
            var cTMP = cGO.AddComponent<TextMeshProUGUI>();
            cTMP.text = cost; cTMP.fontSize = 11;
            cTMP.color = new Color(0.5f, 0.92f, 1f);
            cTMP.alignment = TextAlignmentOptions.Center;
            cTMP.enableWordWrapping = true;
        }

        // ── Equipped item (replaces cost slot, shows bonus explicitly) ───────────
        if (ci.equippedItem != null)
        {
            var eq = ci.equippedItem.data;
            var bonusParts = new System.Collections.Generic.List<string>();
            if (eq.equipCyberBonus    > 0) bonusParts.Add($"+{eq.equipCyberBonus} Cy");
            if (eq.equipPsionicBonus  > 0) bonusParts.Add($"+{eq.equipPsionicBonus} Ps");
            if (eq.equipPhysicalBonus > 0) bonusParts.Add($"+{eq.equipPhysicalBonus} Ph");
            string bonus = bonusParts.Count > 0 ? string.Join(" ", bonusParts) : "";

            var eGO = new GameObject("EQ"); eGO.transform.SetParent(go.transform, false);
            var eRT = eGO.AddComponent<RectTransform>();
            eRT.anchorMin = new Vector2(0, 0.03f); eRT.anchorMax = new Vector2(1, 0.22f);
            eRT.offsetMin = new Vector2(3, 2); eRT.offsetMax = new Vector2(-3, 0);
            var eTMP = eGO.AddComponent<TextMeshProUGUI>();
            eTMP.text = bonus.Length > 0 ? $"{eq.cardName}\n{bonus}" : eq.cardName;
            eTMP.fontSize = 9;
            eTMP.color = new Color(0.9f, 0.5f, 1f);
            eTMP.alignment = TextAlignmentOptions.Center;
            eTMP.enableWordWrapping = true;
        }

        // ── Selection highlight ───────────────────────────────────────────────
        var selGO = new GameObject("Sel"); selGO.transform.SetParent(go.transform, false);
        selGO.transform.SetAsFirstSibling();
        var selRT = selGO.AddComponent<RectTransform>();
        selRT.anchorMin = Vector2.zero; selRT.anchorMax = Vector2.one;
        selRT.offsetMin = new Vector2(-4, -4); selRT.offsetMax = new Vector2(4, 4);
        var selImg = selGO.AddComponent<Image>();
        selImg.color = new Color(1f, 1f, 0f, 0.80f); selImg.raycastTarget = false;
        selGO.SetActive(false);

        // ── Disoriented overlay ───────────────────────────────────────────────
        var dioGO = new GameObject("Diso"); dioGO.transform.SetParent(go.transform, false);
        var dioRT = dioGO.AddComponent<RectTransform>();
        dioRT.anchorMin = Vector2.zero; dioRT.anchorMax = Vector2.one;
        dioRT.offsetMin = dioRT.offsetMax = Vector2.zero;
        var dioImg = dioGO.AddComponent<Image>();
        dioImg.color = new Color(1f, 0.1f, 0.1f, 0.40f); dioImg.raycastTarget = false;
        dioGO.SetActive(ci.isDisoriented);

        // ── Depleted overlay ──────────────────────────────────────────────────
        var depGO = new GameObject("Dep"); depGO.transform.SetParent(go.transform, false);
        var depRT = depGO.AddComponent<RectTransform>();
        depRT.anchorMin = Vector2.zero; depRT.anchorMax = Vector2.one;
        depRT.offsetMin = depRT.offsetMax = Vector2.zero;
        var depImg = depGO.AddComponent<Image>();
        depImg.color = new Color(0.05f, 0.05f, 0.05f, 0.68f); depImg.raycastTarget = false;
        depGO.SetActive(ci.isDepleted);

        // Unaffordable overlay — covers card with semi-transparent gray + X mark
        if (dimmed)
        {
            var dimGO = new GameObject("Dim"); dimGO.transform.SetParent(go.transform, false);
            var dimRT = dimGO.AddComponent<RectTransform>();
            dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
            dimRT.offsetMin = dimRT.offsetMax = Vector2.zero;
            var dimImg = dimGO.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.60f); dimImg.raycastTarget = false;
        }

        var cv = go.AddComponent<CardView>();
        cv.artworkImage       = null;  // bg color is fixed; depletion handled by depGO overlay
        cv.selectedHighlight  = selGO;
        cv.disorientedOverlay = dioGO;
        cv.depletedOverlay    = depGO;
        cv.Init(ci, dimmed ? (_ => { }) : (onClick ?? (_ => { })));
        _views.Add(cv);
    }

    // ── Click handlers ────────────────────────────────────────────────────────

    private void OnHandClick(CardView cv)
    {
        if (GameManager.Instance.CurrentPhase != TurnPhase.Creation) return;
        // Clicking hand deselects any lab selection
        _selLabCard?.SetSelected(false); _selLabCard = null;
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
            // Equipment in hand: tap a lab unit to equip it directly
            if (_selHandCard != null && _selHandCard.CardInstance.data.cardType == CardType.Equipment)
            {
                bool ok = gm.TryEquipFromHand(_selHandCard.CardInstance, ci);
                _statusLabel.text = ok ? $"Equipped {_selHandCard.CardInstance.data.cardName} on {ci.data.cardName}!"
                                       : "Can't equip there — check compatibility and cost.";
                _selHandCard?.SetSelected(false); _selHandCard = null;
                if (ok) RebuildZones();
                UpdateButtons();
                return;
            }

            // Battle unit in lab: selection / stacking logic (only when no hand card active)
            if (ci.data.cardType == CardType.BattleUnit && _selHandCard == null)
            {
                // A Cyborg is already selected — try to stack with this unit
                if (_selLabCard != null && _selLabCard.CardInstance != ci &&
                    _selLabCard.CardInstance.stackedWith == null)
                {
                    bool ok = gm.TryStackUnits(_selLabCard.CardInstance, ci);
                    _selLabCard.SetSelected(false); _selLabCard = null;
                    if (ok) RebuildZones();
                    else    _statusLabel.text = "Stack requires a Cyborg and a non-Cyborg.";
                    UpdateButtons();
                    return;
                }

                // Toggle selection (Cyborg → enters stack-mode; stacked → shows unstack; others → deselect)
                bool wasSelected = _selLabCard?.CardInstance == ci;
                _selLabCard?.SetSelected(false); _selLabCard = null;
                if (!wasSelected)
                {
                    _selLabCard = cv; cv.SetSelected(true);
                    if (ci.stackedWith != null)
                        _statusLabel.text = $"{ci.data.cardName} stack selected.\nPress Unstack to separate.";
                    else if (ci.data.unitType == UnitType.Cyborg)
                        _statusLabel.text = "Cyborg selected.\nTap a Creature or Robot in lab to stack.";
                    else
                        _statusLabel.text = $"{ci.data.cardName} selected.";
                }
                UpdateButtons();
                return;
            }
        }

        if (gm.CurrentPhase == TurnPhase.Battle &&
            _bSub == BattleSub.DeclareAtk &&
            ci.data.cardType == CardType.BattleUnit)
        {
            // Cyborg in stack-mode: try to pair with this unit
            if (_selLabCard != null && _selLabCard.CardInstance != ci)
            {
                bool ok = gm.TryStackUnits(_selLabCard.CardInstance, ci);
                _selLabCard.SetSelected(false); _selLabCard = null;
                if (ok) { _statusLabel.text = "Units stacked!"; RebuildZones(); }
                else      _statusLabel.text = "Stack requires a Cyborg + non-Cyborg.";
                UpdateButtons(); return;
            }
            // Tap same card while selected → cancel stack mode
            if (_selLabCard?.CardInstance == ci)
            {
                _selLabCard.SetSelected(false); _selLabCard = null;
                UpdateButtons(); return;
            }
            // Un-stacked Cyborg → enter stack mode instead of toggling attacker
            if (ci.data.unitType == UnitType.Cyborg && ci.stackedWith == null)
            {
                _selLabCard?.SetSelected(false);
                _selLabCard = cv; cv.SetSelected(true);
                _statusLabel.text = "Cyborg selected.\nTap another lab unit to stack.";
                UpdateButtons(); return;
            }
            // Stacked or non-Cyborg: toggle as attacker normally
            if (_attackers.Contains(ci)) { _attackers.Remove(ci); cv.SetSelected(false); }
            else                         { _attackers.Add(ci);    cv.SetSelected(true); }
            UpdateButtons();
        }
    }

    // No-op during normal attacker turn — kept to satisfy spawn signature
    private void OnOpponentLabClick(CardView cv) { }

    // Defender (bottom after perspective swap) selects their own unit to block with
    private void OnDefenderLabClick(CardView cv)
    {
        var ci = cv.CardInstance;
        var gm = GameManager.Instance;

        // Cyborg in stack mode → try to pair with this unit
        if (_selLabCard != null && _pendingDef == null && _selLabCard.CardInstance != ci)
        {
            bool ok = gm.TryStackUnits(_selLabCard.CardInstance, ci);
            _selLabCard.SetSelected(false); _selLabCard = null;
            if (ok) { _statusLabel.text = "Units stacked!"; RebuildZones(); }
            else      _statusLabel.text = "Stack requires a Cyborg + non-Cyborg.";
            UpdateButtons(); return;
        }
        // Tap same card in stack mode → cancel
        if (_selLabCard?.CardInstance == ci && _pendingDef == null)
        {
            _selLabCard.SetSelected(false); _selLabCard = null;
            UpdateButtons(); return;
        }
        // Un-stacked Cyborg → enter stack mode
        if (ci.data.unitType == UnitType.Cyborg && ci.stackedWith == null)
        {
            _selLabCard?.SetSelected(false); _pendingDef = null;
            _selLabCard = cv; cv.SetSelected(true);
            _statusLabel.text = "Cyborg selected.\nTap another unit to stack.";
            UpdateButtons(); return;
        }
        // Normal blocker assignment
        _selLabCard?.SetSelected(false); _selLabCard = null;
        _selLabCard = cv; cv.SetSelected(true);
        _pendingDef = ci;
        _statusLabel.text = "Now tap one of the\nattacking units at the top.";
    }

    // Defender taps an attacker (top after perspective swap) to assign the pending blocker
    private void OnAttackerLabClick(CardView cv)
    {
        var atk = cv.CardInstance;
        if (!_attackers.Contains(atk)) { _statusLabel.text = "That unit isn't attacking."; return; }
        if (_pendingDef == null) { _statusLabel.text = "Select one of your units first."; return; }

        _defenders[atk] = _pendingDef;
        cv.SetSelected(true);
        _selLabCard?.SetSelected(false);
        _selLabCard = null; _pendingDef = null;
        _statusLabel.text = "Blocker assigned!\nSelect another or Confirm.";
        UpdateButtons();
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void UpdateButtons()
    {
        var gm    = GameManager.Instance;
        var phase = gm.CurrentPhase;

        // ─ Next Phase button ──────────────────────────────────────────────────
        _btnNext.onClick.RemoveAllListeners();
        // Replenish / Draw / Cleanup auto-advance — hide the button entirely
        bool showNext = phase == TurnPhase.Creation ||
                       (phase == TurnPhase.Battle && _bSub == BattleSub.None);
        _btnNext.gameObject.SetActive(showNext);

        switch (phase)
        {
            case TurnPhase.Creation:
                if (_btnNextLbl) _btnNextLbl.text = "Go to Battle →";
                _btnNext.onClick.AddListener(() => gm.AdvancePhase());
                break;
            case TurnPhase.Battle:
                if (_btnNextLbl) _btnNextLbl.text = "Skip Battle →";
                _btnNext.onClick.AddListener(() => { _bSub = BattleSub.None; gm.AdvancePhase(); });
                break;
            // Replenish, Draw, Cleanup: auto-advance via coroutine — no button needed
        }

        // ─ Action button ──────────────────────────────────────────────────────
        _btnAct.onClick.RemoveAllListeners();
        _btnAct.gameObject.SetActive(true);

        if (phase == TurnPhase.Creation)
        {
            // Lab card selected: stack/unstack mode takes priority
            if (_selLabCard != null)
            {
                if (_selLabCard.CardInstance.stackedWith != null)
                {
                    if (_btnActLbl) _btnActLbl.text = "Unstack";
                    _btnAct.interactable = true;
                    _btnAct.onClick.AddListener(() =>
                    {
                        gm.UnstackUnit(_selLabCard.CardInstance);
                        _selLabCard?.SetSelected(false); _selLabCard = null;
                        RebuildZones(); UpdateButtons();
                    });
                }
                else // Cyborg in "pick partner" mode
                {
                    if (_btnActLbl) _btnActLbl.text = "Cancel Stack";
                    _btnAct.interactable = true;
                    _btnAct.onClick.AddListener(() =>
                    {
                        _selLabCard?.SetSelected(false); _selLabCard = null;
                        UpdateButtons();
                    });
                }
            }
            else if (_selHandCard != null && _selHandCard.CardInstance.data.cardType == CardType.Equipment)
            {
                // Equipment selected — must be applied by clicking a lab unit
                if (_btnActLbl) _btnActLbl.text = "Tap a lab unit to equip";
                _btnAct.interactable = false;
                _btnAct.onClick.AddListener(OnPlayCard); // no-op placeholder
            }
            else
            {
                if (_btnActLbl) _btnActLbl.text = "Play Card";
                bool canPlay = _selHandCard != null &&
                    (_selHandCard.CardInstance.data.cardType == CardType.CreationUnit ||
                     gm.CurrentPlayer.CanAfford(_selHandCard.CardInstance.data));
                _btnAct.interactable = canPlay;
                _btnAct.onClick.AddListener(OnPlayCard);
            }
        }
        else if (phase == TurnPhase.Battle)
        {
            switch (_bSub)
            {
                case BattleSub.None:
                    if (_btnActLbl) _btnActLbl.text = "Declare Attackers";
                    _btnAct.interactable = gm.CurrentPlayer.lab.FindAll(c => c.data.cardType == CardType.BattleUnit).Count > 0;
                    _btnAct.onClick.AddListener(EnterDeclareAtk);
                    break;
                case BattleSub.DeclareAtk:
                    if (_selLabCard != null)
                    {
                        // A Cyborg is selected for stacking — show stack controls
                        if (_selLabCard.CardInstance.stackedWith != null)
                        {
                            if (_btnActLbl) _btnActLbl.text = "Unstack";
                            _btnAct.interactable = true;
                            _btnAct.onClick.AddListener(() => {
                                gm.UnstackUnit(_selLabCard.CardInstance);
                                _selLabCard?.SetSelected(false); _selLabCard = null;
                                RebuildZones(); UpdateButtons();
                            });
                        }
                        else
                        {
                            if (_btnActLbl) _btnActLbl.text = "Cancel Stack";
                            _btnAct.interactable = true;
                            _btnAct.onClick.AddListener(() => {
                                _selLabCard?.SetSelected(false); _selLabCard = null;
                                UpdateButtons();
                            });
                        }
                    }
                    else
                    {
                        if (_btnActLbl) _btnActLbl.text = "Confirm Attackers";
                        _btnAct.interactable = true;
                        _btnAct.onClick.AddListener(ConfirmAttackers);
                    }
                    break;
                case BattleSub.DeclareDefend:
                    if (_selLabCard != null && _pendingDef == null)
                    {
                        // A Cyborg is selected for stacking — show stack controls
                        if (_selLabCard.CardInstance.stackedWith != null)
                        {
                            if (_btnActLbl) _btnActLbl.text = "Unstack";
                            _btnAct.interactable = true;
                            _btnAct.onClick.AddListener(() => {
                                gm.UnstackUnit(_selLabCard.CardInstance);
                                _selLabCard?.SetSelected(false); _selLabCard = null;
                                RebuildZones(); UpdateButtons();
                            });
                        }
                        else
                        {
                            if (_btnActLbl) _btnActLbl.text = "Cancel Stack";
                            _btnAct.interactable = true;
                            _btnAct.onClick.AddListener(() => {
                                _selLabCard?.SetSelected(false); _selLabCard = null;
                                UpdateButtons();
                            });
                        }
                    }
                    else
                    {
                        if (_btnActLbl) _btnActLbl.text = "Confirm Defense";
                        _btnAct.interactable = true;
                        _btnAct.onClick.AddListener(ConfirmDefenders);
                    }
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

        // ─ View Hand button (Battle phase only) ───────────────────────────────
        if (_btnViewHand != null)
        {
            bool showViewHand = phase == TurnPhase.Battle;
            _btnViewHand.gameObject.SetActive(showViewHand);
            if (showViewHand && _btnViewHandLbl != null)
                _btnViewHandLbl.text = _handExpanded ? "▼ Hide Hand" : "▲ View Hand";
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
                _selLabCard = null; _pendingDef = null;
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

    // Builds a zone container WITHOUT positioning (anchors set by ApplyPhaseLayout).
    // Cards have a fixed preferred height; zones that are smaller clip them (peek effect).
    private RectTransform MakeZoneContainer(string name, Transform parent, string label, Color bg, out Transform content)
    {
        var container = new GameObject(name);
        container.transform.SetParent(parent, false);
        var rt = container.AddComponent<RectTransform>();  // positioned later
        container.AddComponent<Image>().color = bg;

        // Zone label (bottom-left watermark)
        var lbl = MakeText(label, container.transform, 10);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0.01f, 0f); lRT.anchorMax = new Vector2(0.55f, 0.38f);
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
        lbl.alignment = TextAlignmentOptions.BottomLeft;
        lbl.color = new Color(0.42f, 0.42f, 0.54f, 0.85f);

        // Horizontally scrollable card row
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
        vp.AddComponent<RectMask2D>(); // rect-based clipping — no stencil needed
        sr.viewport = vpRT;

        var c = new GameObject("Content"); c.transform.SetParent(vp.transform, false);
        var cRT = c.AddComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero; cRT.anchorMax = new Vector2(0, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var hlg = c.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        // childControlHeight=true + childForceExpandHeight=false → cards use preferredHeight,
        // but zone clips them (RectMask2D) when the zone is shorter — gives a "peek" effect
        hlg.childControlHeight = true;  hlg.childForceExpandHeight = false;
        hlg.childControlWidth  = false; hlg.childForceExpandWidth  = false;
        c.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = cRT;

        content = c.transform;
        return rt;
    }

    // ── Phase-adaptive layout ─────────────────────────────────────────────────

    private void ApplyPhaseLayout(TurnPhase phase)
    {
        if (_zoneHandCon == null) return; // panel not built yet

        bool isBattle = phase == TurnPhase.Battle;

        if (isBattle && _handExpanded)
        {
            // Battle – hand expanded (slides up, overlaps labs)
            // Opponent area stays compressed; hand gets the bottom 45%
            SetZoneAnchors(_zoneOpCUCon,  0.93f, 1.00f);
            SetZoneAnchors(_zoneOpLabCon, 0.80f, 0.93f);
            SetZoneAnchors(_dividerRT,    0.78f, 0.80f);
            SetZoneAnchors(_zoneMyLabCon, 0.65f, 0.78f);
            SetZoneAnchors(_zoneMyCUCon,  0.58f, 0.65f);
            SetZoneAnchors(_zoneHandCon,  0.00f, 0.58f); // big — hand card tops visible
            _zoneHandCon.SetAsLastSibling();              // render on top of labs
        }
        else if (isBattle)
        {
            // Battle – standard: both labs large, hand collapsed to a peek strip
            SetZoneAnchors(_zoneOpCUCon,  0.93f, 1.00f);
            SetZoneAnchors(_zoneOpLabCon, 0.52f, 0.93f);
            SetZoneAnchors(_dividerRT,    0.50f, 0.52f);
            SetZoneAnchors(_zoneMyLabCon, 0.09f, 0.50f);
            SetZoneAnchors(_zoneMyCUCon,  0.04f, 0.09f);
            SetZoneAnchors(_zoneHandCon,  0.00f, 0.04f); // thin strip — card tops peeking
        }
        else
        {
            // Creation / Replenish / Draw: big hand, visible CUs, small opponent strip
            SetZoneAnchors(_zoneOpCUCon,  0.91f, 1.00f);
            SetZoneAnchors(_zoneOpLabCon, 0.76f, 0.91f);
            SetZoneAnchors(_dividerRT,    0.74f, 0.76f);
            SetZoneAnchors(_zoneMyLabCon, 0.57f, 0.74f);
            SetZoneAnchors(_zoneMyCUCon,  0.41f, 0.57f);
            SetZoneAnchors(_zoneHandCon,  0.00f, 0.41f); // 41% — full cards visible
        }
    }

    private static void SetZoneAnchors(RectTransform rt, float yMin, float yMax)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0, yMin);
        rt.anchorMax = new Vector2(1, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void ToggleHandView()
    {
        _handExpanded = !_handExpanded;
        if (_btnViewHandLbl) _btnViewHandLbl.text = _handExpanded ? "▼ Hide Hand" : "▲ View Hand";
        ApplyPhaseLayout(GameManager.Instance.CurrentPhase);
    }

    // ── Stacked-unit card (Cyborg on top half, partner on bottom half) ──────────

    private void SpawnStackedUnit(CardInstance cyborg, CardInstance partner, Transform parent,
                                  System.Action<CardView> onClick)
    {
        var go = new GameObject($"[{cyborg.data.cardName}+{partner.data.cardName}]");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 130; le.preferredHeight = 170;
        le.minWidth = 110; le.minHeight = 140;

        // Split background: Cyborg color top 62%, partner color bottom 38%
        var bgTop = new GameObject("BgT"); bgTop.transform.SetParent(go.transform, false);
        var bgTopRT = bgTop.AddComponent<RectTransform>();
        bgTopRT.anchorMin = new Vector2(0, 0.38f); bgTopRT.anchorMax = Vector2.one;
        bgTopRT.offsetMin = bgTopRT.offsetMax = Vector2.zero;
        bgTop.AddComponent<Image>().color = CardColor(cyborg.data);

        var bgBot = new GameObject("BgB"); bgBot.transform.SetParent(go.transform, false);
        var bgBotRT = bgBot.AddComponent<RectTransform>();
        bgBotRT.anchorMin = Vector2.zero; bgBotRT.anchorMax = new Vector2(1, 0.38f);
        bgBotRT.offsetMin = bgBotRT.offsetMax = Vector2.zero;
        bgBot.AddComponent<Image>().color = CardColor(partner.data);

        // Type band: "CYBORG STACK"
        var bandGO = new GameObject("Band"); bandGO.transform.SetParent(go.transform, false);
        var bandRT = bandGO.AddComponent<RectTransform>();
        bandRT.anchorMin = new Vector2(0, 0.82f); bandRT.anchorMax = Vector2.one;
        bandRT.offsetMin = bandRT.offsetMax = Vector2.zero;
        bandGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.50f);
        var tGO = new GameObject("T"); tGO.transform.SetParent(bandGO.transform, false);
        var tRT = tGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(3, 1); tRT.offsetMax = new Vector2(-3, -1);
        var tTMP = tGO.AddComponent<TextMeshProUGUI>();
        tTMP.text = "CYBORG STACK"; tTMP.fontSize = 9; tTMP.fontStyle = FontStyles.Bold;
        tTMP.color = new Color(1f, 0.88f, 0.4f); tTMP.alignment = TextAlignmentOptions.Center;

        // Cyborg name: 0.62–0.82
        var cnGO = new GameObject("CyName"); cnGO.transform.SetParent(go.transform, false);
        var cnRT = cnGO.AddComponent<RectTransform>();
        cnRT.anchorMin = new Vector2(0, 0.62f); cnRT.anchorMax = new Vector2(1, 0.82f);
        cnRT.offsetMin = new Vector2(4, 2); cnRT.offsetMax = new Vector2(-4, -2);
        var cnTMP = cnGO.AddComponent<TextMeshProUGUI>();
        cnTMP.text = cyborg.data.cardName; cnTMP.fontSize = 13; cnTMP.fontStyle = FontStyles.Bold;
        cnTMP.color = Color.white; cnTMP.alignment = TextAlignmentOptions.Center;
        cnTMP.enableWordWrapping = true;

        // Combined stats: 0.42–0.62
        var stGO = new GameObject("Stats"); stGO.transform.SetParent(go.transform, false);
        var stRT = stGO.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0, 0.42f); stRT.anchorMax = new Vector2(1, 0.62f);
        stRT.offsetMin = new Vector2(4, 0); stRT.offsetMax = new Vector2(-4, 0);
        var stTMP = stGO.AddComponent<TextMeshProUGUI>();
        stTMP.text = $"Cy {cyborg.CyberStrength}   Ps {cyborg.PsionicStrength}   Ph {cyborg.PhysicalStrength}";
        stTMP.fontSize = 12; stTMP.color = new Color(1f, 0.90f, 0.25f);
        stTMP.alignment = TextAlignmentOptions.Center;

        // Thin white divider line at 0.40
        var divGO = new GameObject("Div"); divGO.transform.SetParent(go.transform, false);
        var divRT = divGO.AddComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.04f, 0.395f); divRT.anchorMax = new Vector2(0.96f, 0.410f);
        divRT.offsetMin = divRT.offsetMax = Vector2.zero;
        divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.45f);

        // Partner type: 0.25–0.39
        var ptGO = new GameObject("PType"); ptGO.transform.SetParent(go.transform, false);
        var ptRT = ptGO.AddComponent<RectTransform>();
        ptRT.anchorMin = new Vector2(0, 0.25f); ptRT.anchorMax = new Vector2(1, 0.39f);
        ptRT.offsetMin = new Vector2(4, 0); ptRT.offsetMax = new Vector2(-4, 0);
        var ptTMP = ptGO.AddComponent<TextMeshProUGUI>();
        ptTMP.text = $"+ {partner.data.unitType.ToString().ToUpper()}"; ptTMP.fontSize = 9;
        ptTMP.fontStyle = FontStyles.Bold;
        ptTMP.color = new Color(1f, 0.88f, 0.4f); ptTMP.alignment = TextAlignmentOptions.Center;

        // Partner name: 0.03–0.25
        var pnGO = new GameObject("PName"); pnGO.transform.SetParent(go.transform, false);
        var pnRT = pnGO.AddComponent<RectTransform>();
        pnRT.anchorMin = new Vector2(0, 0.03f); pnRT.anchorMax = new Vector2(1, 0.25f);
        pnRT.offsetMin = new Vector2(4, 2); pnRT.offsetMax = new Vector2(-4, -2);
        var pnTMP = pnGO.AddComponent<TextMeshProUGUI>();
        pnTMP.text = partner.data.cardName; pnTMP.fontSize = 12; pnTMP.fontStyle = FontStyles.Bold;
        pnTMP.color = Color.white; pnTMP.alignment = TextAlignmentOptions.Center;
        pnTMP.enableWordWrapping = true;

        // Selection highlight
        var selGO = new GameObject("Sel"); selGO.transform.SetParent(go.transform, false);
        selGO.transform.SetAsFirstSibling();
        var selRT = selGO.AddComponent<RectTransform>();
        selRT.anchorMin = Vector2.zero; selRT.anchorMax = Vector2.one;
        selRT.offsetMin = new Vector2(-4, -4); selRT.offsetMax = new Vector2(4, 4);
        var selImg = selGO.AddComponent<Image>();
        selImg.color = new Color(1f, 1f, 0f, 0.80f); selImg.raycastTarget = false;
        selGO.SetActive(false);

        // Disoriented overlay
        var dioGO = new GameObject("Diso"); dioGO.transform.SetParent(go.transform, false);
        var dioRT = dioGO.AddComponent<RectTransform>();
        dioRT.anchorMin = Vector2.zero; dioRT.anchorMax = Vector2.one;
        dioRT.offsetMin = dioRT.offsetMax = Vector2.zero;
        var dioImg = dioGO.AddComponent<Image>();
        dioImg.color = new Color(1f, 0.1f, 0.1f, 0.40f); dioImg.raycastTarget = false;
        dioGO.SetActive(cyborg.isDisoriented || partner.isDisoriented);

        var cv = go.AddComponent<CardView>();
        cv.artworkImage = null; cv.selectedHighlight = selGO;
        cv.disorientedOverlay = dioGO; cv.depletedOverlay = null;
        cv.Init(cyborg, onClick ?? (_ => { }));
        _views.Add(cv);
    }

    // ── Grouped creation-unit card (one card per type, with count badge) ────────

    private void SpawnCUStack(List<CardInstance> cus, Transform parent)
    {
        if (cus == null || cus.Count == 0) return;
        var rep   = cus[0];
        int total = cus.Count;
        int avail = cus.FindAll(c => !c.isDepleted).Count;

        var go = new GameObject($"{rep.data.cardName}x{total}");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = CardColor(rep.data);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 130; le.preferredHeight = 170;
        le.minWidth = 110; le.minHeight = 140;

        // Type band
        var bndGO = new GameObject("Band"); bndGO.transform.SetParent(go.transform, false);
        var bndRT = bndGO.AddComponent<RectTransform>();
        bndRT.anchorMin = new Vector2(0, 0.80f); bndRT.anchorMax = Vector2.one;
        bndRT.offsetMin = bndRT.offsetMax = Vector2.zero;
        bndGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.50f);
        var bTxtGO = new GameObject("T"); bTxtGO.transform.SetParent(bndGO.transform, false);
        var bTxtRT = bTxtGO.AddComponent<RectTransform>();
        bTxtRT.anchorMin = Vector2.zero; bTxtRT.anchorMax = Vector2.one;
        bTxtRT.offsetMin = new Vector2(3, 1); bTxtRT.offsetMax = new Vector2(-3, -1);
        var bTMP = bTxtGO.AddComponent<TextMeshProUGUI>();
        bTMP.text = TypeStr(rep.data); bTMP.fontSize = 10; bTMP.fontStyle = FontStyles.Bold;
        bTMP.color = new Color(1f, 0.88f, 0.4f); bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.enableWordWrapping = true;

        // Count badge (dark pill, top-right corner)
        var badgeGO = new GameObject("Badge"); badgeGO.transform.SetParent(go.transform, false);
        var badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.58f, 0.55f); badgeRT.anchorMax = new Vector2(1f, 0.80f);
        badgeRT.offsetMin = new Vector2(0, 2); badgeRT.offsetMax = new Vector2(-2, 0);
        badgeGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);
        var baTxtGO = new GameObject("T"); baTxtGO.transform.SetParent(badgeGO.transform, false);
        var baTxtRT = baTxtGO.AddComponent<RectTransform>();
        baTxtRT.anchorMin = Vector2.zero; baTxtRT.anchorMax = Vector2.one;
        baTxtRT.offsetMin = baTxtRT.offsetMax = Vector2.zero;
        var baTMP = baTxtGO.AddComponent<TextMeshProUGUI>();
        baTMP.text = $"×{avail}"; baTMP.fontSize = 18; baTMP.fontStyle = FontStyles.Bold;
        baTMP.color = Color.white; baTMP.alignment = TextAlignmentOptions.Center;

        // Small gray depleted count badge in bottom-right when any are spent
        if (total - avail > 0)
        {
            var dBadgeGO = new GameObject("DepBadge"); dBadgeGO.transform.SetParent(go.transform, false);
            var dBadgeRT = dBadgeGO.AddComponent<RectTransform>();
            dBadgeRT.anchorMin = new Vector2(0.55f, 0f); dBadgeRT.anchorMax = new Vector2(1f, 0.18f);
            dBadgeRT.offsetMin = new Vector2(0, 2); dBadgeRT.offsetMax = new Vector2(-2, 0);
            dBadgeGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.60f);
            var dBTxtGO = new GameObject("T"); dBTxtGO.transform.SetParent(dBadgeGO.transform, false);
            var dBTxtRT = dBTxtGO.AddComponent<RectTransform>();
            dBTxtRT.anchorMin = Vector2.zero; dBTxtRT.anchorMax = Vector2.one;
            dBTxtRT.offsetMin = dBTxtRT.offsetMax = Vector2.zero;
            var dBTMP = dBTxtGO.AddComponent<TextMeshProUGUI>();
            dBTMP.text = $"×{total - avail}"; dBTMP.fontSize = 13;
            dBTMP.color = new Color(0.60f, 0.60f, 0.60f, 1f); dBTMP.alignment = TextAlignmentOptions.Center;
        }

        // Card name (left of badge)
        var nGO = new GameObject("Name"); nGO.transform.SetParent(go.transform, false);
        var nRT = nGO.AddComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0.43f); nRT.anchorMax = new Vector2(0.60f, 0.80f);
        nRT.offsetMin = new Vector2(4, 2); nRT.offsetMax = new Vector2(-2, -2);
        var nTMP = nGO.AddComponent<TextMeshProUGUI>();
        nTMP.text = rep.data.cardName; nTMP.fontSize = 13; nTMP.fontStyle = FontStyles.Bold;
        nTMP.color = Color.white; nTMP.alignment = TextAlignmentOptions.TopLeft;
        nTMP.enableWordWrapping = true;

        // Availability indicator
        var aGO = new GameObject("Avail"); aGO.transform.SetParent(go.transform, false);
        var aRT = aGO.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0, 0.14f); aRT.anchorMax = new Vector2(1, 0.43f);
        aRT.offsetMin = new Vector2(4, 0); aRT.offsetMax = new Vector2(-4, 0);
        var aTMP = aGO.AddComponent<TextMeshProUGUI>();
        aTMP.text = avail == total ? $"✓ {avail} available"
                                   : $"✓ {avail} ready   ◉ {total - avail} spent";
        aTMP.fontSize = 11;
        aTMP.color = avail > 0 ? new Color(0.55f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f);
        aTMP.alignment = TextAlignmentOptions.Center;
        aTMP.enableWordWrapping = true;

        // Full depleted overlay when all are spent
        if (avail == 0)
        {
            var dGO = new GameObject("Dep"); dGO.transform.SetParent(go.transform, false);
            var dRT = dGO.AddComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero; dRT.anchorMax = Vector2.one;
            dRT.offsetMin = dRT.offsetMax = Vector2.zero;
            var dImg = dGO.AddComponent<Image>();
            dImg.color = new Color(0.05f, 0.05f, 0.05f, 0.58f); dImg.raycastTarget = false;
        }
        else if (avail < total) // Partial depletion: darken bottom portion
        {
            float ratio = (float)(total - avail) / total;
            var pdGO = new GameObject("PDep"); pdGO.transform.SetParent(go.transform, false);
            var pdRT = pdGO.AddComponent<RectTransform>();
            pdRT.anchorMin = Vector2.zero; pdRT.anchorMax = new Vector2(1, ratio * 0.6f);
            pdRT.offsetMin = pdRT.offsetMax = Vector2.zero;
            var pdImg = pdGO.AddComponent<Image>();
            pdImg.color = new Color(0.05f, 0.05f, 0.05f, 0.38f); pdImg.raycastTarget = false;
        }
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
        vp.AddComponent<RectMask2D>(); // clips children without stencil — avoids Color.clear mask bug
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
            return $"Cy {ci.CyberStrength}   Ps {ci.PsionicStrength}   Ph {ci.PhysicalStrength}";
        if (ci.data.cardType == CardType.Equipment)
        {
            var p = new List<string>();
            if (ci.data.equipCyberBonus    > 0) p.Add($"+{ci.data.equipCyberBonus} Cy");
            if (ci.data.equipPsionicBonus  > 0) p.Add($"+{ci.data.equipPsionicBonus} Ps");
            if (ci.data.equipPhysicalBonus > 0) p.Add($"+{ci.data.equipPhysicalBonus} Ph");
            return string.Join("  ", p);
        }
        return "";
    }

    private static string CostStr(CardData d)
    {
        var p = new List<string>();
        if (d.costDigitalSplicing   > 0) p.Add($"DS×{d.costDigitalSplicing}");
        if (d.costNeurogenesis      > 0) p.Add($"NG×{d.costNeurogenesis}");
        if (d.costBioAcceleration   > 0) p.Add($"BA×{d.costBioAcceleration}");
        if (d.costMaterialAnimation > 0) p.Add($"MA×{d.costMaterialAnimation}");
        if (d.costDestroy           > 0) p.Add($"Sac×{d.costDestroy}");
        return string.Join("  ", p);
    }

    private static string TypeStr(CardData d)
    {
        if (d.cardType == CardType.BattleUnit) return d.unitType.ToString().ToUpper();
        if (d.cardType == CardType.Equipment)  return "EQUIPMENT";
        if (d.cardType == CardType.Power)      return "POWER";
        return d.creationUnitType switch
        {
            CreationUnitType.DigitalSplicing   => "DIGITAL SPLICING",
            CreationUnitType.BioAcceleration   => "BIO-ACCELERATION",
            CreationUnitType.Neurogenesis      => "NEUROGENESIS",
            CreationUnitType.MaterialAnimation => "MATERIAL ANIMATION",
            _                                  => "CREATION UNIT"
        };
    }

    private static Color CardColor(CardData d)
    {
        if (d.cardType == CardType.CreationUnit)
            return d.creationUnitType switch
            {
                CreationUnitType.DigitalSplicing   => new Color(0.08f, 0.58f, 0.18f), // green
                CreationUnitType.Neurogenesis       => new Color(0.10f, 0.22f, 0.72f), // blue
                CreationUnitType.BioAcceleration    => new Color(0.72f, 0.10f, 0.10f), // red
                CreationUnitType.MaterialAnimation  => new Color(0.52f, 0.08f, 0.68f), // purple
                _                                   => new Color(0.20f, 0.20f, 0.25f)
            };
        if (d.cardType == CardType.Equipment) return new Color(0.45f, 0.26f, 0.08f); // warm brown
        return d.unitType switch
        {
            UnitType.Creature => new Color(0.14f, 0.40f, 0.17f), // forest green
            UnitType.Robot    => new Color(0.14f, 0.26f, 0.52f), // steel blue
            UnitType.Cyborg   => new Color(0.40f, 0.14f, 0.48f), // violet
            _                 => new Color(0.20f, 0.20f, 0.25f)
        };
    }
}
