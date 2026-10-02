using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Serializable]
    public class Chapter
    {
        public string title;
        public int levelCount = 5;
        [Tooltip("Picture shown on the chapter page of the Select Level book.")]
        public Texture2D art;
    }

    [Serializable]
    public class OnlineLevel
    {
        public string name;
        public string code;
        public string author;
        public string plays;
    }

    // Level node positions on the chapter map, in map-space pixels (top-left of each 64px node).
    private static readonly Vector2[] MapNodes =
    {
        new Vector2(60, 440), new Vector2(200, 340), new Vector2(90, 200), new Vector2(290, 90), new Vector2(480, 170),
    };

    private const string Hidden = "modal--hidden";
    private const string Entering = "modal--entering";
    private const int FlipMs = 350;
    private const int MarkerStepMs = 340;

    // Fully rounded ends. USS has no "999px" clamp like CSS, so the radius follows the element's height.
    private static readonly string[] PillClasses =
    {
        "pill-primary", "mm-play", "mm-item", "round-button", "map-footer", "map-locked",
        "search", "online-row", "online-row__code", "exit__button",
    };

    [SerializeField] private string levelCreatorScene = "LevelCreator";

    [SerializeField] private List<Chapter> chapters = new List<Chapter>
    {
        new Chapter { title = "The Factory" },
        new Chapter { title = "???" },
        new Chapter { title = "???" },
    };

    [Tooltip("Community levels shown in Online Levels. Empty until a backend fills it.")]
    [SerializeField] private List<OnlineLevel> onlineLevels = new List<OnlineLevel>();

    private VisualElement root;
    private VisualElement levelsModal, onlineModal, exitModal;
    private VisualElement book, pageLeft, pageRight, chapterArt, chapterLock, chapterCogs, map, mapLocked, mapFooter, marker;
    private Label chapterNum, chapterKicker, chapterName, mapTitle, selectedCode, selectedStatus;
    private Button prevButton, nextButton;
    private LevelMapView mapView;
    private readonly List<Button> mapNodes = new List<Button>();
    private TextField onlineSearch;
    private ScrollView onlineList;

    // Chapter being navigated to, and chapter whose map is on the right page (they differ mid-flip).
    private int chapter;
    private int mapChapter;
    private VisualElement leaf;
    private int selected;
    private int markerIndex;
    private bool flipping;
    private bool pulseOn;
    private float markerAngle;
    private IVisualElementScheduledItem walk;

    private static PlayerProgress Progress =>
        SQLiteDB.instance != null ? SQLiteDB.instance.playerProgress : new PlayerProgress(1, 1);

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // Drop the default theme's button look; every button here is styled by MainMenu.uss.
        root.Query<Button>(className: "mm-button").ForEach(b => b.RemoveFromClassList(Button.ussClassName));
        RoundPills(root);

        BindMainPanel();
        BindLevelsModal();
        BindOnlineModal();
        BindExitModal();

        root.schedule.Execute(Animate).Every(16);
        root.schedule.Execute(() => pulseOn = !pulseOn).Every(800);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame) CloseModals();
        if (!IsOpen(levelsModal)) return;
        if (keyboard.rightArrowKey.wasPressedThisFrame) FlipTo(chapter + 1);
        if (keyboard.leftArrowKey.wasPressedThisFrame) FlipTo(chapter - 1);
    }

    // ---------- main panel ----------

    private void BindMainPanel()
    {
        PlayerProgress p = Progress;
        bool hasProgress = p.chapter != 1 || p.level != 1;
        string chapterTitle = ChapterAt(p.chapter - 1)?.title ?? chapters[0].title;

        root.Q<Label>("play-label").text = hasProgress ? "Continue" : "Play";
        root.Q<Label>("play-sub").text = hasProgress
            ? $"Level {p.chapter}-{p.level} · {chapterTitle}"
            : $"Start in {chapters[0].title}";

        root.Q<Button>("play-button").clicked += () => LoadLevel(p.chapter, p.level);
        root.Q<Button>("levels-button").clicked += OpenLevels;
        root.Q<Button>("creator-button").clicked += () => LoadScene(levelCreatorScene);
        root.Q<Button>("online-button").clicked += OpenOnline;
        root.Q<Button>("exit-button").clicked += () => Open(exitModal);
    }

    // ---------- modals ----------

    private void Open(VisualElement modal)
    {
        CloseModals();
        modal.RemoveFromClassList(Hidden);
        modal.AddToClassList(Entering);
        // Let one frame render the entering state so the USS transition plays.
        modal.schedule.Execute(() => modal.RemoveFromClassList(Entering)).StartingIn(16);
    }

    private void CloseModals()
    {
        foreach (VisualElement modal in new[] { levelsModal, onlineModal, exitModal })
            modal.AddToClassList(Hidden);
    }

    private static bool IsOpen(VisualElement modal) => !modal.ClassListContains(Hidden);

    private void CloseOnBackdropClick(VisualElement modal)
    {
        modal.RegisterCallback<ClickEvent>(e =>
        {
            if (e.target == modal) CloseModals();
        });
    }

    // ---------- select level ----------

    private void BindLevelsModal()
    {
        levelsModal = root.Q("levels-modal");
        book = root.Q("book");
        pageLeft = root.Q("page-left");
        pageRight = root.Q("page-right");
        chapterNum = root.Q<Label>("chapter-num");
        chapterArt = root.Q("chapter-art");
        chapterLock = root.Q("chapter-lock");
        chapterKicker = root.Q<Label>("chapter-kicker");
        chapterName = root.Q<Label>("chapter-name");
        chapterCogs = root.Q("chapter-cogs");
        mapTitle = root.Q<Label>("map-title");
        map = root.Q("map");
        mapLocked = root.Q("map-locked");
        mapFooter = root.Q("map-footer");
        selectedCode = root.Q<Label>("selected-code");
        selectedStatus = root.Q<Label>("selected-status");
        prevButton = root.Q<Button>("chapter-prev");
        nextButton = root.Q<Button>("chapter-next");

        mapView = new LevelMapView();
        map.Add(mapView);
        marker = new VisualElement { pickingMode = PickingMode.Ignore };
        marker.AddToClassList("map-marker");

        CloseOnBackdropClick(levelsModal);
        root.Q<Button>("levels-close").clicked += CloseModals;
        prevButton.clicked += () => FlipTo(chapter - 1);
        nextButton.clicked += () => FlipTo(chapter + 1);
        root.Q<Button>("play-selected").clicked += () => LoadLevel(mapChapter + 1, selected + 1);
    }

    private void OpenLevels()
    {
        chapter = mapChapter = Mathf.Clamp(Progress.chapter - 1, 0, chapters.Count - 1);
        flipping = false;
        leaf?.RemoveFromHierarchy();
        RenderCover(chapter);
        RenderMap(chapter);
        RefreshNav();
        Open(levelsModal);
    }

    // Turns a single leaf around the spine, like the design's rotateY page flip:
    // the leaf folds flat over one page, then unfolds over the other showing its back face.
    private void FlipTo(int index)
    {
        if (index < 0 || index >= chapters.Count || index == chapter || flipping) return;

        flipping = true;
        int from = chapter;
        bool forward = index > from;
        chapter = index;
        RefreshNav();

        // The page the leaf uncovers shows the destination right away.
        if (forward) RenderMap(index);
        else RenderCover(index);

        SwapLeaf(forward ? MapFace(from) : CoverFace(from), unfolding: false);
        root.schedule.Execute(() =>
        {
            // Edge-on at the spine: the page it will land on is hidden, so update it now.
            if (forward) RenderCover(index);
            else RenderMap(index);
            SwapLeaf(forward ? CoverFace(index) : MapFace(index), unfolding: true);
        }).StartingIn(FlipMs);
        root.schedule.Execute(() =>
        {
            leaf?.RemoveFromHierarchy();
            leaf = null;
            flipping = false;
        }).StartingIn(FlipMs * 2);
    }

    private void SwapLeaf(VisualElement face, bool unfolding)
    {
        leaf?.RemoveFromHierarchy();
        leaf = face;
        leaf.AddToClassList("leaf");
        leaf.AddToClassList(unfolding ? "leaf--unfold" : "leaf--fold");
        if (unfolding) leaf.AddToClassList("leaf--flat");
        // Under the spine shadow and ribbon, above both pages.
        book.Insert(book.IndexOf(pageRight) + 1, leaf);
        // Toggle on the next frame so the USS transition runs from the initial state.
        leaf.schedule.Execute(() => leaf.EnableInClassList("leaf--flat", !unfolding)).StartingIn(16);
    }

    // Simplified faces of the leaf while it turns (the design shows title + kicker only).
    private VisualElement CoverFace(int index) => LeafFace("page--left", $"CHAPTER {index + 1}", chapters[index].title);

    private VisualElement MapFace(int index) => LeafFace("page--right", $"CHAPTER {index + 1} · MAP", chapters[index].title);

    private static VisualElement LeafFace(string side, string kicker, string title)
    {
        var face = new VisualElement { pickingMode = PickingMode.Ignore };
        face.AddToClassList("page");
        face.AddToClassList(side);
        var kickerLabel = new Label(kicker);
        kickerLabel.AddToClassList("font-bold");
        kickerLabel.AddToClassList("leaf__kicker");
        var titleLabel = new Label(title);
        titleLabel.AddToClassList("font-display");
        titleLabel.AddToClassList("leaf__title");
        face.Add(kickerLabel);
        face.Add(titleLabel);
        return face;
    }

    private void RefreshNav()
    {
        prevButton.SetEnabled(chapter > 0);
        nextButton.SetEnabled(chapter < chapters.Count - 1);
    }

    private Chapter ChapterAt(int index) => index >= 0 && index < chapters.Count ? chapters[index] : null;

    private int LevelCount(int index) => Mathf.Min(chapters[index].levelCount, MapNodes.Length);

    private bool IsUnlocked(int index) => index + 1 <= Progress.chapter;

    // Levels before the player's current one are completed.
    private int CompletedCount(int index)
    {
        PlayerProgress p = Progress;
        if (index + 1 < p.chapter) return LevelCount(index);
        if (index + 1 == p.chapter) return Mathf.Clamp(p.level - 1, 0, LevelCount(index));
        return 0;
    }

    // Left page: chapter cover.
    private void RenderCover(int index)
    {
        Chapter data = chapters[index];
        bool unlocked = IsUnlocked(index);
        int done = CompletedCount(index);

        chapterNum.text = (index + 1).ToString();
        chapterKicker.text = $"CHAPTER {index + 1}";
        chapterName.text = data.title;
        chapterArt.style.backgroundImage = unlocked && data.art != null ? new StyleBackground(data.art) : StyleKeyword.None;
        chapterLock.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
        chapterCogs.Clear();
        for (int k = 0; k < LevelCount(index); k++)
        {
            var cog = new VisualElement();
            cog.AddToClassList("icon");
            cog.AddToClassList("chapter__cog");
            if (k >= done) cog.AddToClassList("chapter__cog--pending");
            chapterCogs.Add(cog);
        }
    }

    // Right page: level map.
    private void RenderMap(int index)
    {
        mapChapter = index;
        bool unlocked = IsUnlocked(index);
        int levels = LevelCount(index);
        int done = CompletedCount(index);

        mapTitle.text = chapters[index].title;
        map.EnableInClassList("map--locked", !unlocked);
        mapLocked.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
        mapFooter.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (Button node in mapNodes) node.RemoveFromHierarchy();
        mapNodes.Clear();
        marker.RemoveFromHierarchy();

        var route = new List<Vector2> { new Vector2(-60, 490) };
        for (int k = 0; k < levels; k++)
        {
            Vector2 pos = MapNodes[k];
            route.Add(new Vector2(pos.x + 32, pos.y + 42));
            mapNodes.Add(CreateNode(k, pos, unlocked && k <= done));
        }
        mapView.SetRoute(route);

        walk?.Pause();
        selected = markerIndex = Mathf.Min(done, levels - 1);
        if (unlocked)
        {
            map.Add(marker);
            PlaceMarker();
        }
        RefreshSelection();
    }

    private Button CreateNode(int index, Vector2 pos, bool playable)
    {
        var node = new Button { name = $"level-{index + 1}" };
        node.RemoveFromClassList(Button.ussClassName);
        node.AddToClassList("mm-button");
        node.AddToClassList("map-node");
        node.style.left = pos.x;
        node.style.top = pos.y + 10;

        var ring = new VisualElement { pickingMode = PickingMode.Ignore };
        ring.AddToClassList("map-node__ring");
        var diamond = new VisualElement { pickingMode = PickingMode.Ignore };
        diamond.AddToClassList("map-node__diamond");
        var code = new Label($"{mapChapter + 1}-{index + 1}") { pickingMode = PickingMode.Ignore };
        code.AddToClassList("font-display");
        code.AddToClassList("map-node__code");
        node.Add(ring);
        node.Add(diamond);
        node.Add(code);

        node.SetEnabled(playable);
        node.clicked += () => WalkTo(index);
        map.Add(node);
        return node;
    }

    private void RefreshSelection()
    {
        bool unlocked = IsUnlocked(mapChapter);
        int done = CompletedCount(mapChapter);
        for (int k = 0; k < mapNodes.Count; k++)
        {
            mapNodes[k].EnableInClassList("map-node--done", unlocked && k < done);
            mapNodes[k].EnableInClassList("map-node--current", unlocked && k == done);
            mapNodes[k].EnableInClassList("map-node--picked", unlocked && k == selected);
        }

        selectedCode.text = $"Level {mapChapter + 1}-{selected + 1}";
        selectedStatus.text = selected < done ? "Completed · replay anytime" : "Next up";
    }

    // Move the gear marker one node at a time towards the picked level.
    private void WalkTo(int target)
    {
        selected = target;
        RefreshSelection();
        walk?.Pause();
        walk = root.schedule.Execute(() =>
        {
            if (markerIndex == target) { walk.Pause(); return; }
            markerIndex += target > markerIndex ? 1 : -1;
            PlaceMarker();
        }).Every(MarkerStepMs);
    }

    private void PlaceMarker()
    {
        Vector2 pos = MapNodes[markerIndex];
        marker.style.left = pos.x + 2;
        marker.style.top = pos.y - 52;
    }

    private void Animate()
    {
        markerAngle = (markerAngle + 90f * Time.unscaledDeltaTime) % 360f;
        marker.style.rotate = new Rotate(markerAngle);

        int done = CompletedCount(mapChapter);
        for (int k = 0; k < mapNodes.Count; k++)
            mapNodes[k].EnableInClassList("map-node--pulse", pulseOn && k == done && k != selected && IsUnlocked(mapChapter));
    }

    // ---------- online levels ----------

    private void BindOnlineModal()
    {
        onlineModal = root.Q("online-modal");
        onlineList = root.Q<ScrollView>("online-list");
        onlineSearch = root.Q<TextField>("online-search");
        onlineSearch.textEdition.placeholder = "Search by level name or code";
        onlineSearch.textEdition.hidePlaceholderOnFocus = false;
        onlineSearch.RegisterValueChangedCallback(e => RenderOnlineResults(e.newValue));

        CloseOnBackdropClick(onlineModal);
        root.Q<Button>("online-close").clicked += CloseModals;
    }

    private void OpenOnline()
    {
        onlineSearch.SetValueWithoutNotify(string.Empty);
        RenderOnlineResults(string.Empty);
        Open(onlineModal);
    }

    private static string Normalize(string s) => new string(s.ToLowerInvariant().Where(c => c != ' ' && c != '-').ToArray());

    private void RenderOnlineResults(string query)
    {
        string q = Normalize(query.Trim());
        List<OnlineLevel> results = onlineLevels
            .Where(o => q.Length == 0 || Normalize(o.name).Contains(q) || Normalize(o.code).Contains(q))
            .ToList();

        onlineList.Clear();
        foreach (OnlineLevel level in results)
            onlineList.Add(CreateOnlineRow(level));

        if (results.Count == 0)
        {
            var empty = new VisualElement();
            empty.AddToClassList("online__empty");
            bool catalogueEmpty = onlineLevels.Count == 0;
            var title = new Label(catalogueEmpty ? "No online levels yet" : "No levels found");
            title.AddToClassList("font-display");
            title.AddToClassList("online__empty-title");
            var body = new Label(catalogueEmpty
                ? "Community levels will show up here."
                : $"Nothing matches “{query}”. Check the name or code and try again.");
            body.AddToClassList("online__empty-body");
            empty.Add(title);
            empty.Add(body);
            onlineList.Add(empty);
        }
    }

    private VisualElement CreateOnlineRow(OnlineLevel level)
    {
        var row = new VisualElement();
        row.AddToClassList("online-row");

        var info = new VisualElement();
        info.AddToClassList("online-row__info");
        var name = new Label(level.name);
        name.AddToClassList("font-bold");
        name.AddToClassList("online-row__name");
        var meta = new Label($"by {level.author} · {level.plays} plays");
        meta.AddToClassList("font-semibold");
        meta.AddToClassList("online-row__meta");
        info.Add(name);
        info.Add(meta);

        var code = new Label(level.code);
        code.AddToClassList("font-bold");
        code.AddToClassList("online-row__code");

        var play = new Button();
        play.RemoveFromClassList(Button.ussClassName);
        play.AddToClassList("mm-button");
        play.AddToClassList("pill-primary");
        play.AddToClassList("online-row__play");
        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList("icon");
        icon.AddToClassList("icon--play");
        var label = new Label("Play") { pickingMode = PickingMode.Ignore };
        label.AddToClassList("font-bold");
        play.Add(icon);
        play.Add(label);
        play.clicked += () => Debug.LogWarning($"Online level {level.code} cannot be played yet: online levels have no backend.");

        row.Add(info);
        row.Add(code);
        row.Add(play);
        RoundPills(row);
        return row;
    }

    // ---------- exit ----------

    private void BindExitModal()
    {
        exitModal = root.Q("exit-modal");
        CloseOnBackdropClick(exitModal);
        root.Q<Button>("exit-stay").clicked += CloseModals;
        root.Q<Button>("exit-confirm").clicked += Application.Quit;
    }

    private static void RoundPills(VisualElement scope)
    {
        foreach (string className in PillClasses)
        {
            scope.Query(className: className).ForEach(element =>
                element.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    float radius = Mathf.Min(e.newRect.width, e.newRect.height) / 2f;
                    element.style.borderTopLeftRadius = radius;
                    element.style.borderTopRightRadius = radius;
                    element.style.borderBottomLeftRadius = radius;
                    element.style.borderBottomRightRadius = radius;
                }));
        }
    }

    // ---------- scene loading ----------

    private static void LoadLevel(int chapterNumber, int level) => LoadScene($"Level{chapterNumber}-{level}");

    private static void LoadScene(string sceneName)
    {
        if (LevelManager.instance != null) LevelManager.instance.LoadSceneFromUI(sceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
