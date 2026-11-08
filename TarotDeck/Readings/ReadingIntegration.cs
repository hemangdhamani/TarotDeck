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
        private Button readingSetupButton, readCardsButton, readingJournalButton;
        private readonly Label[] readingPositionLabels = new Label[3];
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
            readingSetupButton = new Button { Text = "Reading setup", Size = new Size(120, 28), UseVisualStyleBackColor = true };
            readCardsButton = new Button { Text = "Read Cards", Size = new Size(105, 28), UseVisualStyleBackColor = true, Enabled = false };
            readingJournalButton = new Button { Text = "Journal", Size = new Size(90, 28), UseVisualStyleBackColor = true };
            readingSetupButton.Click += (s, e) => ShowReadingSetup();
            readCardsButton.Click += (s, e) => ReadDisplayedCards();
            readingJournalButton.Click += (s, e) => new ReadingHistoryWindow(readingJournal).Show(this);
            Controls.AddRange(new Control[] { readingSetupButton, readCardsButton, readingJournalButton });
            for (int i = 0; i < 3; i++) readingPositionLabels[i] = new Label { TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(224, 213, 190), BackColor = Color.Black, AutoEllipsis = true, Visible = false };
        }
        private void PositionReadingControls()
        {
            if (readingSetupButton == null) return;
            readingSetupButton.Location = new Point(20, settingsButton.Top);
            readCardsButton.Location = new Point(readingSetupButton.Right + 8, settingsButton.Top);
            readingJournalButton.Location = new Point(readCardsButton.Right + 8, settingsButton.Top);
        }
        private void RaiseReadingControls()
        {
            if (readingSetupButton == null) return;
            readingSetupButton.BringToFront(); readCardsButton.BringToFront(); readingJournalButton.BringToFront();
        }
        private void LayoutReadingLabels()
        {
            if (drawnPanel == null || readingPositionLabels[0] == null) return;
            var pictures = new[] { pcbCard01, pcbCard02, pcbCard03 };
            for (int i = 0; i < 3; i++)
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
            readCardsButton.Enabled = pictures.All(picture => picture.Image != null && picture.Tag is Card);
        }
        private ReadingSnapshot CaptureDisplayedReading()
        {
            var pictures = new[] { pcbCard01, pcbCard02, pcbCard03 };
            if (pictures.Any(picture => picture.Image == null || !(picture.Tag is Card)))
                throw new InvalidOperationException("Draw three reading cards first. The outer first/last cards are not part of the three-card reading.");
            return ReadingSnapshot.Create(pictures.Select(picture => {
                var card = (Card)picture.Tag;
                string id = Path.GetFileNameWithoutExtension(card.CardImg).ToLowerInvariant();
                var meaning = CardMeaningLibrary.Find(id);
                return new ReadingCard { Id = id, Name = meaning.Name, ImagePath = FindArtworkFile(card.CardImg) };
            }), readingSpread, readingTopic, readingFocus, ((ArtworkChoice)artworkSelector.SelectedItem).Name);
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
        private void ShowReadingSetup()
        {
            using (var setup = new Form { Text = "Reading setup", StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                ShowInTaskbar = false, ClientSize = new Size(480, 340), Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.White })
            {
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 8 };
                setup.Controls.Add(layout);
                Func<string, ComboBox> makeChoice = label => {
                    layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 5) };
                    layout.Controls.Add(combo); return combo;
                };
                var spread = makeChoice("Spread"); spread.Items.AddRange(ReadingOptions.Spreads); spread.SelectedItem = readingSpread;
                var topic = makeChoice("Topic"); topic.Items.AddRange(ReadingOptions.Topics); topic.SelectedItem = readingTopic;
                var focus = makeChoice("Focus"); focus.Items.AddRange(ReadingOptions.Focuses(readingTopic)); focus.SelectedItem = readingFocus;
                topic.SelectedIndexChanged += (s, e) => { focus.Items.Clear(); focus.Items.AddRange(ReadingOptions.Focuses((string)topic.SelectedItem)); focus.SelectedIndex = 0; };
                layout.Controls.Add(new Label { Text = "Upright cards · Fully offline\r\nChanges interpret the displayed three cards; saved readings keep their original setup.", AutoSize = true,
                    MaximumSize = new Size(430, 0), Margin = new Padding(0, 10, 0, 10) });
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Height = 38 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 30, ForeColor = Color.Black,
                    BackColor = SystemColors.Control, UseVisualStyleBackColor = true };
                var apply = new Button { Text = "Apply", Width = 90, Height = 30, ForeColor = Color.Black,
                    BackColor = SystemColors.Control, UseVisualStyleBackColor = true };
                actions.Controls.Add(cancel); actions.Controls.Add(apply); layout.Controls.Add(actions);
                setup.CancelButton = cancel; setup.AcceptButton = apply;
                apply.Click += (s, e) => {
                    string newSpread = (string)spread.SelectedItem, newTopic = (string)topic.SelectedItem, newFocus = (string)focus.SelectedItem;
                    ReadingOptions.Validate(newSpread, newTopic, newFocus);
                    var preferences = TarotDeck.Properties.Settings.Default;
                    preferences.ReadingSpread = newSpread; preferences.ReadingTopic = newTopic; preferences.ReadingFocus = newFocus;
                    try { preferences.Save(); }
                    catch (System.Configuration.ConfigurationErrorsException error) { MessageBox.Show(setup, "Could not save reading preferences: " + error.Message); return; }
                    readingSpread = newSpread; readingTopic = newTopic; readingFocus = newFocus;
                    UpdateReadingAvailability(); setup.DialogResult = DialogResult.OK; setup.Close();
                };
                setup.ShowDialog(this);
            }
        }
    }
}
