using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Ventana de opciones (pestañas Graphics y Volume). Es un documento de UI Toolkit propio que se
/// crea al abrirla, así que se puede llamar desde cualquier escena: <see cref="Open"/> la muestra
/// y <see cref="Close"/> la oculta. La usan el menú principal y el menú de pausa.
/// Su UXML y sus PanelSettings están en Resources/UI/Options; el estilo es el del menú principal.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class OptionsMenu : MonoBehaviour
{
    private const string LayoutPath = "UI/Options/OptionsMenu";
    private const string PanelSettingsPath = "UI/Options/OptionsPanelSettings";
    private const string Entering = "modal--entering";
    private const string SelectClosed = "select__list--hidden";

    private static OptionsMenu instance;

    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    public static void Open()
    {
        if (instance == null)
        {
            var layout = Resources.Load<VisualTreeAsset>(LayoutPath);
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (layout == null || panelSettings == null)
            {
                Debug.LogError($"OptionsMenu: missing Resources/{LayoutPath}.uxml or Resources/{PanelSettingsPath}.asset.");
                return;
            }

            // Inactive while it is set up, so OnEnable runs once with the document already assigned.
            var host = new GameObject("Options Menu");
            host.SetActive(false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;
            instance = host.AddComponent<OptionsMenu>();
        }

        instance.gameObject.SetActive(true);
    }

    public static void Close()
    {
        if (instance != null) instance.gameObject.SetActive(false);
    }

    private VisualElement root;
    private VisualElement modal;
    // One refresh per setting row: re-reads the stored value and repaints the row.
    private readonly List<Action> optionRefreshers = new List<Action>();
    private readonly List<Button> fpsOptions = new List<Button>();
    private VisualElement fpsList;
    private Slider volumeSlider;
    private Label volumeValue;

    private static readonly string[] AdvancedTitles =
    {
        "Shadows", "Render scale", "Anti-aliasing", "Ambient occlusion", "Post-processing", "Volumetric fog", "Smoke and dust",
    };

    private static readonly string[] AdvancedHints =
    {
        "Off removes every shadow. Lower levels are blurrier and reach less far.",
        "Resolution the game is drawn at before scaling it to the screen. The biggest speed-up.",
        "Smooths jagged edges.",
        "Soft contact shadows in corners and under objects.",
        "Bloom, colour grading and the other screen effects.",
        "The lit fog that fills the pit.",
        "Smoke columns, steam and drifting dust clouds.",
    };

    // The document is rebuilt every time the object is enabled, so everything is bound here.
    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        modal = root.Q("options-modal");
        fpsList = null;

        // Drop the default theme's button look; every button here is styled by MainMenu.uss.
        root.Query<Button>(className: "mm-button").ForEach(b => b.RemoveFromClassList(Button.ussClassName));
        MainMenuController.RoundPills(root);
        root.RegisterCallback<ClickEvent>(OnAnyClick, TrickleDown.TrickleDown);

        Bind();

        // Let one frame render the entering state so the USS transition plays.
        modal.AddToClassList(Entering);
        modal.schedule.Execute(() => modal.RemoveFromClassList(Entering)).StartingIn(16);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private static void OnAnyClick(ClickEvent e)
    {
        for (var element = e.target as VisualElement; element != null; element = element.parent)
        {
            if (!(element is Button)) continue;
            MainMenuController.PlaySharedClick();
            return;
        }
    }

    private void Bind()
    {
        optionRefreshers.Clear();

        // Tabs
        Button graphicsTab = root.Q<Button>("tab-graphics");
        Button volumeTab = root.Q<Button>("tab-volume");
        VisualElement graphicsPage = root.Q("page-graphics");
        VisualElement volumePage = root.Q("page-volume");

        void ShowTab(bool graphics)
        {
            graphicsTab.EnableInClassList("tab--active", graphics);
            volumeTab.EnableInClassList("tab--active", !graphics);
            graphicsPage.EnableInClassList("options__page--hidden", !graphics);
            fpsList.AddToClassList(SelectClosed);
            volumePage.EnableInClassList("options__page--hidden", graphics);
        }

        graphicsTab.clicked += () => ShowTab(true);
        volumeTab.clicked += () => ShowTab(false);

        // Graphics: the profile and the frame cap, then every individual setting under "Advanced".
        VisualElement main = root.Q("graphics-main");
        main.Clear();
        AddChoiceRow(main, "Quality", "Sets every graphics setting at once.",
            GraphicsOptions.PresetNames,
            () => GraphicsOptions.Preset,
            index => GraphicsOptions.Preset = index,
            "Custom", GraphicsOptions.FallbackPreset);
        AddFpsRow(main, (ScrollView)graphicsPage);

        VisualElement advanced = root.Q("graphics-advanced");
        advanced.Clear();
        for (int k = 0; k < GraphicsOptions.SettingCount; k++)
        {
            var setting = (GraphicsOptions.Setting)k;
            AddChoiceRow(advanced, AdvancedTitles[k], AdvancedHints[k],
                GraphicsOptions.ValueNames[k],
                () => GraphicsOptions.Get(setting),
                index => GraphicsOptions.Set(setting, index));
        }

        Button advancedToggle = root.Q<Button>("advanced-toggle");
        advancedToggle.clicked += () =>
        {
            bool open = advanced.ClassListContains("advanced--hidden");
            advanced.EnableInClassList("advanced--hidden", !open);
            advancedToggle.EnableInClassList("advanced-toggle--open", open);
        };

        // Volume
        volumeSlider = root.Q<Slider>("volume-master");
        volumeValue = root.Q<Label>("volume-master-value");
        volumeSlider.RegisterValueChangedCallback(e =>
        {
            GameSettings.MasterVolume = e.newValue / 100f;
            volumeValue.text = $"{Mathf.RoundToInt(e.newValue)}%";
        });

        modal.RegisterCallback<ClickEvent>(e =>
        {
            if (e.target == modal) Close();
        });
        root.Q<Button>("options-close").clicked += Close;

        RefreshOptions();
        float volume = GameSettings.MasterVolume * 100f;
        volumeSlider.SetValueWithoutNotify(volume);
        volumeValue.text = $"{Mathf.RoundToInt(volume)}%";
    }

    private void RefreshOptions()
    {
        foreach (Action refresh in optionRefreshers) refresh();
    }

    private static string FpsLabel(int fps) => fps == GameSettings.Unlimited ? "Unlimited" : fps.ToString();

    // The Max FPS row: a select (button with the current value) that unfolds the list of choices.
    // The list is a child of the window, not of the scrolling page, so the page neither clips it
    // nor draws the rows below over it; it is placed under the button each time it opens.
    private void AddFpsRow(VisualElement parent, ScrollView page)
    {
        VisualElement card = root.Q("options-card");

        var row = new VisualElement();
        row.AddToClassList("setting");

        var text = new VisualElement();
        text.AddToClassList("setting__text");
        var titleLabel = new Label("Max FPS");
        titleLabel.AddToClassList("font-display");
        titleLabel.AddToClassList("setting__title");
        var hintLabel = new Label("Limits how many frames per second the game renders.");
        hintLabel.AddToClassList("font-semibold");
        hintLabel.AddToClassList("setting__hint");
        text.Add(titleLabel);
        text.Add(hintLabel);

        var select = new Button();
        select.RemoveFromClassList(Button.ussClassName);
        select.AddToClassList("mm-button");
        select.AddToClassList("select");
        var value = new Label { pickingMode = PickingMode.Ignore };
        value.AddToClassList("font-bold");
        value.AddToClassList("select__value");
        var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
        arrow.AddToClassList("icon");
        arrow.AddToClassList("icon--chevron-right");
        arrow.AddToClassList("select__arrow");
        select.Add(value);
        select.Add(arrow);

        fpsList?.RemoveFromHierarchy();
        fpsList = new VisualElement();
        fpsList.AddToClassList("select__list");
        fpsList.AddToClassList("options__scroll");
        fpsList.AddToClassList(SelectClosed);
        var scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
        scroll.AddToClassList("select__scroll");
        fpsList.Add(scroll);

        fpsOptions.Clear();
        foreach (int fps in GameSettings.FrameRateOptions)
        {
            int choice = fps;
            var option = new Button { text = FpsLabel(choice) };
            option.RemoveFromClassList(Button.ussClassName);
            option.AddToClassList("mm-button");
            option.AddToClassList("font-semibold");
            option.AddToClassList("select__option");
            option.clicked += () =>
            {
                GameSettings.MaxFps = choice;
                fpsList.AddToClassList(SelectClosed);
                RefreshOptions();
            };
            fpsOptions.Add(option);
            scroll.Add(option);
        }

        select.clicked += () =>
        {
            if (!fpsList.ClassListContains(SelectClosed))
            {
                fpsList.AddToClassList(SelectClosed);
                return;
            }

            // Just under the button, in the window's own coordinates (absolute children start inside the border).
            Rect button = select.worldBound;
            Vector2 corner = card.WorldToLocal(new Vector2(button.xMin, button.yMax));
            fpsList.style.left = corner.x - card.resolvedStyle.borderLeftWidth;
            fpsList.style.top = corner.y - card.resolvedStyle.borderTopWidth + 8f;
            fpsList.style.width = button.width;
            fpsList.RemoveFromClassList(SelectClosed);
        };

        // Clicking anywhere else in the window, or scrolling the page, folds the list back.
        modal.RegisterCallback<ClickEvent>(e =>
        {
            var target = e.target as VisualElement;
            if (target == select || fpsList.Contains(target)) return;
            fpsList.AddToClassList(SelectClosed);
        });
        page.verticalScroller.valueChanged += _ => fpsList.AddToClassList(SelectClosed);

        optionRefreshers.Add(() =>
        {
            int current = GameSettings.MaxFps;
            value.text = FpsLabel(current);
            for (int k = 0; k < fpsOptions.Count; k++)
                fpsOptions[k].EnableInClassList("select__option--selected", GameSettings.FrameRateOptions[k] == current);
        });

        row.Add(text);
        row.Add(select);
        parent.Add(row);
        card.Add(fpsList);
        MainMenuController.RoundPills(row);
    }

    // A setting row: title and hint on the left; on the right the current value between two arrows
    // that step through `choices`. When `get` returns an index outside the list (a custom mix of
    // settings), the row shows `otherLabel` and the arrows start from `otherIndex`.
    private void AddChoiceRow(VisualElement parent, string title, string hint, string[] choices,
        Func<int> get, Action<int> set, string otherLabel = "", int otherIndex = 0)
    {
        var row = new VisualElement();
        row.AddToClassList("setting");

        var text = new VisualElement();
        text.AddToClassList("setting__text");
        var titleLabel = new Label(title);
        titleLabel.AddToClassList("font-display");
        titleLabel.AddToClassList("setting__title");
        var hintLabel = new Label(hint);
        hintLabel.AddToClassList("font-semibold");
        hintLabel.AddToClassList("setting__hint");
        text.Add(titleLabel);
        text.Add(hintLabel);

        var stepper = new VisualElement();
        stepper.AddToClassList("stepper");
        Button previous = CreateStepperArrow("icon--chevron-left");
        Button next = CreateStepperArrow("icon--chevron-right");
        var value = new Label { pickingMode = PickingMode.Ignore };
        value.AddToClassList("font-bold");
        value.AddToClassList("stepper__value");
        stepper.Add(previous);
        stepper.Add(value);
        stepper.Add(next);

        void Step(int direction)
        {
            int index = get();
            bool listed = index >= 0 && index < choices.Length;
            set(Mathf.Clamp(listed ? index + direction : otherIndex, 0, choices.Length - 1));
            // One setting can change others (a profile sets them all; any of them can leave the profile).
            RefreshOptions();
        }

        previous.clicked += () => Step(-1);
        next.clicked += () => Step(1);

        optionRefreshers.Add(() =>
        {
            int index = get();
            bool listed = index >= 0 && index < choices.Length;
            value.text = listed ? choices[index] : otherLabel;
            previous.SetEnabled(!listed || index > 0);
            next.SetEnabled(!listed || index < choices.Length - 1);
        });

        row.Add(text);
        row.Add(stepper);
        parent.Add(row);
        MainMenuController.RoundPills(row);
    }

    private static Button CreateStepperArrow(string iconClass)
    {
        var button = new Button();
        button.RemoveFromClassList(Button.ussClassName);
        button.AddToClassList("mm-button");
        button.AddToClassList("stepper__arrow");
        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList("icon");
        icon.AddToClassList(iconClass);
        button.Add(icon);
        return button;
    }
}
