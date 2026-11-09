using TarotCardsDeck.Assets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace TarotCardsDeck
{
    public partial class frmTarot : Form
    {
        public Deck deck, drawn;
        PictureBox[] pcbDeck = new PictureBox[78];
        double dblScrHgtRat = 1;
        double dblScrWidRat = 1;
        int intCurPage, intMaxPage = 0;
        private ComboBox deckSelector;
        private ComboBox artworkSelector;
        private ComboBox cardBackSelector;
        private Button creditsButton;
        private Button settingsButton;
        private Panel settingsPanel;
        private Panel drawnPanel;
        private Panel creditsPanel;
        private readonly Dictionary<Panel, Form> overlayWindows = new Dictionary<Panel, Form>();
        private TextBox creditsText;
        private CheckBox autoSpreadCheckBox;
        private string artworkCredits;
        private bool loadingSelections;
        private Bitmap deckBackImage;
        private readonly Dictionary<int, Bitmap> shufflePackets = new Dictionary<int, Bitmap>();
        private FanSurface fanSurface;
        private double fanProgress;
        private double dragStartProgress;
        private Point dragStart;
        private bool fanDragging;
        private bool fanMoved;
        private int pressedFanCard = -1;
        private Timer spreadRevealTimer;
        private Timer shufflePileTimer;
        private DateTime shuffleStartedAtUtc;
        private Dictionary<string, string> activeArtworkFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private DeckChoice selectedDeck = DeckChoice.CompleteTarot;

        public frmTarot()
        {
            InitializeComponent();
            foreach (PictureBox card in new[] { pcbCardFirst, pcbCard01, pcbCard02, pcbCard03, pcbCardLast })
                card.Paint += PaintDrawnCardBorder;
            FormClosed += frmTarot_FormClosed;
        }

        private void PaintDrawnCardBorder(object sender, PaintEventArgs e)
        {
            PictureBox card = (PictureBox)sender;
            if (card.Image == null) return;
            int thickness = Math.Max(1, (int)Math.Round(2.0 * card.DeviceDpi / 96));
            Color border = Color.FromArgb(224, 213, 190);
            ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                border, thickness, ButtonBorderStyle.Solid,
                border, thickness, ButtonBorderStyle.Solid,
                border, thickness, ButtonBorderStyle.Solid,
                border, thickness, ButtonBorderStyle.Solid);
        }

        private void frmTarot_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (spreadRevealTimer != null)
            {
                spreadRevealTimer.Stop();
                spreadRevealTimer.Dispose();
                spreadRevealTimer = null;
            }
            if (shufflePileTimer != null)
            {
                shufflePileTimer.Stop();
                shufflePileTimer.Dispose();
                shufflePileTimer = null;
            }
            RemoveDeck();
            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);
            SetPictureImage(pcbCardFirst, null);
            SetPictureImage(pcbCardLast, null);
            foreach (Form overlay in overlayWindows.Values) overlay.Dispose();
            overlayWindows.Clear();
        }

        private void frmTarot_Load(object sender, EventArgs e)
        {
            loadingSelections = true;
            settingsPanel = new Panel { Location = new Point(20, 48), Size = new Size(430, 230),
                BackColor = Color.FromArgb(35, 35, 35), BorderStyle = BorderStyle.FixedSingle, Visible = false };
            Controls.Add(settingsPanel);
            settingsButton = new Button { Text = "Settings", Location = new Point(20, 12),
                Size = new Size(110, 28), UseVisualStyleBackColor = true };
            settingsButton.Click += (s, args) =>
            {
                if (settingsPanel.Visible) { overlayWindows[settingsPanel].Activate(); return; }
                ResetPendingSettings();
                if (creditsPanel != null) HideOverlay(creditsPanel);
                PositionSettings();
                ShowOverlay(settingsPanel);
            };
            Controls.Add(settingsButton);
            deckSelector = new ComboBox();
            deckSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            deckSelector.Items.AddRange(new object[] { "Complete Tarot (78 cards)", "Major Arcana (22 cards)", "Minor Arcana (56 cards)" });
            deckSelector.SelectedIndex = Math.Max(0, Math.Min(2, TarotDeck.Properties.Settings.Default.CardSet));
            deckSelector.Location = new Point(20, 24);
            deckSelector.Width = 210;
            deckSelector.SelectedIndexChanged += DeckSelector_SelectedIndexChanged;
            deckSelector.Visible = false;
            settingsPanel.Controls.Add(deckSelector);
            deckSelector.BringToFront();

            artworkSelector = new ComboBox();
            artworkSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            artworkSelector.Location = new Point(20, 40);
            artworkSelector.Width = 260;
            artworkSelector.SelectedIndexChanged += ArtworkSelector_SelectedIndexChanged;
            settingsPanel.Controls.Add(artworkSelector);

            Label artworkLabel = new Label();
            artworkLabel.AutoSize = true;
            artworkLabel.ForeColor = Color.White;
            artworkLabel.Text = "Artwork deck";
            artworkLabel.Location = new Point(20, 20);
            settingsPanel.Controls.Add(artworkLabel);

            settingsPanel.Controls.Add(new Label { Text = "Card back", AutoSize = true,
                ForeColor = Color.White, Location = new Point(20, 80) });
            cardBackSelector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 100), Width = 260 };
            string backFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "cardbacks");
            string[] backNames = { "Celestial Garden", "Moonlit Lotus", "Emerald Labyrinth", "Rosewood Mandala",
                "Sapphire Tides", "Obsidian Eclipse", "Blush Peony", "Lavender Dream", "Ivory Astrolabe", "Teal Phoenix",
                "Mauryan Lotus", "Gupta Bloom", "Chola Bronze", "Mughal Inlay", "Indigo Shibori", "Persian Starlight", "Nordic Frost",
                "Desert Mosaic", "Pearl Lace", "Cosmic Aurora", "Obsidian Geometry" };
            foreach (string name in backNames.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase))
                cardBackSelector.Items.Add(new ArtworkChoice(name, Path.Combine(backFolder,
                    name.ToLowerInvariant().Replace(' ', '-') + ".png")));
            SelectCardBack(TarotDeck.Properties.Settings.Default.CardBackName);
            settingsPanel.Controls.Add(cardBackSelector);
            autoSpreadCheckBox = new CheckBox { Text = "Auto spread after shuffle", AutoSize = true,
                ForeColor = Color.White, Location = new Point(20, 140),
                Checked = TarotDeck.Properties.Settings.Default.AutoSpread };
            settingsPanel.Controls.Add(autoSpreadCheckBox);
            int settingsActionsLeft = settingsPanel.ClientSize.Width - 200;
            Button applySettings = new Button { Text = "Apply", Location = new Point(settingsActionsLeft, 183),
                Size = new Size(80, 27), UseVisualStyleBackColor = true };
            Button cancelSettings = new Button { Text = "Cancel", Location = new Point(settingsActionsLeft + 100, 183),
                Size = new Size(80, 27), UseVisualStyleBackColor = true };
            applySettings.Click += ApplySettings_Click;
            cancelSettings.Click += (s, args) => { ResetPendingSettings(); HideOverlay(settingsPanel); };
            settingsPanel.Controls.Add(applySettings);
            settingsPanel.Controls.Add(cancelSettings);
            Action alignSettingsActions = () =>
            {
                int left = settingsPanel.ClientSize.Width - applySettings.Width - cancelSettings.Width - 40;
                applySettings.Left = left;
                cancelSettings.Left = left + applySettings.Width + 20;
                applySettings.Top = cancelSettings.Top = settingsPanel.ClientSize.Height - applySettings.Height - 20;
            };
            settingsPanel.ClientSizeChanged += (s, args) => alignSettingsActions();
            settingsPanel.HandleCreated += (s, args) => alignSettingsActions();
            alignSettingsActions();

            creditsButton = new Button();
            creditsButton.Text = "Credits";
            creditsButton.Location = new Point(140, 12);
            creditsButton.Size = new Size(85, 28);
            creditsButton.UseVisualStyleBackColor = true;
            creditsButton.Click += CreditsButton_Click;
            Controls.Add(creditsButton);

            PopulateArtworkChoices(TarotDeck.Properties.Settings.Default.ArtworkName);
            RefreshArtworkFileIndex();
            loadingSelections = false;
            InitializeReadings();
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            StartNewDeck();
            SetScreenRatio();
            PositionSettings();
            ClientSizeChanged += (s, args) => { LayoutDrawnCards(); PositionSettings(); };
            LocationChanged += (s, args) => PositionSettings();
            Shown += (s, args) => BeginInvoke((MethodInvoker)(() => { LayoutDrawnCards(); PositionSettings(); }));
            Shuffle();
            SpreadDeck();
        }

        private void DeckSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSelections || settingsPanel.Visible) return;
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            SaveSelection();
            StartNewDeck();
            Shuffle();
            SpreadDeck();
        }

        private void ArtworkSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSelections || settingsPanel.Visible || artworkSelector.SelectedIndex < 0) return;
            RefreshArtworkFileIndex();
            SaveSelection();
            StartNewDeck();
            Shuffle();
            SpreadDeck();
        }

        private void SaveSelection()
        {
            TarotDeck.Properties.Settings.Default.CardSet = deckSelector.SelectedIndex;
            TarotDeck.Properties.Settings.Default.ArtworkName = ((ArtworkChoice)artworkSelector.SelectedItem).Name;
            TarotDeck.Properties.Settings.Default.Save();
        }

        private void PositionSettings()
        {
            if (settingsButton == null || creditsButton == null) return;
            int margin = 20;
            Rectangle available = Rectangle.Intersect(ClientRectangle,
                RectangleToClient(Screen.FromControl(this).WorkingArea));
            if (available.Width <= 0 || available.Height <= 0) return;
            settingsButton.Location = new Point(available.Left + margin,
                Math.Max(available.Top, available.Bottom - settingsButton.Height - margin));
            creditsButton.Location = new Point(settingsButton.Right + 10,
                settingsButton.Top);
            PositionReadingControls();
            PositionOverlay(settingsPanel, new Rectangle(
                new Point(available.Left + margin,
                    Math.Max(available.Top + margin, settingsButton.Top - settingsPanel.Height - 12)),
                settingsPanel.Size));
            if (creditsPanel != null)
            {
                creditsPanel.Size = new Size(Math.Min(520, Math.Max(240, available.Width - margin * 2)),
                    Math.Min(260, Math.Max(160, settingsButton.Top - available.Top - margin * 2)));
                PositionOverlay(creditsPanel, new Rectangle(
                    new Point(available.Left + margin,
                        Math.Max(available.Top + margin, settingsButton.Top - creditsPanel.Height - 12)),
                    creditsPanel.Size));
            }
        }

        private void ResetPendingSettings()
        {
            loadingSelections = true;
            deckSelector.SelectedIndex = (int)selectedDeck;
            string activeName = TarotDeck.Properties.Settings.Default.ArtworkName;
            for (int i = 0; i < artworkSelector.Items.Count; i++)
                if (((ArtworkChoice)artworkSelector.Items[i]).Name == activeName)
                    artworkSelector.SelectedIndex = i;
            autoSpreadCheckBox.Checked = TarotDeck.Properties.Settings.Default.AutoSpread;
            SelectCardBack(TarotDeck.Properties.Settings.Default.CardBackName);
            ResetPendingReadingSettings();
            loadingSelections = false;
        }

        private void SelectCardBack(string name)
        {
            cardBackSelector.SelectedIndex = cardBackSelector.Items.Cast<ArtworkChoice>().ToList()
                .FindIndex(item => item.Name == "Emerald Labyrinth");
            for (int i = 0; i < cardBackSelector.Items.Count; i++)
                if (((ArtworkChoice)cardBackSelector.Items[i]).Name == name)
                    cardBackSelector.SelectedIndex = i;
        }

        private void ApplySettings_Click(object sender, EventArgs e)
        {
            ArtworkChoice pending = (ArtworkChoice)artworkSelector.SelectedItem;
            if (!HasCompleteArtwork(pending.Path))
            {
                MessageBox.Show(this, "This planned deck is coming soon. Its complete 78-card artwork must be added before it can be applied.",
                    "Artwork not available yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Complete closing the panel before changing images or rebuilding controls.
            // Hiding it halfway through Apply leaves native child-window paints mixed
            // with the image updates on the same UI message.
            HideOverlay(settingsPanel);
            settingsButton.Enabled = false;
            BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    if (!IsDisposed) ApplySelectedSettings();
                }
                finally
                {
                    if (!settingsButton.IsDisposed) settingsButton.Enabled = true;
                }
            }));
        }

        private void ApplySelectedSettings()
        {
            StageReadingSettings();
            bool deckChanged = (int)selectedDeck != deckSelector.SelectedIndex ||
                TarotDeck.Properties.Settings.Default.ArtworkName != ((ArtworkChoice)artworkSelector.SelectedItem).Name;
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            TarotDeck.Properties.Settings.Default.AutoSpread = autoSpreadCheckBox.Checked;
            string backName = ((ArtworkChoice)cardBackSelector.SelectedItem).Name;
            bool backChanged = TarotDeck.Properties.Settings.Default.CardBackName != backName;
            TarotDeck.Properties.Settings.Default.CardBackName = backName;
            SaveSelection();
            ActivateReadingSettings();
            if (deckChanged)
            {
                RefreshArtworkFileIndex();
                StartNewDeck();
                Shuffle();
                SpreadDeck();
            }
            else if (backChanged)
            {
                // Change only the back; preserve the fan slots and already drawn cards.
                Bitmap replacement = LoadCardBack();
                Bitmap previous = deckBackImage;
                deckBackImage = replacement;
                foreach (PictureBox card in pcbDeck)
                    if (card != null && !card.IsDisposed) card.Image = replacement;
                foreach (Bitmap packet in shufflePackets.Values) packet.Dispose();
                shufflePackets.Clear();
                if (shufflePileTimer != null) PrepareShufflePackets();
                if (fanSurface != null) fanSurface.Invalidate();
                if (previous != null) previous.Dispose();
            }
        }

        private Bitmap LoadCardBack()
        {
            string selectedName = TarotDeck.Properties.Settings.Default.CardBackName;
            ArtworkChoice choice = cardBackSelector.Items.Cast<ArtworkChoice>()
                .FirstOrDefault(item => item.Name == selectedName) ?? cardBackSelector.Items.Cast<ArtworkChoice>().First(item => item.Name == "Emerald Labyrinth");
            return CardArtwork.Load(choice.Path);
        }

        private void HideOverlay(Panel panel)
        {
            Form overlay;
            if (overlayWindows.TryGetValue(panel, out overlay)) overlay.Hide();
            else panel.Hide();
            if (!IsDisposed && settingsButton != null) settingsButton.Focus();
        }

        private void ShowOverlay(Panel panel)
        {
            Form overlay;
            if (!overlayWindows.TryGetValue(panel, out overlay))
            {
                Rectangle bounds = RectangleToScreen(panel.Bounds);
                overlay = new Form { FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
                    AutoScaleMode = AutoScaleMode.None, Font = Font,
                    BackColor = panel.BackColor, ClientSize = panel.Size,
                    Location = bounds.Location, Text = panel == settingsPanel ? "Settings" : "Artwork credits" };
                Controls.Remove(panel);
                overlay.Controls.Add(panel);
                panel.Dock = DockStyle.Fill;
                overlayWindows.Add(panel, overlay);
                overlay.FormClosing += (s, args) =>
                {
                    if (args.CloseReason != CloseReason.UserClosing) return;
                    args.Cancel = true;
                    if (panel == settingsPanel) ResetPendingSettings();
                    HideOverlay(panel);
                };
            }
            panel.Show();
            overlay.Show(this);
            overlay.Activate();
        }

        private void PositionOverlay(Panel panel, Rectangle bounds)
        {
            Form overlay;
            if (overlayWindows.TryGetValue(panel, out overlay))
            {
                overlay.Location = PointToScreen(bounds.Location);
                overlay.ClientSize = bounds.Size;
            }
            else panel.Bounds = bounds;
        }

        private void PopulateArtworkChoices(string preferredName)
        {
            string appImages = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            string userDecks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TarotDeck", "Decks");
            if (preferredName == "Classic Tarot" || preferredName == "Classic RWS — Approved Violet Drapery" ||
                preferredName == "Classic Rider–Waite–Smith Inspired" || preferredName == "Classic RWS")
            {
                preferredName = "The Illustrated Arcana";
                TarotDeck.Properties.Settings.Default.ArtworkName = preferredName;
                TarotDeck.Properties.Settings.Default.Save();
            }
            List<ArtworkChoice> choices = new List<ArtworkChoice>();
            string bundledSoimoi = Path.Combine(appImages, "soimoi");
            if (AssetStore.DirectoryExists(bundledSoimoi)) choices.Add(new ArtworkChoice("Soimoi Tarot", bundledSoimoi));
            foreach (string directory in AssetStore.GetDirectories(appImages).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(directory, bundledSoimoi, StringComparison.OrdinalIgnoreCase) || (!HasCompleteArtwork(directory) && !AssetStore.Exists(Path.Combine(directory, "PLANNED.txt")))) continue;
                string titleFile = Path.Combine(directory, "DECKNAME.txt");
                string title = AssetStore.Exists(titleFile) ? AssetStore.ReadAllText(titleFile).Trim() : Path.GetFileName(directory);
                choices.Add(new ArtworkChoice(string.IsNullOrWhiteSpace(title) ? Path.GetFileName(directory) : title, directory, !HasCompleteArtwork(directory)));
            }
            if (AssetStore.DirectoryExists(userDecks))
            {
                foreach (string directory in AssetStore.GetDirectories(userDecks))
                    if (HasCompleteArtwork(directory)) choices.Add(new ArtworkChoice(Path.GetFileName(directory), directory));
            }

            choices = choices.OrderBy(choice => choice.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            artworkSelector.Items.Clear();
            foreach (ArtworkChoice choice in choices) artworkSelector.Items.Add(choice);
            int selected = choices.FindIndex(x => string.Equals(x.Name, preferredName, StringComparison.OrdinalIgnoreCase));
            artworkSelector.SelectedIndex = selected < 0 || choices[selected].IsPlanned ? choices.FindIndex(choice => !choice.IsPlanned) : selected;
        }

        private void CreditsButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(artworkCredits)) return;
            if (creditsPanel == null)
            {
                creditsPanel = new Panel { BackColor = Color.FromArgb(35, 35, 35),
                    BorderStyle = BorderStyle.FixedSingle, Visible = false };
                Label heading = new Label { Text = "Artwork credits", ForeColor = Color.White,
                    AutoSize = true, Location = new Point(16, 12) };
                creditsText = new TextBox { Multiline = true, ReadOnly = true,
                    ScrollBars = ScrollBars.None, BorderStyle = BorderStyle.None,
                    BackColor = creditsPanel.BackColor, ForeColor = Color.White,
                    Location = new Point(16, 40), Size = new Size(486, 164),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
                creditsPanel.Size = new Size(520, 260);
                Button close = new Button { Text = "Close", Size = new Size(80, 27),
                    Location = new Point(422, 217), Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    UseVisualStyleBackColor = true };
                close.Click += (s, args) => HideOverlay(creditsPanel);
                creditsPanel.Controls.Add(heading);
                creditsPanel.Controls.Add(creditsText);
                creditsText.TextChanged += (s, args) => UpdateCreditsScrollbar();
                creditsText.SizeChanged += (s, args) => UpdateCreditsScrollbar();
                creditsPanel.Controls.Add(close);
                Controls.Add(creditsPanel);
            }
            // Discard pending Settings edits when switching panels.
            if (settingsPanel.Visible) { ResetPendingSettings(); HideOverlay(settingsPanel); }
            creditsText.Text = artworkCredits;
            PositionSettings();
            ShowOverlay(creditsPanel);
            UpdateCreditsScrollbar();
        }

        private void UpdateCreditsScrollbar()
        {
            if (creditsText == null || creditsText.ClientSize.Width <= 0) return;
            // Measure against the width available without a scrollbar so an existing
            // scrollbar cannot cause itself to remain visible through extra wrapping.
            int width = creditsText.ClientSize.Width +
                (creditsText.ScrollBars == ScrollBars.Vertical ? SystemInformation.VerticalScrollBarWidth : 0);
            Size required = TextRenderer.MeasureText(creditsText.Text, creditsText.Font,
                new Size(width, int.MaxValue), TextFormatFlags.WordBreak |
                TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix);
            ScrollBars desired = required.Height > creditsText.ClientSize.Height
                ? ScrollBars.Vertical : ScrollBars.None;
            if (creditsText.ScrollBars != desired) creditsText.ScrollBars = desired;
        }

        private void UpdateArtworkCredits()
        {
            ArtworkChoice choice = artworkSelector.SelectedItem as ArtworkChoice;
            artworkCredits = null;
            if (choice != null && choice.Name == "Soimoi Tarot")
                artworkCredits = "Soimoi Tarot artwork by Mike Koz (https://koz.tv/), licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). The artwork was adapted to this app's card filename format. The custom card back was created for TarotDeck.";
            else if (choice != null)
            {
                List<string> credits = new List<string>();
                foreach (string file in new[] { "CREDITS.txt", "ATTRIBUTION.txt", "LICENSE", "LICENSE.txt", "LICENSE.md" })
                {
                    string path = Path.Combine(choice.Path, file);
                    if (AssetStore.Exists(path))
                    {
                        string text = AssetStore.ReadAllText(path);
                        if (!string.IsNullOrWhiteSpace(text)) credits.Add(text.Trim());
                    }
                }
                artworkCredits = string.Join(Environment.NewLine + Environment.NewLine, credits);
            }
            creditsButton.Visible = !string.IsNullOrWhiteSpace(artworkCredits);
            string attributionRequirement = choice == null ? null : Path.Combine(choice.Path, "ATTRIBUTION_REQUIRED.txt");
            if (attributionRequirement != null && AssetStore.Exists(attributionRequirement) &&
                string.Equals(AssetStore.ReadAllText(attributionRequirement).Trim(), "false", StringComparison.OrdinalIgnoreCase))
                creditsButton.Visible = false;
            if (creditsPanel != null)
            {
                creditsText.Text = artworkCredits ?? "";
                if (!creditsButton.Visible) HideOverlay(creditsPanel);
            }
        }

        private void RaiseMainControls()
        {
            PositionSettings();
            RaiseReadingControls();
            settingsButton.BringToFront();
            creditsButton.BringToFront();
            if (settingsPanel.Visible) settingsPanel.BringToFront();
            if (creditsPanel != null && creditsPanel.Visible) creditsPanel.BringToFront();
        }

        private static bool HasCompleteArtwork(string directory)
        {
            if (!AssetStore.DirectoryExists(directory)) return false;
            return HasCompleteArtwork(BuildArtworkFileIndex(directory));
        }

        private static bool HasCompleteArtwork(Dictionary<string, string> files)
        {
            foreach (string file in GetRequiredImageNames())
                if (!files.ContainsKey(Path.GetFileNameWithoutExtension(file))) return false;
            return true;
        }

        private static IEnumerable<string> GetRequiredImageNames()
        {
            Deck completeDeck = new Deck(false);
            return completeDeck.cards.Select(card => card.CardImg);
        }

        private static Dictionary<string, string> BuildArtworkFileIndex(string directory)
        {
            Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] preferredExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            foreach (string path in AssetStore.GetFiles(directory))
            {
                string extension = Path.GetExtension(path);
                if (!preferredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                if (!files.ContainsKey(name)) files.Add(name, path);
            }
            return files;
        }

        private void RefreshArtworkFileIndex()
        {
            ArtworkChoice choice = artworkSelector.SelectedItem as ArtworkChoice;
            string directory = choice == null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "the-illustrated-arcana") : choice.Path;
            activeArtworkFiles = BuildArtworkFileIndex(directory);
            UpdateArtworkCredits();
        }

        private string FindArtworkFile(string expectedName)
        {
            string path;
            activeArtworkFiles.TryGetValue(Path.GetFileNameWithoutExtension(expectedName), out path);
            return path;
        }

        private sealed class ArtworkChoice
        {
            public string Name { get; private set; }
            public string Path { get; private set; }
            public bool IsPlanned { get; private set; }
            public ArtworkChoice(string name, string path, bool isPlanned = false) { Name = name; Path = path; IsPlanned = isPlanned; }
            public override string ToString() { return Name + (IsPlanned ? " (Coming soon)" : ""); }
        }

        private void StartNewDeck()
        {
            deck = new Deck(false, selectedDeck);
            drawn = new Deck(true);
        }

        private void btnDrawCards_Click(object sender, EventArgs e)
        {
            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);
            //string strImg = null;
            Card curCard = null;

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard01, LoadCardImage(curCard.CardImg), curCard);
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard02, LoadCardImage(curCard.CardImg), curCard);
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard03, LoadCardImage(curCard.CardImg), curCard);
                RemoveCardFromDeck(curCard);
            }
            //MessageBox.Show(drawn.cards.Count.ToString());
        }

        private void btnShufCards_Click(object sender, EventArgs e)
        {
            // Match the original app: reshuffle the current deck unless cards were already drawn.
            if (deck.cards.Count < deck.Size)
                StartNewDeck();

            this.Shuffle();
            this.SpreadDeck();
        }

        /// <summary>
        /// Shuffles the cards in the deck
        /// </summary>
        public void Shuffle()
        {
            SetCurrentPage(0);
            SetPictureImage(pcbCardFirst, null);
            SetPictureImage(pcbCardLast, null);

            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);

            btnDrawCards.Enabled = true;

            deck.Shuffle();
        }

        private void pcbDeck_Click(object sender, EventArgs e)
        {
            if (spreadRevealTimer != null || shufflePileTimer != null) return;

            PictureBox pcbClick = (PictureBox)sender;
            Card curCard = (Card)pcbClick.Tag;
            int cardPosition = deck.cards.IndexOf(curCard);
            //MessageBox.Show(curCard.cardIndex.ToString());

            if (intCurPage != intMaxPage)
                ShowPage(0);

            if (cardPosition == 0)
            {
                SetPictureImage(pcbCardFirst, LoadCardImage(curCard.CardImg), curCard);
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
            }
            else if (cardPosition == deck.cards.Count - 1)
            {
                SetPictureImage(pcbCardLast, LoadCardImage(curCard.CardImg), curCard);
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
            }
            else
            {
                if (pcbCard01.Image != null && pcbCard02.Image != null && pcbCard03.Image != null)
                {
                    SetPictureImage(pcbCard01, null);
                    SetPictureImage(pcbCard02, null);
                    SetPictureImage(pcbCard03, null);
                }

                if (pcbCard01.Image == null)
                {
                    SetPictureImage(pcbCard01, LoadCardImage(curCard.CardImg), curCard);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard02.Image == null)
                {
                    SetPictureImage(pcbCard02, LoadCardImage(curCard.CardImg), curCard);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard03.Image == null)
                {
                    SetPictureImage(pcbCard03, LoadCardImage(curCard.CardImg), curCard);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
                drawn.cards.Add(curCard);
                SetCurrentPage(0);

                if (pcbCard01.Image != null && pcbCard02.Image != null && pcbCard03.Image != null)
                {
                    btnDrawCards.Enabled = true;
                }
                else
                {
                    btnDrawCards.Enabled = false;
                }
                //MessageBox.Show(drawn.cards.Count.ToString());
            }

            //MessageBox.Show(pcbClick.Name, "Deck");
        }

        private void btnGoBack_Click(object sender, EventArgs e)
        {
            ShowPage(-1);
        }

        private void btnGoFowd_Click(object sender, EventArgs e)
        {
            ShowPage(+1);
        }

        //public class DeckPictureBox : PictureBox
        //{
        //    public Card myCard;

        //    public Card MyCard
        //    {
        //        get { return myCard; }

        //        set { myCard = value; }
        //    }
        //}

        private void RemoveCardFromDeck(Card pCard)
        {
            drawn.cards.Add(pCard);
            SetCurrentPage(0);
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                if (pcbDeck[i] == null) continue;
                Card dCard = pcbDeck[i].Tag as Card;
                if (dCard == null) continue;
                if (pCard.Suit == dCard.Suit && pCard.CardVal == dCard.CardVal)
                {
                    RemoveDeckPictureBox(pcbDeck[i]);
                    break;
                }
            }
        }

        private void RemoveDeckPictureBox(PictureBox pictureBox)
        {
            if (pictureBox == null) return;
            pictureBox.Visible = false;
            pictureBox.Image = null;
            pictureBox.Click -= pcbDeck_Click;
            Controls.Remove(pictureBox);
            pictureBox.Dispose();
            for (int i = 0; i < pcbDeck.Length; i++)
                if (ReferenceEquals(pcbDeck[i], pictureBox)) pcbDeck[i] = null;

            // Paint the underlying cards at their existing locations.
            if (fanSurface != null) fanSurface.Invalidate();
            Refresh();
        }

        private void RemoveDeck()
        {
            foreach (Bitmap packet in shufflePackets.Values) packet.Dispose();
            shufflePackets.Clear();
            if (fanSurface != null)
            {
                Controls.Remove(fanSurface);
                fanSurface.Dispose();
                fanSurface = null;
            }
            SuspendLayout();
            try
            {
                for (int i = 0; i < pcbDeck.Length; i++)
                {
                    PictureBox pictureBox = pcbDeck[i];
                    if (pictureBox == null) continue;
                    pictureBox.Image = null;
                    pictureBox.Click -= pcbDeck_Click;
                    Controls.Remove(pictureBox);
                    pictureBox.Dispose();
                    pcbDeck[i] = null;
                }
            }
            finally
            {
                ResumeLayout(false);
            }
            if (deckBackImage != null)
            {
                deckBackImage.Dispose();
                deckBackImage = null;
            }
        }

        private void ShuffleDeck()
        {
            RemoveDeck();
            deckBackImage = LoadCardBack();
            pcbDeck = new PictureBox[deck.cards.Count];
            SuspendLayout();
            try
            {
                for (int i = 0; i < pcbDeck.Length; i++)
                {
                    PictureBox pictureBox = new PictureBox();
                    pcbDeck[i] = pictureBox;
                    pictureBox.Size = new Size(Math.Max(1, (int)(175 * dblScrWidRat)),
                        Math.Max(1, (int)(300 * dblScrHgtRat)));
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox.Image = deckBackImage;
                    pictureBox.Tag = deck.cards[i];
                    pictureBox.Click += pcbDeck_Click;
                    pictureBox.Visible = false;
                    pictureBox.Location = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
                    Controls.Add(pictureBox);
                    pictureBox.BringToFront();
                }
            }
            finally
            {
                ResumeLayout(false);
            }
        }

        private void SpreadDeck()
        {
            if (spreadRevealTimer != null)
            {
                spreadRevealTimer.Stop();
                spreadRevealTimer.Dispose();
                spreadRevealTimer = null;
            }
            if (shufflePileTimer != null)
            {
                shufflePileTimer.Stop();
                shufflePileTimer.Dispose();
                shufflePileTimer = null;
            }

            ShuffleDeck();
            btnShufCards.Enabled = false;
            btnDrawCards.Enabled = false;
            PrepareShufflePackets();
            shuffleStartedAtUtc = DateTime.UtcNow;
            fanSurface = new FanSurface();
            fanSurface.Bounds = ClientRectangle;
            fanSurface.PaintCards = PaintShuffle;
            Controls.Add(fanSurface);
            fanSurface.BringToFront();
            RaiseMainControls();
            shufflePileTimer = new Timer();
            shufflePileTimer.Interval = 16;
            shufflePileTimer.Tick += ShufflePileTimer_Tick;
            shufflePileTimer.Start();
        }

        private void ShufflePileTimer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.UtcNow - shuffleStartedAtUtc).TotalSeconds >= 2)
            {
                StopShufflePileTimer();
                Controls.Remove(fanSurface);
                fanSurface.Dispose();
                fanSurface = null;
                BeginFanSpread();
                return;
            }
            fanSurface.Invalidate();
        }

        private static double SmoothStep(double value)
        {
            value = Math.Max(0, Math.Min(1, value));
            return value * value * (3 - 2 * value);
        }

        private void PaintShuffle(Graphics graphics)
        {
            double elapsed = Math.Max(0, (DateTime.UtcNow - shuffleStartedAtUtc).TotalSeconds);
            float width = Math.Min((float)(175 * dblScrWidRat), ClientSize.Width * 0.18f);
            float height = width * 300f / 175f;
            double gather = SmoothStep((elapsed - 1.6) / 0.4);
            float remaining = (float)(1 - gather);
            float destinationX = ClientSize.Width * 0.05f + height * 0.08f + width / 2;
            float peak = Math.Max(height * 0.10f + 8, (float)(42 * dblScrHgtRat));
            float destinationY = peak + Math.Min(76f, flpMain.Top * 0.12f) + height / 2;
            float centerX = (float)(ClientSize.Width / 2f * (1 - gather) + destinationX * gather);
            float centerY = (float)(ClientSize.Height / 2f * (1 - gather) + destinationY * gather);
            const int packetDrops = 6;
            const double dropDuration = 1.6 / packetDrops;
            int completed = Math.Min(packetDrops, (int)(elapsed / dropDuration));
            double phase = elapsed < 1.6 ? (elapsed % dropDuration) / dropDuration : 1;
            // Lift gently, then let a small packet fall onto the receiving stack.
            double drop = SmoothStep((phase - 0.25) / 0.75);
            float lift = (float)(Math.Sin(Math.PI * Math.Min(1, phase / 0.25)) * height * 0.035);
            float sourceX = centerX + width * 0.13f * remaining;
            float sourceY = centerY - height * 0.10f * remaining;
            float receiverX = centerX - width * 0.10f * remaining;
            float receiverY = centerY + height * 0.08f * remaining;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            if (completed > 0)
                PaintShufflePacket(graphics, receiverX, receiverY, width, height, 0,
                    Math.Min(4, completed), remaining);
            PaintShufflePacket(graphics, sourceX, sourceY, width, height,
                -2 * remaining, Math.Max(1, 4 - completed * 3 / packetDrops), remaining);
            if (elapsed < 1.6)
            {
                float packetX = (float)(sourceX * (1 - drop) + receiverX * drop);
                float packetY = (float)(sourceY * (1 - drop) + receiverY * drop) - lift;
                PaintShufflePacket(graphics, packetX, packetY, width, height,
                    (float)(-2 * (1 - drop)), 2, remaining);
            }
        }

        private void PaintShufflePacket(Graphics graphics, float x, float y,
            float width, float height, float angle, int depth, float depthScale)
        {
            if (depth <= 0) return;
            var saved = graphics.Save();
            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(angle);
            Bitmap packet = shufflePackets[depth];
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            graphics.DrawImage(packet, -width / 2, -height / 2,
                width + (packet.Width - (int)Math.Ceiling(width)) * depthScale,
                height + (packet.Height - (int)Math.Ceiling(height)) * depthScale);
            graphics.Restore(saved);
        }

        private void PrepareShufflePackets()
        {
            int width = (int)Math.Ceiling(Math.Min(175 * dblScrWidRat, ClientSize.Width * 0.18));
            int height = (int)Math.Ceiling(width * 300.0 / 175);
            int edge = Math.Max(1, (int)Math.Round(width * 0.005));
            for (int depth = 1; depth <= 4; depth++)
            {
                Bitmap packet = new Bitmap(width + (depth - 1) * edge, height + (depth - 1) * edge);
                using (Graphics graphics = Graphics.FromImage(packet))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    for (int layer = depth - 1; layer >= 0; layer--)
                        graphics.DrawImage(deckBackImage, layer * edge, layer * edge, width, height);
                }
                shufflePackets.Add(depth, packet);
            }
        }

        private void StopShufflePileTimer()
        {
            if (shufflePileTimer == null) return;
            shufflePileTimer.Stop();
            shufflePileTimer.Dispose();
            shufflePileTimer = null;
        }

        private void BeginFanSpread()
        {
            foreach (PictureBox card in pcbDeck)
            {
                card.Visible = false;
                card.Image = deckBackImage;
            }
            fanProgress = 0;
            fanDragging = false;
            fanSurface = new FanSurface();
            fanSurface.Bounds = new Rectangle(0, 0, ClientSize.Width,
                Math.Max(1, flpMain.Top));
            fanSurface.PaintCards = PaintFan;
            fanSurface.MouseDown += FanMouseDown;
            fanSurface.MouseMove += FanMouseMove;
            fanSurface.MouseUp += FanMouseUp;
            Controls.Add(fanSurface);
            fanSurface.BringToFront();
            RaiseMainControls();
            deckSelector.BringToFront();
            artworkSelector.BringToFront();
            creditsButton.BringToFront();
            foreach (Control control in Controls)
                if (control is Label) control.BringToFront();
            btnShufCards.Enabled = true;
            btnDrawCards.Enabled = false;
            fanSurface.Cursor = Cursors.Hand;
            fanSurface.Invalidate();
            if (TarotDeck.Properties.Settings.Default.AutoSpread)
            {
                DateTime started = DateTime.UtcNow;
                btnShufCards.Enabled = false;
                spreadRevealTimer = new Timer { Interval = 16 };
                spreadRevealTimer.Tick += (s, args) =>
                {
                    double progress = (DateTime.UtcNow - started).TotalSeconds / 0.9;
                    fanProgress = SmoothStep(progress);
                    fanSurface.Invalidate();
                    if (progress >= 1)
                    {
                        spreadRevealTimer.Stop();
                        spreadRevealTimer.Dispose();
                        spreadRevealTimer = null;
                        btnShufCards.Enabled = true;
                        btnDrawCards.Enabled = deck.cards.Count >= 3;
                    }
                };
                spreadRevealTimer.Start();
            }
        }

        // One geometry calculation is shared by painting and hit testing.
        // The end cards have equal heights; the central pair have equal minimum Y.
        private RectangleF FanCardBounds(int index, out float angle)
        {
            int count = pcbDeck.Length;
            float width = Math.Min((float)(175 * dblScrWidRat), ClientSize.Width * 0.18f);
            float height = width * 300f / 175f;
            float rotationPadding = height * 0.08f;
            float left = ClientSize.Width * 0.05f + rotationPadding;
            float span = Math.Max(0, ClientSize.Width - 2 * left - width);
            double t = count > 1 ? index / (double)(count - 1) : 0.5;
            double u = 2 * t - 1;
            double centralU = count % 2 == 0 ? 1.0 / Math.Max(1, count - 1) : 0;
            float rise = Math.Min(76f, fanSurface.Height * 0.12f);
            float peak = Math.Max(height * 0.10f + 8, (float)(42 * dblScrHgtRat));
            float baseY = peak + rise;
            float x = left + (float)(span * t * fanProgress);
            float y = baseY - (float)(rise * (1 - (u * u - centralU * centralU)) * fanProgress);
            angle = (float)(u * 7 * fanProgress);
            return new RectangleF(x, y, width, height);
        }

        private void PaintFan(Graphics graphics)
        {
            bool moving = fanDragging || spreadRevealTimer != null;
            graphics.InterpolationMode = moving
                ? System.Drawing.Drawing2D.InterpolationMode.Bilinear
                : System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = moving
                ? System.Drawing.Drawing2D.PixelOffsetMode.Default
                : System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = moving
                ? System.Drawing.Drawing2D.CompositingQuality.Default
                : System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            for (int i = fanProgress <= 0 ? pcbDeck.Length - 1 : 0; i < pcbDeck.Length; i++)
            {
                if (pcbDeck[i] == null) continue;
                float angle;
                RectangleF bounds = FanCardBounds(i, out angle);
                var saved = graphics.Save();
                graphics.TranslateTransform(bounds.Left + bounds.Width / 2,
                    bounds.Top + bounds.Height / 2);
                graphics.RotateTransform(angle);
                graphics.DrawImage(deckBackImage, -bounds.Width / 2, -bounds.Height / 2,
                    bounds.Width, bounds.Height);
                graphics.Restore(saved);
            }
            if (!TarotDeck.Properties.Settings.Default.AutoSpread && fanProgress == 0 && pcbDeck.Length > 0)
            {
                float angle;
                RectangleF stack = FanCardBounds(pcbDeck.Length - 1, out angle);
                graphics.DrawString("Drag the top card to the right to spread the deck", Font,
                    Brushes.White, stack.Left, stack.Bottom + 12);
            }
        }

        private int HitFan(Point point)
        {
            for (int i = pcbDeck.Length - 1; i >= 0; i--)
            {
                if (pcbDeck[i] == null) continue;
                float angle;
                RectangleF bounds = FanCardBounds(i, out angle);
                double radians = -angle * Math.PI / 180;
                double dx = point.X - (bounds.Left + bounds.Width / 2);
                double dy = point.Y - (bounds.Top + bounds.Height / 2);
                double x = dx * Math.Cos(radians) - dy * Math.Sin(radians);
                double y = dx * Math.Sin(radians) + dy * Math.Cos(radians);
                if (Math.Abs(x) <= bounds.Width / 2 && Math.Abs(y) <= bounds.Height / 2)
                    return i;
            }
            return -1;
        }

        private void FanMouseDown(object sender, MouseEventArgs e)
        {
            if (spreadRevealTimer != null) return;
            if (e.Button != MouseButtons.Left) return;
            pressedFanCard = HitFan(e.Location);
            if (pressedFanCard < 0) return;
            dragStart = e.Location;
            dragStartProgress = fanProgress;
            fanMoved = false;
            fanDragging = true;
            fanSurface.Capture = true;
        }

        private void FanMouseMove(object sender, MouseEventArgs e)
        {
            if (!fanDragging || deck.cards.Count != deck.Size) return;
            int delta = e.X - dragStart.X;
            if (Math.Abs(delta) < SystemInformation.DragSize.Width && !fanMoved) return;
            fanMoved = true;
            float unusedAngle;
            RectangleF first = FanCardBounds(0, out unusedAngle);
            float distance = Math.Max(1, ClientSize.Width * 0.9f - first.Width - first.Height * 0.16f);
            fanProgress = Math.Max(0, Math.Min(1, dragStartProgress + delta / distance));
            fanSurface.Invalidate();
        }

        private void FanMouseUp(object sender, MouseEventArgs e)
        {
            if (!fanDragging) return;
            fanDragging = false;
            fanSurface.Capture = false;
            btnDrawCards.Enabled = fanProgress > 0 && deck.cards.Count >= 3;
            if (!fanMoved && fanProgress > 0 && pressedFanCard >= 0 &&
                pcbDeck[pressedFanCard] != null && HitFan(e.Location) == pressedFanCard)
                pcbDeck_Click(pcbDeck[pressedFanCard], EventArgs.Empty);
            fanSurface.Invalidate();
        }

        private sealed class FanSurface : Control
        {
            public Action<Graphics> PaintCards;
            public FanSurface()
            {
                BackColor = Color.Black;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (PaintCards != null) PaintCards(e.Graphics);
            }
        }

        private void SetScreenRatio()
        {
            Rectangle working = Screen.GetWorkingArea(this);
            dblScrHgtRat = (double)working.Height / 860;
            dblScrWidRat = (double)working.Width / 1600;
            LayoutDrawnCards();
        }

        private void LayoutDrawnCards()
        {
            if (drawnPanel == null)
            {
                drawnPanel = new Panel { BackColor = Color.Black };
                Controls.Add(drawnPanel);
                foreach (Control control in new Control[] { pcbCardFirst, pcbCard01, pcbCard02,
                    pcbCard03, pcbCardLast, btnGoBack, btnShufCards, btnDrawCards, btnGoFowd })
                {
                    control.Margin = Padding.Empty;
                    drawnPanel.Controls.Add(control);
                }
                flpMain.Visible = false;
                flpMain.AutoSize = false;
            }
            Rectangle available = Rectangle.Intersect(ClientRectangle,
                RectangleToClient(Screen.FromControl(this).WorkingArea));
            int cardWidth = Math.Max(1, (int)Math.Round(Math.Min(175 * dblScrWidRat, ClientSize.Width * 0.18)));
            int cardHeight = (int)Math.Round(cardWidth * 300.0 / 175);
            int gap = Math.Max(12, Math.Min((int)Math.Round(cardWidth * 0.40),
                (ClientSize.Width - 5 * cardWidth - 40) / 4));
            int buttonHeight = Math.Max(28, (int)Math.Round(28 * dblScrWidRat));
            int buttonWidth = Math.Max(90, (int)Math.Round(90 * dblScrWidRat));
            int buttonGap = Math.Max(12, (int)Math.Round(12 * dblScrWidRat));
            int cardButtonGap = Math.Max(24, (int)Math.Round(cardHeight * 0.08));
            int rowHeight = cardHeight + buttonHeight + cardButtonGap;
            int footer = settingsButton == null ? 60 : settingsButton.Height + 50;
            int top = Math.Max(0, available.Bottom - footer - rowHeight);
            drawnPanel.Bounds = new Rectangle(0, top, ClientSize.Width, rowHeight);
            // The three reading cards are centered; first/last cards mirror each other outside them.
            PictureBox[] cards = { pcbCardFirst, pcbCard01, pcbCard02, pcbCard03, pcbCardLast };
            for (int i = 0; i < cards.Length; i++)
            {
                int x = (int)Math.Round((ClientSize.Width - cardWidth) / 2.0 + (i - 2) * (cardWidth + gap));
                cards[i].Bounds = new Rectangle(x, 0, cardWidth, cardHeight);
            }
            Button[] buttons = { btnGoBack, btnShufCards, btnDrawCards, btnGoFowd };
            double readingWidth = 3 * cardWidth + 2 * gap;
            double buttonSpacing = Math.Max(buttonWidth + buttonGap, (readingWidth - buttonWidth) / 3.0);
            for (int i = 0; i < buttons.Length; i++)
            {
                int x = (int)Math.Round((ClientSize.Width - buttonWidth) / 2.0 + (i - 1.5) * buttonSpacing);
                buttons[i].Bounds = new Rectangle(x, cardHeight + cardButtonGap, buttonWidth, buttonHeight);
            }
            // The fan uses the remaining upper area; the reading row has its own layout.
            flpMain.Top = top;
            if (fanSurface != null && shufflePileTimer == null)
                fanSurface.Bounds = new Rectangle(0, 0, ClientSize.Width, Math.Max(1, top));
            // Initial scaling and resize can run while the shuffle is active.
            // Keep its full-window surface above the reading row until it finishes.
            if (shufflePileTimer != null && fanSurface != null)
            {
                fanSurface.Bounds = ClientRectangle;
                fanSurface.BringToFront();
                RaiseMainControls();
            }
            else
                drawnPanel.BringToFront();
            LayoutReadingLabels();
        }

        //private void SetCurrentPage()
        //{
        //    intCurPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
        //    intMaxPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
        //    btnGoBack.Enabled = (intCurPage != 1);
        //    btnGoFowd.Enabled = false;
        //}

        private void SetCurrentPage(int pintMovePage)
        {
            if (pintMovePage == 0)
            {
                intCurPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
                intMaxPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
                //btnGoBack.Enabled = (intCurPage != 1);
                //btnGoFowd.Enabled = false;
            }
            else
            {
                intCurPage = intCurPage + pintMovePage;
            }
            btnGoBack.Enabled = (intCurPage == 0) ? false : (intCurPage != 1);
            btnGoFowd.Enabled = (intCurPage != intMaxPage);
            btnDrawCards.Enabled = (deck.cards.Count >= 3);
        }

        private void ShowPage(int pintPage)
        {
            SetCurrentPage(pintPage);
            int intCurCard = intCurPage * 3;

            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);

            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard03, LoadCardImage(drawn.cards[intCurCard - 1].CardImg), drawn.cards[intCurCard - 1]);
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard02, LoadCardImage(drawn.cards[intCurCard - 1].CardImg), drawn.cards[intCurCard - 1]);
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard01, LoadCardImage(drawn.cards[intCurCard - 1].CardImg), drawn.cards[intCurCard - 1]);
            }
        }

        private Bitmap LoadCardImage(string fileName)
        {
            string path = FindArtworkFile(fileName);
            if (path == null) throw new FileNotFoundException("Missing artwork image " + fileName);
            return CardArtwork.Load(path);
        }

        private void SetPictureImage(PictureBox pictureBox, Image image, Card card = null)
        {
            Image previous = pictureBox.Image;
            if (ReferenceEquals(previous, image)) return;
            pictureBox.Image = image;
            pictureBox.Tag = card;
            UpdateReadingAvailability();
            if (previous != null && !ReferenceEquals(previous, deckBackImage)) previous.Dispose();
        }
    }
}
