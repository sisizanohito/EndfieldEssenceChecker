using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace EndfieldEssenceChecker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        StartupLog("Main start");
        try
        {
            ApplicationConfiguration.Initialize();
            StartupLog("ApplicationConfiguration initialized");
            var form = new MainForm();
            StartupLog("MainForm constructed");
            form.FormClosed += (_, _) => Application.ExitThread();
            form.Show();
            StartupLog($"MainForm shown: handle={form.Handle}, visible={form.Visible}");
            Application.Run();
            StartupLog("Application.Run returned");
        }
        catch (Exception ex)
        {
            StartupLog("Fatal startup error: " + ex);
            MessageBox.Show(ex.ToString(), "EndfieldEssenceChecker 起動エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void StartupLog(string message)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "startup.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch
        {
            // Logging must never block app startup.
        }
    }
}

public sealed class MainForm : Form
{
    private static readonly Color Bg = Color.FromArgb(18, 22, 27);
    private static readonly Color PanelBg = Color.FromArgb(27, 33, 40);
    private static readonly Color PanelBg2 = Color.FromArgb(35, 42, 50);
    private static readonly Color Line = Color.FromArgb(70, 80, 92);
    private static readonly Color TextMain = Color.FromArgb(236, 240, 244);
    private static readonly Color TextMuted = Color.FromArgb(174, 184, 196);
    private static readonly Color Accent = Color.FromArgb(82, 193, 206);

    private static readonly string[] Effects =
    [
        "メイン能力UP", "筋力UP", "敏捷UP", "知性UP", "意思UP",
        "HP UP", "攻撃力UP", "会心率UP", "会心ダメージUP",
        "物理ダメージUP", "熱ダメージUP", "寒冷ダメージUP", "電磁ダメージUP", "自然ダメージUP",
        "物理・術ダメージUP", "術ダメージUP", "治療効果UP", "通常技UP", "スキル技UP", "必殺技UP",
        "圧制", "強攻", "効率", "巧技", "昂揚", "残虐", "治癒", "切骨", "追襲", "破砕", "付術", "噴発", "夜幕", "流回"
    ];
    private static readonly string[] PrimaryEffects = Effects.Take(5).ToArray();
    private static readonly string[] SecondaryEffects = Effects.Skip(5).Take(15).ToArray();
    private static readonly string[] SkillEffects = Effects.Skip(20).ToArray();
    private static readonly Dictionary<string, string> SiteTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attr_str"] = "筋力UP",
        ["attr_agi"] = "敏捷UP",
        ["attr_wisd"] = "知性UP",
        ["attr_will"] = "意思UP",
        ["attr_main"] = "メイン能力UP",
        ["attr_atk"] = "攻撃力UP",
        ["attr_hp"] = "HP UP",
        ["attr_crirate"] = "会心率UP",
        ["attr_phydam"] = "物理ダメージUP",
        ["attr_firedam"] = "熱ダメージUP",
        ["attr_pulsedam"] = "電磁ダメージUP",
        ["attr_icedam"] = "寒冷ダメージUP",
        ["attr_naturaldam"] = "自然ダメージUP",
        ["attr_physpell"] = "物理・術ダメージUP",
        ["attr_magicdam"] = "術ダメージUP",
        ["attr_usp"] = "必殺技UP",
        ["attr_heal"] = "治療効果UP",
        ["force"] = "強攻",
        ["tactic"] = "圧制",
        ["combo"] = "追襲",
        ["smash"] = "破砕",
        ["spirit"] = "昂揚",
        ["phyabn"] = "巧技",
        ["magabn"] = "付術",
        ["break"] = "残虐",
        ["heal"] = "治癒",
        ["crit"] = "切骨",
        ["burst"] = "噴発",
        ["ult"] = "夜幕",
        ["tacafter"] = "流回",
        ["keyword"] = "効率"
    };
    private static readonly Dictionary<string, string> BlackboardTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["all_attr_up"] = "メイン能力UP",
        ["all_attr_up2"] = "メイン能力UP",
        ["primary_attr_up"] = "メイン能力UP",
        ["second_attr_up"] = "メイン能力UP",
        ["atk_up"] = "攻撃力UP",
        ["atk_up_add"] = "攻撃力UP",
        ["atk_up_mult"] = "攻撃力UP",
        ["atk_up1"] = "攻撃力UP",
        ["atk_up2"] = "攻撃力UP",
        ["crit_up"] = "会心率UP",
        ["crit_up2"] = "会心率UP",
        ["hp_up"] = "HP UP",
        ["phy_damage_up"] = "物理ダメージUP",
        ["phy_damage_up2"] = "物理ダメージUP",
        ["phy_dmg_up"] = "物理ダメージUP",
        ["phy_dmg_up_mult"] = "物理ダメージUP",
        ["phy_dmg_up2"] = "物理ダメージUP",
        ["phy_dmg_up3"] = "物理ダメージUP",
        ["fire_dmg_up"] = "熱ダメージUP",
        ["pulse_dmg_up"] = "電磁ダメージUP",
        ["pulse_dmg_up2"] = "電磁ダメージUP",
        ["pulse_dmg_up3"] = "電磁ダメージUP",
        ["nature_dmg_up"] = "自然ダメージUP",
        ["nature_dmg_up_mult"] = "自然ダメージUP",
        ["cryst_dmg_up"] = "寒冷ダメージUP",
        ["cryst_dmg_up2"] = "寒冷ダメージUP",
        ["spell_dmg_up"] = "術ダメージUP",
        ["spell_dmg_up2"] = "術ダメージUP",
        ["spell_dmg_up3"] = "術ダメージUP",
        ["phy_spell_up"] = "物理・術ダメージUP",
        ["heal_up"] = "治療効果UP",
        ["normal_atk_up"] = "通常技UP"
    };
    // Some aliases below are deliberately mojibake strings emitted by OCR or garbled logs.
    // They are never displayed; they only let pasted/OCR text normalize back to canonical labels.
    private static readonly Dictionary<string, string[]> EffectAliases = new()
    {
        ["メイン能力UP"] = ["メイン能力", "メイン能カUP", "メイン能力アップ"],
        ["筋力UP"] = ["遲句鴨UP"],
        ["敏捷UP"] = ["謨乗差UP"],
        ["知性UP"] = ["遏･諤ｧUP", "知性", "知"],
        ["意思UP"] = ["諢乗拔P", "意思", "意思UP", "意志", "意志UP"],
        ["HP UP"] = ["HPUP", "HP"],
        ["攻撃力UP"] = ["謾ｻ謦・鴨UP", "攻撃", "攻撃力"],
        ["会心率UP"] = ["莨壼ｿ・紫UP", "会心率"],
        ["会心ダメージUP"] = ["莨壼ｿ・ム繝｡繝ｼ繧ｸUP", "会心ダメージ"],
        ["物理ダメージUP"] = ["迚ｩ逅・ム繝｡繝ｼ繧ｸUP", "物理ダメージ"],
        ["物理・術ダメージUP"] = ["物理術ダメージ", "物理・術ダメージ", "物理/術ダメージ"],
        ["術ダメージUP"] = ["術ダメージ", "アーツダメージ"],
        ["熱ダメージUP"] = ["辭ｱ繝繝｡繝ｼ繧ｸUP", "熱ダメージ"],
        ["寒冷ダメージUP"] = ["蟇貞・繝繝｡繝ｼ繧ｸUP", "寒冷ダメージ"],
        ["電磁ダメージUP"] = ["髮ｻ逎√ム繝｡繝ｼ繧ｸUP", "電磁ダメージ"],
        ["自然ダメージUP"] = ["閾ｪ辟ｶ繝繝｡繝ｼ繧ｸUP", "自然ダメージ"],
        ["治療効果UP"] = ["治療効果", "回復効果"],
        ["通常技UP"] = ["通常技", "通常攻撃"],
        ["スキル技UP"] = ["スキル技", "スキル"],
        ["必殺技UP"] = ["必殺技", "究極技", "ULT"],
        ["切骨"] = ["蛻・ｪｨ", "会心", "Crit"],
        ["圧制"] = ["蝨ｧ蛻ｶ"],
        ["強攻"] = ["Force"],
        ["効率"] = ["Keyword"],
        ["巧技"] = ["PhyAbn"],
        ["昂揚"] = ["Spirit"],
        ["残虐"] = ["Break"],
        ["治癒"] = ["Heal", "回復"],
        ["追襲"] = ["Combo", "連携", "コンボ"],
        ["破砕"] = ["Smash"],
        ["付術"] = ["MagAbn"],
        ["噴発"] = ["Burst", "バースト"],
        ["夜幕"] = ["螟懷ｹ・"],
        ["流回"] = ["Tactic", "戦術"]
    };

    private readonly TextBox nameBox = new() { PlaceholderText = "例: 純粋基質 / 候補A" };
    private readonly ComboBox rarityBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox baseBox = new();
    private readonly ComboBox subBox = new();
    private readonly ComboBox skillBox = new();
    private readonly NumericUpDown lv1 = new() { Minimum = 0, Maximum = 9 };
    private readonly NumericUpDown lv2 = new() { Minimum = 0, Maximum = 9 };
    private readonly NumericUpDown lv3 = new() { Minimum = 0, Maximum = 9 };
    private readonly CheckBox equipped = new() { Text = "装備中" };
    private readonly CheckBox locked = new() { Text = "ロック済み" };
    private readonly CheckBox pure = new() { Text = "純粋基質" };
    private readonly CheckBox duplicate = new() { Text = "同効果の上位/予備あり" };
    private readonly Label verdict = new() { Text = "入力待ち", AutoSize = false, Dock = DockStyle.Top, Height = 58, Font = new Font("Yu Gothic UI", 22, FontStyle.Bold) };
    private readonly Label reasons = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
    private readonly Label selectedEffects = new() { Text = "未選択", AutoSize = false, Dock = DockStyle.Fill, Font = new Font("Yu Gothic UI", 10, FontStyle.Bold) };
    private readonly DataGridView matchingWeapons = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        ReadOnly = true,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly Label feedStatus = new() { Text = "基質を選んでください", AutoSize = false, Dock = DockStyle.Top, Height = 34, Font = new Font("Yu Gothic UI", 14, FontStyle.Bold) };
    private readonly List<EffectChoiceButton> effectButtons = [];
    private readonly RichTextBox ocrText = new() { Dock = DockStyle.Fill, ReadOnly = false, ScrollBars = RichTextBoxScrollBars.Both, WordWrap = false, DetectUrls = false, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Yu Gothic UI", 11) };
    private readonly PictureBox preview = new() { BackColor = Color.FromArgb(14, 18, 23), SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private readonly PictureBox cardPreview = new() { BackColor = Color.FromArgb(14, 18, 23), SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private readonly DataGridView targets = new() { AllowUserToAddRows = true, AllowUserToDeleteRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Dock = DockStyle.Fill };
    private readonly NumericUpDown interval = new() { Minimum = 1, Maximum = 3600, Value = 5 };
    private readonly ComboBox windowBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
    private readonly CheckBox bringToFront = new() { Text = "前面化", Checked = false, AutoSize = true };
    private readonly TextBox modelBox = new() { Text = "gpt-4.1-mini", Width = 100 };
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly HttpClient http = new();
    private readonly string dataPath;
    private readonly string captureDir;
    private readonly string siteCachePath;
    private readonly string siteI18nCachePath;
    private readonly List<WindowItem> windows = [];
    private Bitmap? lastShot;
    private Bitmap? selectedCardShot;

    public MainForm()
    {
        Text = "エンドフィールド 基質餌チェック";
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1200, 800);
        Width = Math.Max(1120, Math.Min(1400, workArea.Width - 40));
        Height = Math.Max(660, Math.Min(800, workArea.Height - 60));
        MinimumSize = new Size(Math.Min(900, Width), Math.Min(640, Height));
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Yu Gothic UI", 9);
        BackColor = Bg;
        ForeColor = TextMain;

        dataPath = Path.Combine(AppContext.BaseDirectory, "targets.json");
        captureDir = Path.Combine(AppContext.BaseDirectory, "captures");
        siteCachePath = Path.Combine(AppContext.BaseDirectory, "endfieldtools_targets_cache.json");
        siteI18nCachePath = Path.Combine(AppContext.BaseDirectory, "endfieldtools_weapons_i18n_jp_cache.json");
        Directory.CreateDirectory(captureDir);

        rarityBox.Items.AddRange(["5★", "4★", "3★", "2★", "1★"]);
        rarityBox.SelectedIndex = 0;
        foreach (var box in new[] { baseBox, subBox, skillBox })
        {
            box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            box.AutoCompleteSource = AutoCompleteSource.ListItems;
            box.Items.AddRange(Effects);
        }
        ocrText.Text =
            "ログ / 読み取り結果\r\n" +
            "1. Endfield.exe を起動して「更新」を押す\r\n" +
            "2. 対象に Endfield.exe が選ばれていることを確認\r\n" +
            "3. 「撮影」でゲームウィンドウを撮る\r\n" +
            "4. 左カードを自動OCRして基質ボタンへ反映します\r\n\r\n" +
            "待機中: まだ読み取りは実行されていません。";

        ConfigureTargetsGrid();
        targets.Rows.Add("フレイムフォージ例", false, "知性UP", "攻撃力UP", "夜幕");
        targets.Rows.Add("弔いの詩例", false, "意思UP", "攻撃力UP", "夜幕");
        LoadTargets();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(12) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 620));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(BuildLeftPanel(), 0, 0);
        root.Controls.Add(BuildRightPanel(), 1, 0);
        ApplyTheme(this);
        UpdateEffectButtonState();
        targets.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (targets.IsCurrentCellDirty) targets.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        targets.CellValueChanged += (_, _) => ShowJudgement();

        timer.Tick += (_, _) => CaptureSelectedWindow();
        FormClosing += (_, _) => SaveTargets();
        Shown += (_, _) => FitInsideWorkingArea();
        RefreshWindowList();
        ShowJudgement();
    }

    private void FitInsideWorkingArea()
    {
        var workArea = Screen.FromControl(this).WorkingArea;
        if (Width > workArea.Width) Width = workArea.Width;
        if (Height > workArea.Height) Height = workArea.Height;

        var x = Math.Max(workArea.Left, Math.Min(Left, workArea.Right - Width));
        var y = Math.Max(workArea.Top, Math.Min(Top, workArea.Bottom - Height));
        Location = new Point(x, y);
    }

    private Control BuildEffectChoiceSection(string title, IReadOnlyList<string> effects, ComboBox targetBox, Color titleColor)
    {
        var section = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(0, 0, 0, 6) };
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = titleColor,
            Font = new Font("Yu Gothic UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        section.Controls.Add(titleLabel, 0, 0);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            AutoScroll = false,
            FlowDirection = FlowDirection.LeftToRight
        };
        AddEffectChoiceButton(flow, targetBox, "", "なし");
        foreach (var effect in effects)
        {
            AddEffectChoiceButton(flow, targetBox, effect, effect);
        }
        section.Controls.Add(flow, 0, 1);
        return section;
    }

    private void AddEffectChoiceButton(FlowLayoutPanel flow, ComboBox targetBox, string effect, string text)
    {
        var button = new Button
        {
            Text = text,
            Width = Math.Max(58, Math.Min(118, TextRenderer.MeasureText(text, Font).Width + 22)),
            Height = 32,
            Margin = new Padding(0, 0, 6, 6),
            Tag = effect
        };
        button.Click += (_, _) => SetSlotEffect(targetBox, effect);
        flow.Controls.Add(button);
        effectButtons.Add(new EffectChoiceButton(targetBox, effect, button));
    }

    private void SetSlotEffect(ComboBox targetBox, string effect)
    {
        targetBox.Text = effect;
        UpdateEffectButtonState();
        ShowJudgement();
    }

    private void ClearSelectedEffects()
    {
        SetSelectedEffects([]);
        locked.Checked = false;
        pure.Checked = false;
        duplicate.Checked = false;
    }

    private List<string> SelectedEffectList()
    {
        return new[] { baseBox.Text, subBox.Text, skillBox.Text }
            .Select(NormalizeEffectLabel)
            .Where(value => value.Length > 0)
            .Distinct()
            .ToList();
    }

    private void SetSelectedEffects(IReadOnlyList<string> effects)
    {
        var normalized = effects
            .Select(NormalizeEffectLabel)
            .Where(value => value.Length > 0)
            .Distinct()
            .ToList();
        baseBox.Text = FirstInCategory(normalized, PrimaryEffects);
        subBox.Text = FirstInCategory(normalized, SecondaryEffects);
        skillBox.Text = FirstInCategory(normalized, SkillEffects);
        UpdateEffectButtonState();
        ShowJudgement();
    }

    private static string FirstInCategory(IEnumerable<string> effects, IReadOnlyList<string> category)
    {
        return effects.FirstOrDefault(effect => category.Any(categoryEffect => EffectEquals(effect, categoryEffect))) ?? "";
    }

    private void UpdateEffectButtonState()
    {
        var selected = SelectedEffectList();
        selectedEffects.Text = selected.Count == 0
            ? "未選択"
            : $"基質1: {SlotText(baseBox)} / 基質2: {SlotText(subBox)} / 基質3: {SlotText(skillBox)}";

        foreach (var choice in effectButtons)
        {
            var current = NormalizeEffectLabel(choice.TargetBox.Text);
            var isSelected = choice.Effect.Length == 0
                ? current.Length == 0
                : EffectEquals(current, choice.Effect);
            choice.Button.BackColor = isSelected ? Accent : PanelBg2;
            choice.Button.ForeColor = isSelected ? Color.FromArgb(5, 18, 22) : TextMain;
            choice.Button.FlatAppearance.BorderColor = isSelected ? Accent : Line;
        }
    }

    private static string SlotText(ComboBox box)
    {
        var value = NormalizeEffectLabel(box.Text);
        return value.Length == 0 ? "なし" : value;
    }

    private Control BuildLeftPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 590));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var input = new GroupBox { Text = "左カードの基質選択", Dock = DockStyle.Fill };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 6 };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 186));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        selectedEffects.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(selectedEffects, 0, 0);
        effectButtons.Clear();
        grid.Controls.Add(BuildEffectChoiceSection("基質1 / 主な属性", PrimaryEffects, baseBox, Color.FromArgb(91, 167, 255)), 0, 1);
        grid.Controls.Add(BuildEffectChoiceSection("基質2 / 二次統計", SecondaryEffects, subBox, Color.FromArgb(82, 193, 206)), 0, 2);
        grid.Controls.Add(BuildEffectChoiceSection("基質3 / スキルステータス", SkillEffects, skillBox, Color.FromArgb(181, 117, 255)), 0, 3);
        var checks = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        checks.Controls.AddRange([locked, pure, duplicate]);
        grid.Controls.Add(checks, 0, 4);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var clear = new Button { Text = "選択解除", Width = 96 };
        var apply = new Button { Text = "OCR本文から抽出", Width = 150 };
        clear.Click += (_, _) => ClearSelectedEffects();
        apply.Click += (_, _) => ExtractFromText(ocrText.Text);
        buttons.Controls.AddRange([clear, apply]);
        grid.Controls.Add(buttons, 0, 5);
        input.Controls.Add(grid);
        panel.Controls.Add(input, 0, 0);

        var result = new GroupBox { Text = "判定 / 合致する武器", Dock = DockStyle.Fill };
        var resultLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 4 };
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        resultLayout.Controls.Add(verdict, 0, 0);
        resultLayout.Controls.Add(feedStatus, 0, 1);
        resultLayout.Controls.Add(reasons, 0, 2);
        resultLayout.Controls.Add(matchingWeapons, 0, 3);
        result.Controls.Add(resultLayout);
        panel.Controls.Add(result, 0, 1);
        ConfigureMatchGrid();

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var manage = new Button { Text = "武器基質を管理", Dock = DockStyle.Fill };
        manage.Click += (_, _) => ShowTargetsDialog();
        bottom.Controls.Add(manage, 0, 0);
        panel.Controls.Add(bottom, 0, 2);
        return panel;
    }

    private Control BuildRightPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));

        var tools = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 0, 0, 8) };
        tools.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tools.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var targetRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false };
        var optionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = false };
        var refresh = new Button { Text = "更新", Width = 58 };
        var capture = new Button { Text = "撮影", Width = 74 };
        var auto = new Button { Text = "自動", Width = 58 };
        var open = new Button { Text = "画像", Width = 58 };
        var zoomLog = new Button { Text = "ログ", Width = 58 };
        refresh.Click += (_, _) => RefreshWindowList();
        capture.Click += (_, _) => CaptureSelectedWindow();
        auto.Click += (_, _) => ToggleAutoCapture();
        open.Click += (_, _) => OpenImage();
        zoomLog.Click += (_, _) => ShowLargeLog();
        targetRow.Controls.AddRange([
            new Label { Text = "対象", AutoSize = true, Padding = new Padding(0, 9, 0, 0) },
            windowBox,
            refresh,
            capture
        ]);
        optionRow.Controls.AddRange([
            new Label { Text = "間隔(秒)", AutoSize = true, Padding = new Padding(0, 9, 0, 0) },
            interval,
            auto,
            bringToFront,
            open,
            zoomLog
        ]);
        tools.Controls.Add(targetRow, 0, 0);
        tools.Controls.Add(optionRow, 0, 1);
        panel.Controls.Add(tools, 0, 0);

        var imageGroup = new GroupBox { Text = "対象ウィンドウ / 左の選択基質カード", Dock = DockStyle.Fill };
        var imageSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 430 };
        imageSplit.Panel1.Controls.Add(preview);
        imageSplit.Panel2.Controls.Add(cardPreview);
        imageGroup.Controls.Add(imageSplit);
        panel.Controls.Add(imageGroup, 0, 1);

        var ocrGroup = new GroupBox { Text = "ログ / OCR本文", Dock = DockStyle.Fill };
        ocrGroup.Controls.Add(ocrText);
        panel.Controls.Add(ocrGroup, 0, 2);
        return panel;
    }

    private static void ApplyTheme(Control root)
    {
        foreach (Control control in root.Controls)
        {
            ApplyTheme(control);
        }

        switch (root)
        {
            case Form form:
                form.BackColor = Bg;
                form.ForeColor = TextMain;
                break;
            case GroupBox group:
                group.BackColor = PanelBg;
                group.ForeColor = TextMain;
                group.Padding = new Padding(10);
                break;
            case TableLayoutPanel or FlowLayoutPanel or Panel or SplitContainer:
                root.BackColor = Bg;
                root.ForeColor = TextMain;
                break;
            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = TextMuted;
                break;
            case TextBox box:
                box.BackColor = Color.FromArgb(14, 18, 23);
                box.ForeColor = TextMain;
                box.BorderStyle = BorderStyle.FixedSingle;
                break;
            case RichTextBox rich:
                rich.BackColor = Color.FromArgb(14, 18, 23);
                rich.ForeColor = TextMain;
                rich.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox list:
                list.BackColor = Color.FromArgb(14, 18, 23);
                list.ForeColor = TextMain;
                list.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = Color.FromArgb(14, 18, 23);
                combo.ForeColor = TextMain;
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = Color.FromArgb(14, 18, 23);
                numeric.ForeColor = TextMain;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = PanelBg2;
                button.ForeColor = TextMain;
                button.FlatAppearance.BorderColor = Line;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 58, 68);
                button.Height = Math.Max(button.Height, 30);
                break;
            case CheckBox check:
                check.BackColor = Color.Transparent;
                check.ForeColor = TextMain;
                break;
            case DataGridView grid:
                grid.BackgroundColor = PanelBg;
                grid.GridColor = Line;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = PanelBg2;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMain;
                grid.RowHeadersDefaultCellStyle.BackColor = PanelBg2;
                grid.RowHeadersDefaultCellStyle.ForeColor = TextMain;
                grid.DefaultCellStyle.BackColor = Color.FromArgb(14, 18, 23);
                grid.DefaultCellStyle.ForeColor = TextMain;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(48, 86, 96);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 25, 31);
                break;
            case PictureBox picture:
                picture.BackColor = Color.FromArgb(12, 15, 19);
                break;
        }

        if (root is Button button2 && (button2.Text.Contains("撮影") || button2.Text.Contains("読取") || button2.Text.Contains("OCR")))
        {
            button2.BackColor = Accent;
            button2.ForeColor = Color.FromArgb(5, 18, 22);
            button2.FlatAppearance.BorderColor = Accent;
        }
    }

    private static void AddLabeled(TableLayoutPanel grid, string text, Control control, int col, int row, int span = 1)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 8) };
        var label = new Label { Text = text, Dock = DockStyle.Top, Height = 18 };
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(label);
        grid.Controls.Add(panel, col, row);
        if (span > 1) grid.SetColumnSpan(panel, span);
    }

    private void RefreshWindowList()
    {
        windows.Clear();
        windowBox.Items.Clear();
        foreach (var process in Process.GetProcesses().OrderBy(p => p.ProcessName))
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(process.MainWindowTitle)) continue;
                windows.Add(new WindowItem(process.Id, process.ProcessName, process.MainWindowTitle, process.MainWindowHandle));
            }
            catch
            {
                // Processes can exit while enumerating.
            }
        }

        foreach (var window in windows) windowBox.Items.Add(window.DisplayName);

        var preferred = windows.FindIndex(w =>
            w.ProcessName.Contains("endfield", StringComparison.OrdinalIgnoreCase) ||
            w.Title.Contains("Endfield", StringComparison.OrdinalIgnoreCase) ||
            w.Title.Contains("エンドフィールド", StringComparison.OrdinalIgnoreCase));
        windowBox.SelectedIndex = windows.Count == 0 ? -1 : Math.Max(0, preferred);
        Text = windows.Count == 0
            ? "エンドフィールド 基質餌チェック - 対象ウィンドウなし"
            : "エンドフィールド 基質餌チェック - 対象を選択してください";
    }

    private void CaptureSelectedWindow()
    {
        var item = SelectedWindow();
        if (item is null)
        {
            MessageBox.Show("対象のゲームウィンドウを選択してください。見つからない場合はゲームを起動して「ウィンドウ更新」を押してください。");
            return;
        }

        if (!IsWindow(item.Handle))
        {
            RefreshWindowList();
            MessageBox.Show("対象ウィンドウが見つからなくなりました。ウィンドウ一覧を更新しました。");
            return;
        }

        if (bringToFront.Checked)
        {
            ShowWindow(item.Handle, SW_RESTORE);
            SetForegroundWindow(item.Handle);
            Thread.Sleep(180);
        }

        var rect = GetCaptureRect(item.Handle);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            MessageBox.Show("対象ウィンドウのサイズを取得できませんでした。最小化されている場合は復元してください。");
            return;
        }

        Bitmap? bitmap;
        var captureMode = "";
        if (bringToFront.Checked)
        {
            bitmap = CaptureFromScreen(rect);
            captureMode = "前面撮影";
        }
        else if (CaptureWindowGraphics(item.Handle) is { } graphicsCapture)
        {
            bitmap = graphicsCapture;
            captureMode = "背景撮影";
        }
        else if (CaptureWindowDirect(item.Handle, rect.Size) is { } directCapture)
        {
            bitmap = directCapture;
            captureMode = "背景撮影(PrintWindow)";
        }
        else
        {
            ocrText.Text =
                "背景撮影に失敗しました。\r\n" +
                "対象ゲームの描画方式では、前面化なしのウィンドウ直接撮影が使えない可能性があります。\r\n\r\n" +
                "右上の「前面化」をONにすると、従来方式で撮影できます。";
            Text = "エンドフィールド 基質餌チェック - 背景撮影失敗";
            return;
        }

        lastShot?.Dispose();
        lastShot = bitmap;
        preview.Image = lastShot;
        CropSelectedEssenceCard(showMessage: false);
        var safeExe = Regex.Replace(item.ProcessName, @"[^\w.-]+", "_");
        var path = Path.Combine(captureDir, $"{safeExe}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        lastShot.Save(path, ImageFormat.Png);
        Text = $"エンドフィールド 基質餌チェック - {captureMode}: {Path.GetFileName(path)}";
        _ = RunWindowsOcrAsync(showErrors: false);
    }

    private Rectangle GetCaptureRect(IntPtr hwnd)
    {
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT dwmRect, Marshal.SizeOf<RECT>()) == 0)
        {
            return Rectangle.FromLTRB(dwmRect.Left, dwmRect.Top, dwmRect.Right, dwmRect.Bottom);
        }

        return GetWindowRect(hwnd, out var rect)
            ? Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom)
            : Rectangle.Empty;
    }

    private static Bitmap CaptureFromScreen(Rectangle rect)
    {
        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Bitmap? CaptureWindowDirect(IntPtr hwnd, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0) return null;

        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        var hdc = g.GetHdc();
        try
        {
            if (PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT) || PrintWindow(hwnd, hdc, 0))
            {
                return bitmap;
            }
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }

        bitmap.Dispose();
        return null;
    }

    private static Bitmap? CaptureWindowGraphics(IntPtr hwnd)
    {
        if (!GraphicsCaptureSession.IsSupported()) return null;

        IntPtr d3dDevice = IntPtr.Zero;
        IntPtr d3dContext = IntPtr.Zero;
        IntPtr dxgiDevice = IntPtr.Zero;
        IntPtr winrtDevice = IntPtr.Zero;
        try
        {
            var item = CreateCaptureItemForWindow(hwnd);
            if (item is null || item.Size.Width <= 0 || item.Size.Height <= 0) return null;

            var hr = D3D11CreateDevice(
                IntPtr.Zero,
                D3D_DRIVER_TYPE_HARDWARE,
                IntPtr.Zero,
                D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                null,
                0,
                D3D11_SDK_VERSION,
                out d3dDevice,
                out _,
                out d3dContext);
            if (hr < 0 || d3dDevice == IntPtr.Zero || d3dContext == IntPtr.Zero) return null;

            var iidDxgiDevice = IID_IDXGIDevice;
            Marshal.QueryInterface(d3dDevice, in iidDxgiDevice, out dxgiDevice);
            if (dxgiDevice == IntPtr.Zero) return null;

            hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out winrtDevice);
            if (hr < 0 || winrtDevice == IntPtr.Zero) return null;

            var device = (IDirect3DDevice)Marshal.GetObjectForIUnknown(winrtDevice);
            using var frameReady = new ManualResetEventSlim(false);
            using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                item.Size);
            using var session = framePool.CreateCaptureSession(item);
            Direct3D11CaptureFrame? capturedFrame = null;
            framePool.FrameArrived += (_, _) =>
            {
                capturedFrame ??= framePool.TryGetNextFrame();
                frameReady.Set();
            };
            session.IsCursorCaptureEnabled = false;
            session.StartCapture();
            if (!frameReady.Wait(1200) || capturedFrame is null) return null;

            using (capturedFrame)
            {
                return CopyFrameToBitmap(capturedFrame.Surface, d3dDevice, d3dContext);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            ReleaseIfNeeded(winrtDevice);
            ReleaseIfNeeded(dxgiDevice);
            ReleaseIfNeeded(d3dContext);
            ReleaseIfNeeded(d3dDevice);
        }
    }

    private static GraphicsCaptureItem? CreateCaptureItemForWindow(IntPtr hwnd)
    {
        IntPtr hstring = IntPtr.Zero;
        IntPtr factoryPtr = IntPtr.Zero;
        IntPtr itemPtr = IntPtr.Zero;
        try
        {
            const string captureItemClassName = "Windows.Graphics.Capture.GraphicsCaptureItem";
            WindowsCreateString(captureItemClassName, captureItemClassName.Length, out hstring);
            var iidInterop = IID_IGraphicsCaptureItemInterop;
            var hr = RoGetActivationFactory(hstring, ref iidInterop, out factoryPtr);
            if (hr < 0 || factoryPtr == IntPtr.Zero) return null;

            var interop = Marshal.GetObjectForIUnknown(factoryPtr) as IGraphicsCaptureItemInterop;
            if (interop is null) return null;

            var iidItem = IID_IGraphicsCaptureItem;
            hr = interop.CreateForWindow(hwnd, ref iidItem, out itemPtr);
            if (hr < 0 || itemPtr == IntPtr.Zero) return null;

            return (GraphicsCaptureItem)Marshal.GetObjectForIUnknown(itemPtr);
        }
        finally
        {
            ReleaseIfNeeded(itemPtr);
            ReleaseIfNeeded(factoryPtr);
            if (hstring != IntPtr.Zero) WindowsDeleteString(hstring);
        }
    }

    private static Bitmap? CopyFrameToBitmap(IDirect3DSurface surface, IntPtr d3dDevice, IntPtr d3dContext)
    {
        IntPtr texture = IntPtr.Zero;
        IntPtr staging = IntPtr.Zero;
        try
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var iidTexture = IID_ID3D11Texture2D;
            texture = access.GetInterface(ref iidTexture);
            if (texture == IntPtr.Zero) return null;

            var desc = D3D11Texture2D.GetDesc(texture);
            if (desc.Width <= 0 || desc.Height <= 0) return null;

            var stagingDesc = desc;
            stagingDesc.Usage = D3D11_USAGE_STAGING;
            stagingDesc.BindFlags = 0;
            stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
            stagingDesc.MiscFlags = 0;
            stagingDesc.MipLevels = 1;
            stagingDesc.ArraySize = 1;
            stagingDesc.SampleDesc.Count = 1;
            stagingDesc.SampleDesc.Quality = 0;

            var hr = D3D11Device.CreateTexture2D(d3dDevice, ref stagingDesc, IntPtr.Zero, out staging);
            if (hr < 0 || staging == IntPtr.Zero) return null;

            D3D11DeviceContext.CopyResource(d3dContext, staging, texture);
            hr = D3D11DeviceContext.Map(d3dContext, staging, 0, D3D11_MAP_READ, 0, out var mapped);
            if (hr < 0 || mapped.DataPointer == IntPtr.Zero) return null;

            try
            {
                var bitmap = new Bitmap(desc.Width, desc.Height, PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, desc.Width, desc.Height);
                var bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var rowBytes = desc.Width * 4;
                    for (var y = 0; y < desc.Height; y++)
                    {
                        var src = IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch);
                        var dst = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                        CopyMemory(dst, src, (nuint)rowBytes);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                return bitmap;
            }
            finally
            {
                D3D11DeviceContext.Unmap(d3dContext, staging, 0);
            }
        }
        finally
        {
            ReleaseIfNeeded(staging);
            ReleaseIfNeeded(texture);
        }
    }

    private WindowItem? SelectedWindow()
    {
        if (windowBox.SelectedIndex < 0 || windowBox.SelectedIndex >= windows.Count) return null;
        return windows[windowBox.SelectedIndex];
    }

    private void ToggleAutoCapture()
    {
        if (timer.Enabled)
        {
            timer.Stop();
            Text = "エンドフィールド 基質餌チェック - 自動撮影停止";
            return;
        }
        timer.Interval = Math.Max(1000, (int)interval.Value * 1000);
        timer.Start();
        CaptureSelectedWindow();
        Text = "エンドフィールド 基質餌チェック - 自動撮影中";
    }

    private void OpenImage()
    {
        using var dialog = new OpenFileDialog { Filter = "画像|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        lastShot?.Dispose();
        lastShot = new Bitmap(dialog.FileName);
        preview.Image = lastShot;
        CropSelectedEssenceCard(showMessage: false);
    }

    private async void RunOcr()
    {
        if (lastShot is null)
        {
            MessageBox.Show("先に対象ウィンドウを撮影するか、画像を開いてください。");
            return;
        }

        var tesseract = FindTesseract();
        if (tesseract is null)
        {
            var message = "OCRエンジンが見つかりません。\r\n\r\nこのツールのOCRは Tesseract OCR を使います。\r\n次のどちらかに tesseract.exe を置いてください。\r\n\r\n1. このexeと同じフォルダ\r\n2. C:\\Program Files\\Tesseract-OCR\\tesseract.exe\r\n3. PATHが通っている場所\r\n\r\n日本語OCRには jpn.traineddata も必要です。\r\nスクショ撮影と手入力の餌チェックはこのまま使えます。";
            ocrText.Text = message;
            MessageBox.Show(message, "OCRエラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var imageForOcr = selectedCardShot ?? lastShot;
        if (imageForOcr is null)
        {
            MessageBox.Show("OCR対象の画像がありません。先に対象ウィンドウを撮影してください。");
            return;
        }

        var input = Path.Combine(captureDir, "ocr_input.png");
        var outputBase = Path.Combine(captureDir, "ocr_output");
        imageForOcr.Save(input, ImageFormat.Png);

        ocrText.Text = $"OCR実行中...\r\nエンジン: {tesseract}\r\n入力: {input}";
        var result = await Task.Run(() => RunTesseract(tesseract, input, outputBase));
        if (!result.Success)
        {
            ocrText.Text = result.Message;
            MessageBox.Show(result.Message, "OCRエラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ocrText.Text = MakeOcrReadable(result.Message);
        ExtractFromText(ocrText.Text);
    }

    private async Task RunWindowsOcrAsync(bool showErrors)
    {
        var imageForOcr = selectedCardShot ?? lastShot;
        if (imageForOcr is null)
        {
            if (showErrors) MessageBox.Show("無料OCR対象の画像がありません。先に対象ウィンドウを撮影してください。");
            return;
        }

        var script = Path.Combine(AppContext.BaseDirectory, "winocr.ps1");
        if (!File.Exists(script))
        {
            var message = $"無料OCRスクリプトが見つかりません。\r\n{script}";
            ocrText.Text = message;
            if (showErrors) MessageBox.Show(message, "無料OCRエラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var input = Path.Combine(captureDir, "winocr_input.png");
        imageForOcr.Save(input, ImageFormat.Png);
        ocrText.Text = "無料OCR実行中...\r\nWindows標準OCRで左カード画像を読んでいます。";

        var result = await Task.Run(() => RunWindowsOcrScript(script, input));
        if (!result.Success)
        {
            ocrText.Text = result.Message;
            if (showErrors) MessageBox.Show(result.Message, "無料OCRエラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ocrText.Text = MakeOcrReadable(result.Message);
        ExtractFromText(ocrText.Text);
    }

    private static OcrRunResult RunWindowsOcrScript(string script, string input)
    {
        var output = Path.ChangeExtension(input, ".txt");
        try
        {
            if (File.Exists(output)) File.Delete(output);
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ImagePath \"{input}\" -OutputPath \"{output}\" -LanguageTag ja",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using var process = Process.Start(psi);
            if (process is null) return new(false, "無料OCRプロセスを開始できませんでした。");
            if (!process.WaitForExit(30000))
            {
                try { process.Kill(); } catch { }
                return new(false, "無料OCRが30秒以内に完了しませんでした。");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                return new(false, $"無料OCRに失敗しました。ExitCode={process.ExitCode}\r\n\r\n{stderr}\r\n{stdout}");
            }

            var text = File.Exists(output) ? File.ReadAllText(output, Encoding.UTF8) : stdout;
            return string.IsNullOrWhiteSpace(text)
                ? new(false, "無料OCRで文字を検出できませんでした。左カードが切り出されているか確認してください。")
                : new(true, text.Trim());
        }
        catch (Exception ex)
        {
            return new(false, "無料OCR実行中に例外が発生しました。\r\n" + ex.Message);
        }
    }

    private async void RunAiVision()
    {
        var apiKey = GetOpenAiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var message = "OpenAI APIキーが設定されていません。\r\n\r\n右上の「APIキー設定」からキー作成ページを開き、作成したキーを貼り付けて保存してください。\r\n\r\n環境変数 OPENAI_API_KEY がある場合はそちらも使えます。";
            ocrText.Text = message;
            MessageBox.Show(message, "AI読取エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var imageForAi = selectedCardShot ?? lastShot;
        if (imageForAi is null)
        {
            MessageBox.Show("AI読取対象の画像がありません。先に対象ウィンドウを撮影してください。");
            return;
        }

        try
        {
            ocrText.Text = "AI読取中...\r\n左の選択基質カード画像を解析しています。";
            using var stream = new MemoryStream();
            imageForAi.Save(stream, ImageFormat.Png);
            var base64 = Convert.ToBase64String(stream.ToArray());
            var dataUrl = "data:image/png;base64," + base64;

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(modelBox.Text) ? "gpt-4.1-mini" : modelBox.Text.Trim(),
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_text",
                                text = """
                                アークナイツ:エンドフィールドの基質カード画像を読んでください。
                                左側で選択されている基質カードだけが対象です。
                                次のJSONだけを返してください。説明文やMarkdownは不要です。
                                不明な値は空文字または0にしてください。
                                {
                                  "name": "基質名",
                                  "rarity": 5,
                                  "base": "基質1の効果名",
                                  "sub": "基質2の効果名",
                                  "skill": "基質3の効果名",
                                  "lv1": 0,
                                  "lv2": 0,
                                  "lv3": 0,
                                  "locked": false,
                                  "equipped": false,
                                  "pure": false
                                }
                                効果名の例: 筋力UP, 攻撃力UP, 切骨, 圧制, 四号谷地。
                                """
                            },
                            new
                            {
                                type = "input_image",
                                image_url = dataUrl,
                                detail = "high"
                            }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var message = $"AI読取に失敗しました。HTTP {(int)response.StatusCode}\r\n\r\n{body}";
                ocrText.Text = message;
                MessageBox.Show(message, "AI読取エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var outputText = ExtractOutputText(body);
            ocrText.Text = outputText.Length == 0 ? body : outputText;
            ApplyAiJson(ocrText.Text);
        }
        catch (Exception ex)
        {
            var message = "AI読取中に例外が発生しました。\r\n" + ex.Message;
            ocrText.Text = message;
            MessageBox.Show(message, "AI読取エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyAiJson(string text)
    {
        var json = ExtractJsonObject(text);
        if (json.Length == 0)
        {
            MessageBox.Show("AIの返答からJSONを取り出せませんでした。ログを確認してください。", "AI読取エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        nameBox.Text = GetString(root, "name", nameBox.Text);
        var rarity = GetInt(root, "rarity", 5);
        if (rarity is >= 1 and <= 5) rarityBox.SelectedIndex = 5 - rarity;
        baseBox.Text = GetString(root, "base", baseBox.Text);
        subBox.Text = GetString(root, "sub", subBox.Text);
        skillBox.Text = GetString(root, "skill", skillBox.Text);
        lv1.Value = ClampLv(GetInt(root, "lv1", (int)lv1.Value));
        lv2.Value = ClampLv(GetInt(root, "lv2", (int)lv2.Value));
        lv3.Value = ClampLv(GetInt(root, "lv3", (int)lv3.Value));
        locked.Checked = GetBool(root, "locked", locked.Checked);
        equipped.Checked = GetBool(root, "equipped", equipped.Checked);
        pure.Checked = GetBool(root, "pure", pure.Checked) || nameBox.Text.Contains("純粋基質");
        UpdateEffectButtonState();
        ShowJudgement();
    }

    private void ShowApiKeyDialog()
    {
        using var dialog = new Form
        {
            Text = "OpenAI APIキー設定",
            Width = 720,
            Height = 320,
            StartPosition = FormStartPosition.CenterParent,
            Font = new Font("Yu Gothic UI", 10)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var info = new Label
        {
            Text = "OpenAI APIキーはOpenAIのダッシュボードで作成します。アプリがログインだけで自動取得することはできません。保存したキーはWindows資格情報に入れます。",
            Dock = DockStyle.Fill
        };
        var keyBox = new TextBox
        {
            PlaceholderText = "sk-...",
            UseSystemPasswordChar = true,
            Dock = DockStyle.Fill,
            Text = ReadCredential() ?? ""
        };
        var show = new CheckBox { Text = "表示", Dock = DockStyle.Left };
        show.CheckedChanged += (_, _) => keyBox.UseSystemPasswordChar = !show.Checked;
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var open = new Button { Text = "APIキー作成ページを開く", Width = 175 };
        var save = new Button { Text = "保存", Width = 90 };
        var delete = new Button { Text = "保存キー削除", Width = 120 };
        var close = new Button { Text = "閉じる", Width = 90 };
        open.Click += (_, _) => Process.Start(new ProcessStartInfo { FileName = "https://platform.openai.com/api-keys", UseShellExecute = true });
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(keyBox.Text))
            {
                MessageBox.Show("APIキーを入力してください。");
                return;
            }
            WriteCredential(keyBox.Text.Trim());
            MessageBox.Show("APIキーをWindows資格情報に保存しました。");
            dialog.Close();
        };
        delete.Click += (_, _) =>
        {
            DeleteCredential();
            keyBox.Text = "";
            MessageBox.Show("保存済みAPIキーを削除しました。");
        };
        close.Click += (_, _) => dialog.Close();
        buttons.Controls.AddRange([open, save, delete, close]);
        layout.Controls.Add(info, 0, 0);
        layout.Controls.Add(keyBox, 0, 1);
        layout.Controls.Add(show, 0, 2);
        layout.Controls.Add(new Label
        {
            Text = "補足: API利用料金はキーを作成したOpenAIプロジェクトに課金されます。キーは他人に共有しないでください。",
            Dock = DockStyle.Fill
        }, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        dialog.Controls.Add(layout);
        dialog.ShowDialog(this);
    }

    private static string? GetOpenAiApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        return string.IsNullOrWhiteSpace(fromEnv) ? ReadCredential() : fromEnv;
    }

    private static string ExtractOutputText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (doc.RootElement.TryGetProperty("output_text", out var direct)) return direct.GetString() ?? "";
        var parts = new List<string>();
        FindTextParts(doc.RootElement, parts);
        return string.Join(Environment.NewLine, parts);
    }

    private static void FindTextParts(JsonElement element, List<string> parts)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                element.TryGetProperty("text", out var text))
            {
                parts.Add(text.GetString() ?? "");
            }
            foreach (var property in element.EnumerateObject()) FindTextParts(property.Value, parts);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) FindTextParts(item, parts);
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "";
    }

    private static string GetString(JsonElement root, string name, string fallback)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
    }

    private static int GetInt(JsonElement root, string name, int fallback)
    {
        return root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : fallback;
    }

    private static bool GetBool(JsonElement root, string name, bool fallback)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;
    }

    private static int ClampLv(int value) => Math.Max(0, Math.Min(9, value));

    private static OcrRunResult RunTesseract(string tesseract, string input, string outputBase)
    {
        var outFile = outputBase + ".txt";
        try
        {
            if (File.Exists(outFile)) File.Delete(outFile);
            var psi = new ProcessStartInfo
            {
                FileName = tesseract,
                Arguments = $"\"{input}\" \"{outputBase}\" -l jpn+eng --psm 6",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            if (process is null) return new(false, "OCRプロセスを開始できませんでした。");
            if (!process.WaitForExit(60000))
            {
                try { process.Kill(); } catch { }
                return new(false, "OCRが60秒以内に完了しませんでした。画像をトリミングするか、ゲーム画面を大きく表示して再撮影してください。");
            }

            var stderr = process.StandardError.ReadToEnd();
            var stdout = process.StandardOutput.ReadToEnd();
            if (process.ExitCode != 0)
            {
                return new(false, $"OCRに失敗しました。ExitCode={process.ExitCode}\r\n\r\n{stderr}\r\n{stdout}");
            }

            if (!File.Exists(outFile))
            {
                return new(false, $"OCR結果ファイルが作られませんでした。\r\n\r\n{stderr}\r\n{stdout}");
            }

            return new(true, File.ReadAllText(outFile, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            return new(false, "OCR実行中に例外が発生しました。\r\n" + ex.Message);
        }
    }

    private void ShowLargeLog()
    {
        using var dialog = new Form
        {
            Text = "OCRログ",
            Width = 900,
            Height = 620,
            StartPosition = FormStartPosition.CenterParent,
            Font = new Font("Yu Gothic UI", 11)
        };
        var log = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            Font = new Font("Yu Gothic UI", 12),
            Text = ocrText.Text
        };
        dialog.Controls.Add(log);
        dialog.ShowDialog(this);
    }

    private static string? FindTesseract()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tesseract.exe"),
            @"C:\Program Files\Tesseract-OCR\tesseract.exe",
            @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Tesseract-OCR\tesseract.exe")
        };
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            var candidate = Path.Combine(dir.Trim(), "tesseract.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private void CropSelectedEssenceCard(bool showMessage)
    {
        if (lastShot is null)
        {
            if (showMessage) MessageBox.Show("先に対象ウィンドウを撮影してください。");
            return;
        }

        // The selected essence card sits in the left middle of the Endfield essence-consume screen.
        // Ratios keep this useful across 16:9, 16:10, windowed, and scaled captures.
        var x = (int)(lastShot.Width * 0.065);
        var y = (int)(lastShot.Height * 0.350);
        var width = (int)(lastShot.Width * 0.245);
        var height = (int)(lastShot.Height * 0.285);
        var rect = Rectangle.Intersect(new Rectangle(x, y, width, height), new Rectangle(Point.Empty, lastShot.Size));
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            if (showMessage) MessageBox.Show("左カード領域を切り出せませんでした。");
            return;
        }

        selectedCardShot?.Dispose();
        selectedCardShot = lastShot.Clone(rect, PixelFormat.Format32bppArgb);
        cardPreview.Image = selectedCardShot;

        var path = Path.Combine(captureDir, $"selected_card_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        selectedCardShot.Save(path, ImageFormat.Png);
        if (showMessage) Text = $"エンドフィールド 基質餌チェック - 左カード抽出: {Path.GetFileName(path)}";
    }

    private void ExtractFromText(string text)
    {
        text = MakeOcrReadable(text ?? "");
        var clean = Regex.Replace(text ?? "", @"\s+", " ");
        var compact = Regex.Replace(text ?? "", @"\s+", "");
        var found = FindEffectsInText(clean, compact);
        SetSelectedEffects(found);
        if (ContainsAny(compact, "純粋基質", "純粋基質・切骨", "邏皮ｲ句渕雉ｪ")) { nameBox.Text = found.Contains("切骨") ? "純粋基質・切骨" : "純粋基質"; pure.Checked = true; }
        if (Regex.IsMatch(compact, "装備中|装着中|使用中|陬・ｙ荳ｭ|陬・捩荳ｭ|菴ｿ逕ｨ荳ｭ")) equipped.Checked = true;
        if (Regex.IsMatch(compact, "ロック|保護|LOCK|繝ｭ繝・け|菫晁ｭｷ", RegexOptions.IgnoreCase)) locked.Checked = true;
        var rarity = Regex.Match(compact, @"([1-5])[★☆]");
        if (rarity.Success) rarityBox.SelectedIndex = 5 - int.Parse(rarity.Groups[1].Value);
        var slash = Regex.Match(compact, @"([0-9])/9.*?([0-9])/9.*?([0-9])/9");
        if (slash.Success)
        {
            lv1.Value = int.Parse(slash.Groups[1].Value);
            lv2.Value = int.Parse(slash.Groups[2].Value);
            lv3.Value = int.Parse(slash.Groups[3].Value);
        }
        else
        {
            var plus = Regex.Matches(compact, @"[+＋]([0-9])").Select(match => int.Parse(match.Groups[1].Value)).Take(3).ToList();
            if (plus.Count > 0) lv1.Value = plus[0];
            if (plus.Count > 1) lv2.Value = plus[1];
            if (plus.Count > 2) lv3.Value = plus[2];
        }
        UpdateEffectButtonState();
        ShowJudgement();
    }

    private static string MakeOcrReadable(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var readable = text;
        foreach (var (effect, aliases) in EffectAliases)
        {
            if (ContainsAny(readable, effect)) continue;
            foreach (var alias in aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    readable = readable.Replace(alias, effect, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        readable = readable.Replace("邏皮ｲ句渕雉ｪ", "純粋基質", StringComparison.OrdinalIgnoreCase);
        readable = readable.Replace("繝ｭ繝・け", "ロック", StringComparison.OrdinalIgnoreCase);
        readable = readable.Replace("菫晁ｭｷ", "保護", StringComparison.OrdinalIgnoreCase);
        readable = readable.Replace("陬・ｙ荳ｭ", "装備中", StringComparison.OrdinalIgnoreCase);
        readable = readable.Replace("菴ｿ逕ｨ荳ｭ", "使用中", StringComparison.OrdinalIgnoreCase);
        return readable;
    }

    private static List<string> FindEffectsInText(string clean, string compact)
    {
        var found = new List<string>();
        foreach (var effect in Effects)
        {
            var aliases = EffectAliases.TryGetValue(effect, out var values) ? values : [];
            if (ContainsAny(clean, effect) || ContainsAny(compact, effect) ||
                aliases.Any(alias => ContainsAny(clean, alias) || ContainsAny(compact, alias)))
            {
                found.Add(effect);
            }
        }
        return found.Take(3).ToList();
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        var normalizedText = NormalizeSearchText(text);
        return values.Any(value => !string.IsNullOrWhiteSpace(value) &&
            normalizedText.Contains(NormalizeSearchText(value), StringComparison.Ordinal));
    }

    private void ShowJudgement()
    {
        var item = CurrentItem();
        var result = Judge(item);
        verdict.Text = result.Label;
        verdict.ForeColor = result.Kind switch
        {
            "safe" => Color.FromArgb(95, 211, 155),
            "stop" => Color.FromArgb(239, 112, 101),
            "material" => Color.FromArgb(148, 171, 255),
            _ => Color.FromArgb(229, 189, 88)
        };
        UpdateMatchingWeapons(item);
        reasons.Text = string.Join(Environment.NewLine, result.Reasons.Take(2).Select(r => "・" + r));
    }

    private void UpdateMatchingWeapons(Essence item)
    {
        matchingWeapons.Rows.Clear();
        var matches = SearchTargetRows()
            .Select(t => new { Target = t, Count = MatchCount(item, t) })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Target.Name)
            .ToList();

        foreach (var match in matches)
        {
            var rowIndex = matchingWeapons.Rows.Add($"{match.Count}/3", match.Target.Name, match.Target.Base, match.Target.Sub, match.Target.Skill);
            PaintMatchRow(matchingWeapons.Rows[rowIndex], item, match.Target, match.Count);
        }

        if (matches.Count == 0)
        {
            matchingWeapons.Rows.Add("-", "一致する登録武器なし", "", "", "");
        }

        var hasExact = matches.Any(x => x.Count == 3);
        feedStatus.Text = hasExact ? "完全一致あり: 餌にしない" : "完全一致なし: 餌にしてOK";
        feedStatus.ForeColor = hasExact ? Color.FromArgb(239, 112, 101) : Color.FromArgb(95, 211, 155);
    }

    private void ConfigureMatchGrid()
    {
        matchingWeapons.Columns.Clear();
        matchingWeapons.Columns.Add("Match", "一致");
        matchingWeapons.Columns.Add("Weapon", "武器");
        matchingWeapons.Columns.Add("Base", "基質1");
        matchingWeapons.Columns.Add("Sub", "基質2");
        matchingWeapons.Columns.Add("Skill", "基質3");
        matchingWeapons.Columns[0].FillWeight = 42;
        matchingWeapons.Columns[1].FillWeight = 115;
        matchingWeapons.Columns[2].FillWeight = 78;
        matchingWeapons.Columns[3].FillWeight = 78;
        matchingWeapons.Columns[4].FillWeight = 78;
        matchingWeapons.RowTemplate.Height = 28;
    }

    private void ConfigureTargetsGrid()
    {
        targets.Columns.Clear();
        targets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Weapon", HeaderText = "武器名", FillWeight = 125 });
        targets.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Excluded", HeaderText = "入手済み", FillWeight = 48 });
        targets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Base", HeaderText = "基質1", FillWeight = 100 });
        targets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sub", HeaderText = "基質2", FillWeight = 100 });
        targets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Skill", HeaderText = "基質3", FillWeight = 100 });
    }

    private static void PaintMatchRow(DataGridViewRow row, Essence item, Target target, int count)
    {
        row.DefaultCellStyle.BackColor = count == 3 ? Color.FromArgb(19, 45, 34) : Color.FromArgb(14, 18, 23);
        row.Cells[0].Style.ForeColor = count == 3 ? Color.FromArgb(116, 232, 171) : TextMuted;
        row.Cells[0].Style.Font = new Font("Yu Gothic UI", 9, FontStyle.Bold);
        PaintMatchCell(row.Cells[2], EffectEquals(item.Base, target.Base));
        PaintMatchCell(row.Cells[3], EffectEquals(item.Sub, target.Sub));
        PaintMatchCell(row.Cells[4], EffectEquals(item.Skill, target.Skill));
    }

    private static void PaintMatchCell(DataGridViewCell cell, bool matched)
    {
        if (!matched) return;
        cell.Style.BackColor = Color.FromArgb(52, 122, 91);
        cell.Style.ForeColor = Color.White;
        cell.Style.Font = new Font("Yu Gothic UI", 9, FontStyle.Bold);
    }

    private Essence CurrentItem() => new()
    {
        Name = string.IsNullOrWhiteSpace(nameBox.Text) ? "未命名基質" : nameBox.Text.Trim(),
        Rarity = 5 - rarityBox.SelectedIndex,
        Base = NormalizeEffectLabel(baseBox.Text),
        Sub = NormalizeEffectLabel(subBox.Text),
        Skill = NormalizeEffectLabel(skillBox.Text),
        Lv1 = (int)lv1.Value,
        Lv2 = (int)lv2.Value,
        Lv3 = (int)lv3.Value,
        Equipped = equipped.Checked,
        Locked = locked.Checked,
        Pure = pure.Checked,
        Duplicate = duplicate.Checked
    };

    private JudgeResult Judge(Essence item)
    {
        var reasons = new List<string>();
        var best = SearchTargetRows()
            .Select(t => new { Target = t, Count = MatchCount(item, t) })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        if (item.Locked) reasons.Add("ロック済みなので捨てない");
        if (item.Locked) return new("stop", "捨てない", reasons);

        if (best?.Count == 3)
        {
            reasons.Add($"{best.Target.Name} の目標基質と3効果一致");
            return new("stop", "捨てない", reasons);
        }

        if (best?.Count == 2) reasons.Add($"{best.Target.Name} と2効果一致。ただし完全一致ではありません");
        if (item.Pure) reasons.Add("純粋基質です");
        if (item.Duplicate) reasons.Add("同効果の上位/予備あり");
        reasons.Add(best is null ? "一致する登録武器なし" : $"最大一致は{best.Count}/3");
        reasons.Add("完全一致する目標武器がないため、餌にしてOK");
        return new("safe", "餌にしてOK", reasons);
    }

    private static int MatchCount(Essence item, Target target)
    {
        var count = 0;
        if (EffectEquals(item.Base, target.Base)) count++;
        if (EffectEquals(item.Sub, target.Sub)) count++;
        if (EffectEquals(item.Skill, target.Skill)) count++;
        return count;
    }

    private IEnumerable<Target> TargetRows()
    {
        foreach (DataGridViewRow row in targets.Rows)
        {
            if (row.IsNewRow) continue;
            var name = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
            var excluded = ToBool(row.Cells[1].Value);
            var b = NormalizeEffectLabel(Convert.ToString(row.Cells[2].Value) ?? "");
            var s = NormalizeEffectLabel(Convert.ToString(row.Cells[3].Value) ?? "");
            var k = NormalizeEffectLabel(Convert.ToString(row.Cells[4].Value) ?? "");
            if (name.Length + b.Length + s.Length + k.Length == 0) continue;
            yield return new Target(name.Length == 0 ? "未命名武器" : name, b, s, k, excluded);
        }
    }

    private IEnumerable<Target> SearchTargetRows() => TargetRows().Where(target => !target.Excluded);

    private static bool ToBool(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var parsed) && parsed,
        _ => false
    };

    private void SaveTargets()
    {
        targets.EndEdit();
        var json = JsonSerializer.Serialize(TargetRows().ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dataPath, json, Encoding.UTF8);
        Text = "エンドフィールド 基質餌チェック - 目標保存済み";
    }

    private void LoadTargets()
    {
        if (!File.Exists(dataPath)) return;
        var loaded = JsonSerializer.Deserialize<List<Target>>(File.ReadAllText(dataPath, Encoding.UTF8)) ?? [];
        targets.Rows.Clear();
        foreach (var target in loaded)
        {
            targets.Rows.Add(
                target.Name,
                target.Excluded,
                NormalizeEffectLabel(target.Base),
                NormalizeEffectLabel(target.Sub),
                NormalizeEffectLabel(target.Skill));
        }
    }

    private void ShowTargetsDialog()
    {
        using var dialog = new Form
        {
            Text = "武器基質を管理",
            Width = Math.Min(960, Screen.FromControl(this).WorkingArea.Width - 80),
            Height = Math.Min(620, Screen.FromControl(this).WorkingArea.Height - 120),
            StartPosition = FormStartPosition.CenterParent,
            Font = Font,
            BackColor = Bg,
            ForeColor = TextMain
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var host = new Panel { Dock = DockStyle.Fill };
        targets.Parent?.Controls.Remove(targets);
        host.Controls.Add(targets);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var close = new Button { Text = "閉じる", Width = 90 };
        var save = new Button { Text = "保存", Width = 90 };
        var sync = new Button { Text = "サイト基準を取得", Width = 130 };
        close.Click += (_, _) => dialog.Close();
        save.Click += (_, _) =>
        {
            SaveTargets();
            ShowJudgement();
        };
        sync.Click += async (_, _) => await SyncTargetsFromEndfieldTools();
        buttons.Controls.AddRange([close, save, sync]);

        layout.Controls.Add(host, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        dialog.Controls.Add(layout);
        ApplyTheme(dialog);
        dialog.FormClosed += (_, _) =>
        {
            host.Controls.Remove(targets);
        };
        dialog.ShowDialog(this);
    }

    private async Task SyncTargetsFromEndfieldTools()
    {
        const string url = "https://endfieldtools.dev/localdb/optimized/weapons/weapons-list.json";
        const string i18nUrl = "https://endfieldtools.dev/localdb/optimized/i18n/I18nTextTable_JP.json";
        targets.EndEdit();
        var excludedNames = TargetRows()
            .Where(target => target.Excluded)
            .Select(target => target.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        try
        {
            Text = "エンドフィールド 基質餌チェック - EndfieldTools.DEVから取得中...";
            var json = await ReadUtf8Url(url);
            var i18nJson = await ReadUtf8Url(i18nUrl);
            var i18n = LoadI18nMap(i18nJson);
            File.WriteAllText(siteCachePath, json, Encoding.UTF8);
            File.WriteAllText(siteI18nCachePath, i18nJson, Encoding.UTF8);
            var imported = ParseEndfieldToolsTargets(json, i18n).ToList();
            if (imported.Count == 0)
            {
                MessageBox.Show("EndfieldTools.DEVから武器基質を読み取れませんでした。");
                return;
            }

            targets.Rows.Clear();
            foreach (var target in imported)
            {
                targets.Rows.Add(target.Name, excludedNames.Contains(target.Name), target.Base, target.Sub, target.Skill);
            }
            SaveTargets();
            ShowJudgement();
            ocrText.Text = $"EndfieldTools.DEV基準データを取得しました。\r\n武器: {imported.Count}件\r\n取得元: {url}";
        }
        catch (Exception ex)
        {
            if (File.Exists(siteCachePath))
            {
                try
                {
                    var i18n = File.Exists(siteI18nCachePath)
                        ? LoadI18nMap(File.ReadAllText(siteI18nCachePath, Encoding.UTF8))
                        : new Dictionary<string, string>();
                    var cached = ParseEndfieldToolsTargets(File.ReadAllText(siteCachePath, Encoding.UTF8), i18n).ToList();
                    if (cached.Count > 0)
                    {
                        targets.Rows.Clear();
                        foreach (var target in cached) targets.Rows.Add(target.Name, excludedNames.Contains(target.Name), target.Base, target.Sub, target.Skill);
                        SaveTargets();
                        ShowJudgement();
                        MessageBox.Show($"サイト取得に失敗したため、前回キャッシュを使いました。\r\n武器: {cached.Count}件\r\n\r\n{ex.Message}");
                        return;
                    }
                }
                catch
                {
                    // Fall through to the original error.
                }
            }

            MessageBox.Show("EndfieldTools.DEVの取得に失敗しました。\r\n\r\n" + ex.Message);
        }
    }

    private async Task<string> ReadUtf8Url(string url)
    {
        var bytes = await http.GetByteArrayAsync(url);
        return Encoding.UTF8.GetString(bytes);
    }

    private static Dictionary<string, string> LoadI18nMap(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<Target> ParseEndfieldToolsTargets(string json, IReadOnlyDictionary<string, string>? i18n = null)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var weapon = property.Value;
            var name = GetWeaponDisplayName(weapon, property.Name, i18n);

            var tags = new List<string>();
            if (weapon.TryGetProperty("baseStats", out var baseStats) && baseStats.ValueKind == JsonValueKind.Array)
            {
                foreach (var stat in baseStats.EnumerateArray().Take(2))
                {
                    var tag = GetJsonString(stat, "tagId");
                    AddUnique(tags, SiteTagToLabel(tag));
                }
            }

            if (weapon.TryGetProperty("passiveSkills", out var passives) && passives.ValueKind == JsonValueKind.Array)
            {
                foreach (var skill in passives.EnumerateArray())
                {
                    if (tags.Count >= 3) break;
                    AddUnique(tags, PassiveSkillToLabel(skill, i18n));
                }
            }

            tags = tags.Where(value => value.Length > 0).Take(3).ToList();
            if (tags.Count >= 2)
            {
                yield return TargetFromLabels(name, tags);
            }
        }
    }

    private static Target TargetFromLabels(string name, IReadOnlyList<string> labels)
    {
        var primary = FirstInCategory(labels, PrimaryEffects);
        var secondary = FirstInCategory(labels, SecondaryEffects);
        var skill = FirstInCategory(labels, SkillEffects);
        return new Target(name, primary, secondary, skill);
    }

    private static string GetWeaponDisplayName(JsonElement weapon, string fallback, IReadOnlyDictionary<string, string>? i18n)
    {
        var englishName = GetTextObjectValue(weapon, "engName", fallback);
        var nameI18nId = GetJsonString(weapon, "nameI18nId");
        if (string.IsNullOrWhiteSpace(nameI18nId) && weapon.TryGetProperty("engName", out var engName))
        {
            nameI18nId = GetJsonString(engName, "id");
        }

        if (!string.IsNullOrWhiteSpace(nameI18nId) &&
            i18n is not null &&
            i18n.TryGetValue(nameI18nId, out var translated) &&
            !string.IsNullOrWhiteSpace(translated))
        {
            return CleanI18nText(translated);
        }

        return string.IsNullOrWhiteSpace(englishName) ? fallback : englishName;
    }

    private static string CleanI18nText(string text)
    {
        return Regex.Replace(text, @"<[^>]+>", "").Trim();
    }

    private static string SiteTagToLabel(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return "";
        if (SiteTagNames.TryGetValue(tag, out var label)) return label;
        return tag.Replace("attr_", "", StringComparison.OrdinalIgnoreCase).Replace("_", " ").Trim();
    }

    private static string BlackboardTagToLabel(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        return BlackboardTagNames.TryGetValue(key.Trim(), out var label) ? label : "";
    }

    private static string PassiveSkillToLabel(JsonElement skill, IReadOnlyDictionary<string, string>? i18n)
    {
        var skillNameId = GetJsonString(skill, "skillNameId");
        if (!string.IsNullOrWhiteSpace(skillNameId) &&
            i18n is not null &&
            i18n.TryGetValue(skillNameId, out var translated) &&
            !string.IsNullOrWhiteSpace(translated))
        {
            var prefix = CleanI18nText(translated).Split('・', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            var effect = NormalizeEffectLabel(prefix);
            if (Effects.Contains(effect)) return effect;
        }

        return SiteTagToLabel(GetJsonString(skill, "tagId"));
    }

    private static void AddUnique(List<string> values, string value)
    {
        value = NormalizeEffectLabel(value);
        if (value.Length == 0 || values.Contains(value)) return;
        values.Add(value);
    }

    private static bool EffectEquals(string left, string right)
    {
        left = NormalizeEffectLabel(left);
        right = NormalizeEffectLabel(right);
        return left.Length > 0 && left == right;
    }

    private static string NormalizeEffectLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var raw = value.Trim();
        var rawCompact = NormalizeSearchText(raw);
        foreach (var effect in Effects)
        {
            if (EffectTextEquals(raw, rawCompact, effect)) return effect;
            if (!EffectAliases.TryGetValue(effect, out var aliases)) continue;
            if (aliases.Any(alias => EffectTextEquals(raw, rawCompact, alias))) return effect;
        }

        var contained = FindContainedEffect(raw, rawCompact);
        if (contained.Length > 0) return contained;

        var readable = MakeOcrReadable(raw).Trim();
        var compact = NormalizeSearchText(readable);

        foreach (var effect in Effects)
        {
            if (EffectTextEquals(readable, compact, effect)) return effect;
            if (!EffectAliases.TryGetValue(effect, out var aliases)) continue;
            if (aliases.Any(alias => EffectTextEquals(readable, compact, alias))) return effect;
        }

        return readable;
    }

    private static string FindContainedEffect(string readable, string compact)
    {
        foreach (var effect in Effects.OrderByDescending(effect => NormalizeSearchText(effect).Length))
        {
            if (ContainsEffectText(readable, compact, effect)) return effect;
            if (!EffectAliases.TryGetValue(effect, out var aliases)) continue;
            if (aliases.Any(alias => ContainsEffectText(readable, compact, alias))) return effect;
        }
        return "";
    }

    private static bool ContainsEffectText(string readable, string compact, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalizedValue = NormalizeSearchText(value);
        return NormalizeSearchText(readable).Contains(normalizedValue, StringComparison.Ordinal) ||
            NormalizeSearchText(compact).Contains(normalizedValue, StringComparison.Ordinal);
    }

    private static bool EffectTextEquals(string readable, string compact, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalizedValue = NormalizeSearchText(value);
        return NormalizeSearchText(readable) == normalizedValue ||
            NormalizeSearchText(compact) == normalizedValue;
    }

    private static string NormalizeSearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return Regex.Replace(value, @"\s+", "").ToLowerInvariant();
    }

    private static string GetTextObjectValue(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var property)) return fallback;
        if (property.ValueKind == JsonValueKind.String) return property.GetString() ?? fallback;
        if (property.ValueKind == JsonValueKind.Object && property.TryGetProperty("text", out var text)) return text.GetString() ?? fallback;
        return fallback;
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static void ReleaseIfNeeded(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero) Marshal.Release(ptr);
    }

    [ComImport]
    [Guid("3628e81b-3cac-4c60-b7f4-23ce0e0c3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        [PreserveSig]
        int CreateForWindow(IntPtr window, ref Guid iid, out IntPtr result);

        [PreserveSig]
        int CreateForMonitor(IntPtr monitor, ref Guid iid, out IntPtr result);
    }

    [ComImport]
    [Guid("a9b3d012-3df2-4ee3-b8d1-8695f457d3c1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface(ref Guid iid);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_TEXTURE2D_DESC
    {
        public int Width;
        public int Height;
        public uint MipLevels;
        public uint ArraySize;
        public uint Format;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint Usage;
        public uint BindFlags;
        public uint CPUAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_MAPPED_SUBRESOURCE
    {
        public IntPtr DataPointer;
        public uint RowPitch;
        public uint DepthPitch;
    }

    private static class D3D11Texture2D
    {
        private delegate void GetDescDelegate(IntPtr self, out D3D11_TEXTURE2D_DESC desc);

        public static D3D11_TEXTURE2D_DESC GetDesc(IntPtr texture)
        {
            var method = GetMethod<GetDescDelegate>(texture, 10);
            method(texture, out var desc);
            return desc;
        }
    }

    private static class D3D11Device
    {
        private delegate int CreateTexture2DDelegate(IntPtr self, ref D3D11_TEXTURE2D_DESC desc, IntPtr initialData, out IntPtr texture);

        public static int CreateTexture2D(IntPtr device, ref D3D11_TEXTURE2D_DESC desc, IntPtr initialData, out IntPtr texture)
        {
            var method = GetMethod<CreateTexture2DDelegate>(device, 5);
            return method(device, ref desc, initialData, out texture);
        }
    }

    private static class D3D11DeviceContext
    {
        private delegate int MapDelegate(IntPtr self, IntPtr resource, uint subresource, uint mapType, uint mapFlags, out D3D11_MAPPED_SUBRESOURCE mappedResource);
        private delegate void UnmapDelegate(IntPtr self, IntPtr resource, uint subresource);
        private delegate void CopyResourceDelegate(IntPtr self, IntPtr destinationResource, IntPtr sourceResource);

        public static int Map(IntPtr context, IntPtr resource, uint subresource, uint mapType, uint mapFlags, out D3D11_MAPPED_SUBRESOURCE mappedResource)
        {
            var method = GetMethod<MapDelegate>(context, 14);
            return method(context, resource, subresource, mapType, mapFlags, out mappedResource);
        }

        public static void Unmap(IntPtr context, IntPtr resource, uint subresource)
        {
            var method = GetMethod<UnmapDelegate>(context, 15);
            method(context, resource, subresource);
        }

        public static void CopyResource(IntPtr context, IntPtr destinationResource, IntPtr sourceResource)
        {
            var method = GetMethod<CopyResourceDelegate>(context, 47);
            method(context, destinationResource, sourceResource);
        }
    }

    private static TDelegate GetMethod<TDelegate>(IntPtr comObject, int slot) where TDelegate : Delegate
    {
        var vtbl = Marshal.ReadIntPtr(comObject);
        var methodPtr = Marshal.ReadIntPtr(vtbl, slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(methodPtr);
    }

    private sealed record Essence
    {
        public string Name { get; init; } = "";
        public int Rarity { get; init; } = 5;
        public string Base { get; init; } = "";
        public string Sub { get; init; } = "";
        public string Skill { get; init; } = "";
        public int Lv1 { get; init; }
        public int Lv2 { get; init; }
        public int Lv3 { get; init; }
        public bool Equipped { get; init; }
        public bool Locked { get; init; }
        public bool Pure { get; init; }
        public bool Duplicate { get; init; }
    }

    private sealed record EffectChoiceButton(ComboBox TargetBox, string Effect, Button Button);
    private sealed record Target(string Name, string Base, string Sub, string Skill, bool Excluded = false);
    private sealed record JudgeResult(string Kind, string Label, List<string> Reasons);
    private sealed record OcrRunResult(bool Success, string Message);
    private sealed record WindowItem(int ProcessId, string ProcessName, string Title, IntPtr Handle)
    {
        public string DisplayName => $"{ProcessName}.exe [{ProcessId}] - {Title}";
    }

    private const int SW_RESTORE = 9;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint PW_RENDERFULLCONTENT = 0x00000002;
    private const uint D3D11_SDK_VERSION = 7;
    private const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
    private const uint D3D_DRIVER_TYPE_HARDWARE = 1;
    private const uint D3D11_USAGE_STAGING = 3;
    private const uint D3D11_CPU_ACCESS_READ = 0x20000;
    private const uint D3D11_MAP_READ = 1;
    private static readonly Guid IID_IDXGIDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private static readonly Guid IID_ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    private static readonly Guid IID_IGraphicsCaptureItemInterop = new("3628e81b-3cac-4c60-b7f4-23ce0e0c3356");
    private static readonly Guid IID_IGraphicsCaptureItem = new("79c3f95b-31f7-4ec2-a464-632ef5d30760");
    private const string CredentialTarget = "EndfieldEssenceChecker/OpenAI";
    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(
        IntPtr pAdapter,
        uint driverType,
        IntPtr software,
        uint flags,
        uint[]? featureLevels,
        uint featureLevelsCount,
        uint sdkVersion,
        out IntPtr device,
        out uint featureLevel,
        out IntPtr immediateContext);

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [DllImport("combase.dll")]
    private static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        int length,
        out IntPtr hstring);

    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
    private static extern void CopyMemory(IntPtr destination, IntPtr source, nuint length);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    private static void WriteCredential(string secret)
    {
        var secretBytes = Encoding.Unicode.GetBytes(secret);
        var secretPtr = Marshal.AllocHGlobal(secretBytes.Length);
        try
        {
            Marshal.Copy(secretBytes, 0, secretPtr, secretBytes.Length);
            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = CredentialTarget,
                CredentialBlobSize = (uint)secretBytes.Length,
                CredentialBlob = secretPtr,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = Environment.UserName
            };
            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException($"Windows資格情報への保存に失敗しました。Win32Error={Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.Copy(new byte[secretBytes.Length], 0, secretPtr, secretBytes.Length);
            Marshal.FreeHGlobal(secretPtr);
        }
    }

    private static string? ReadCredential()
    {
        if (!CredRead(CredentialTarget, CRED_TYPE_GENERIC, 0, out var credentialPtr)) return null;
        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) return null;
            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    private static void DeleteCredential()
    {
        CredDelete(CredentialTarget, CRED_TYPE_GENERIC, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}
