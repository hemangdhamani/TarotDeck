using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TarotCardsDeck.Readings;

namespace TarotCardsDeck
{
    public partial class frmTarot
    {
        private Button readCardsButton, readingJournalButton;
        private ComboBox readingSpreadSelector, readingTopicSelector, readingFocusSelector;
        private readonly Label[] readingPositionLabels = new Label[5];
        private string readingSpread, readingTopic, readingFocus;
        private readonly ReadingJournal readingJournal = new ReadingJournal();

        private void InitializeReadings()
        {
            var preferences = TarotDeck.Properties.Settings.Default;
            readingSpread = preferences.ReadingSpread;
            readingTopic = preferences.ReadingTopic;
            readingFocus = preferences.ReadingFocus;
            try { ReadingOptions.Validate(readingSpread, readingTopic, readingFocus); }
            catch (ArgumentException) { readingSpread = ReadingOptions.Spreads[0]; readingTopic = "General"; readingFocus = "Overview"; }
            readCardsButton = new Button { Text = "Read Cards", Size = new Size(105, 28), UseVisualStyleBackColor = true, Enabled = false };
            readingJournalButton = new Button { Text = "Journal", Size = new Size(90, 28), UseVisualStyleBackColor = true };
            readCardsButton.Click += (s, e) => ReadDisplayedCards();
            readingJournalButton.Click += (s, e) => new ReadingHistoryWindow(readingJournal).Show(this);
            Controls.AddRange(new Control[] { readCardsButton, readingJournalButton });
            for (int i = 0; i < 5; i++) readingPositionLabels[i] = new Label { TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(224, 213, 190), BackColor = Color.Black, AutoEllipsis = true, Visible = false };
            InitializeReadingSettings();
        }
        private void PositionReadingControls()
        {
            if (readCardsButton == null) return;
            Rectangle available = Rectangle.Intersect(ClientRectangle, RectangleToClient(Screen.FromControl(this).WorkingArea));
            readCardsButton.Location = new Point(Math.Max(available.Left, available.Right - readCardsButton.Width - 20), settingsButton.Top);
            readingJournalButton.Location = new Point(readCardsButton.Left - readingJournalButton.Width - 8, settingsButton.Top);
        }
        private void RaiseReadingControls()
        {
            if (readCardsButton == null) return;
            readCardsButton.BringToFront(); readingJournalButton.BringToFront();
        }
        private void LayoutReadingLabels()
        {
            if (drawnPanel == null || readingPositionLabels[0] == null) return;
            var pictures = new[] { pcbCard01, pcbCard02, pcbCard03, pcbCardFirst, pcbCardLast };
            for (int i = 0; i < 5; i++)
            {
                if (readingPositionLabels[i].Parent != drawnPanel) drawnPanel.Controls.Add(readingPositionLabels[i]);
                readingPositionLabels[i].Bounds = new Rectangle(pictures[i].Left, pictures[i].Bottom + 2, pictures[i].Width,
                    Math.Max(16, Math.Min(22, btnDrawCards.Top - pictures[i].Bottom - 4)));
                readingPositionLabels[i].BringToFront();
            }
            UpdateReadingAvailability();
        }
        private void UpdateReadingAvailability()
        {
            if (readCardsButton == null || IsDisposed) return;
            var pictures = new[] { pcbCard01, pcbCard02, pcbCard03 };
            var positions = ReadingOptions.Positions(readingSpread);
            for (int i = 0; i < 3; i++)
            {
                readingPositionLabels[i].Text = positions[i];
                readingPositionLabels[i].Visible = pictures[i].Image != null && pictures[i].Tag is Card;
            }
            var outer = new[] { pcbCardFirst, pcbCardLast };
            for (int i = 0; i < 2; i++)
            {
                readingPositionLabels[i+3].Text = i == 0 ? "First · Opening theme" : "Last · Underlying theme";
                readingPositionLabels[i+3].Visible = outer[i].Image != null && outer[i].Tag is Card;
            }
            readCardsButton.Enabled = pictures.All(picture => picture.Image != null && picture.Tag is Card);
        }
        private ReadingSnapshot CaptureDisplayedReading()
        {
            var pictures = new[] { pcbCard01, pcbCard02, pcbCard03 };
            if (pictures.Any(picture => picture.Image == null || !(picture.Tag is Card)))
                throw new InvalidOperationException("Draw three reading cards first. The outer first/last cards are not part of the three-card reading.");
            var snapshot = ReadingSnapshot.Create(pictures.Select(picture => {
                var card = (Card)picture.Tag;
                string id = Path.GetFileNameWithoutExtension(card.CardImg).ToLowerInvariant();
                var meaning = CardMeaningLibrary.Find(id);
                return new ReadingCard { Id = id, Name = meaning.Name, ImagePath = FindArtworkFile(card.CardImg) };
            }), readingSpread, readingTopic, readingFocus, ((ArtworkChoice)artworkSelector.SelectedItem).Name);
            snapshot.FirstCard = CaptureContextCard(pcbCardFirst);
            snapshot.LastCard = CaptureContextCard(pcbCardLast);
            snapshot.Validate();
            return snapshot;
        }
        private ReadingCard CaptureContextCard(PictureBox picture)
        {
            var card = picture.Tag as Card;
            if (picture.Image == null || card == null) return null;
            string id = Path.GetFileNameWithoutExtension(card.CardImg).ToLowerInvariant();
            return new ReadingCard { Id = id, Name = CardMeaningLibrary.Find(id).Name, ImagePath = FindArtworkFile(card.CardImg) };
        }
        private void ReadDisplayedCards()
        {
            try
            {
                var snapshot = CaptureDisplayedReading();
                var report = ReadingEngine.Analyze(snapshot);
                snapshot.Interpretation = report.Text; snapshot.Report = report;
                new ReadingWindow(snapshot, report, readingJournal).Show(this);
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            { MessageBox.Show(this, error.Message, "Reading", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        private void InitializeReadingSettings()
        {
            settingsPanel.Size = new Size(520, 360);
            var tabs = new TabControl { Location = new Point(18, 16),
                Size = new Size(settingsPanel.ClientSize.Width - 36, settingsPanel.ClientSize.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AccessibleName = "Settings sections" };
            var artworkPage = new TabPage("Cards and artwork") { BackColor = settingsPanel.BackColor };
            var readingPage = new TabPage("Reading setup") { BackColor = settingsPanel.BackColor };
            // Keep one shared Apply/Cancel pair outside the tabs.
            foreach (Control control in settingsPanel.Controls.Cast<Control>().ToArray())
                if (control != deckSelector && !(control is Button && (control.Text == "Apply" || control.Text == "Cancel")))
                    artworkPage.Controls.Add(control);
            tabs.TabPages.Add(artworkPage); tabs.TabPages.Add(readingPage);
            settingsPanel.Controls.Add(tabs);
            artworkPage.ClientSizeChanged += (s, e) => {
                artworkSelector.Width = Math.Max(100, artworkPage.ClientSize.Width - 40);
                cardBackSelector.Width = artworkSelector.Width;
            };
            InitializeVisualArtworkSettings(artworkPage);
            Func<string, int, ComboBox> choice = (label, top) => {
                readingPage.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Color.White,
                    Location = new Point(20, top) });
                var selector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(20, top + 20), Width = 420,
                    AccessibleName = label };
                readingPage.Controls.Add(selector); return selector;
            };
            readingSpreadSelector = choice("Spread", 16);
            readingSpreadSelector.Items.AddRange(ReadingOptions.Spreads);
            readingTopicSelector = choice("Topic", 76);
            readingTopicSelector.Items.AddRange(ReadingOptions.Topics);
            readingFocusSelector = choice("Focus", 136);
            readingPage.ClientSizeChanged += (s, e) => {
                foreach (var selector in new[] { readingSpreadSelector, readingTopicSelector, readingFocusSelector })
                    selector.Width = Math.Max(100, readingPage.ClientSize.Width - 40);
            };
            readingTopicSelector.SelectedIndexChanged += (s, e) => {
                if (readingTopicSelector.SelectedItem == null) return;
                PopulatePendingReadingFocus("Overview");
            };
            readingPage.Controls.Add(new Label { Text = "Upright cards · Fully offline. Apply updates both tabs.\r\nSaved readings retain their original setup.",
                ForeColor = Color.White, AutoSize = true, Location = new Point(20, 204) });
            ResetPendingReadingSettings();
        }
        private void PopulatePendingReadingFocus(string preferred)
        {
            readingFocusSelector.Items.Clear();
            readingFocusSelector.Items.AddRange(ReadingOptions.Focuses((string)readingTopicSelector.SelectedItem));
            readingFocusSelector.SelectedItem = preferred;
            if (readingFocusSelector.SelectedIndex < 0) readingFocusSelector.SelectedIndex = 0;
        }
        private void ResetPendingReadingSettings()
        {
            if (readingSpreadSelector == null) return;
            readingSpreadSelector.SelectedItem = readingSpread;
            readingTopicSelector.SelectedItem = readingTopic;
            PopulatePendingReadingFocus(readingFocus);
        }
        private void StageReadingSettings()
        {
            string spread = (string)readingSpreadSelector.SelectedItem;
            string topic = (string)readingTopicSelector.SelectedItem;
            string focus = (string)readingFocusSelector.SelectedItem;
            ReadingOptions.Validate(spread, topic, focus);
            var preferences = TarotDeck.Properties.Settings.Default;
            preferences.ReadingSpread = spread; preferences.ReadingTopic = topic; preferences.ReadingFocus = focus;
        }
        private void ActivateReadingSettings()
        {
            var preferences = TarotDeck.Properties.Settings.Default;
            readingSpread = preferences.ReadingSpread; readingTopic = preferences.ReadingTopic; readingFocus = preferences.ReadingFocus;
            UpdateReadingAvailability();
        }
    }
}
