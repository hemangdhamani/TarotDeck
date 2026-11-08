using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TarotCardsDeck.Readings
{
    /// <summary>Displays a reading snapshot without holding references to the live deck.</summary>
    public sealed class ReadingWindow : Form
    {
        private readonly ReadingSnapshot snapshot;
        private readonly ReadingReport report;
        private readonly ReadingJournal journal;
        private readonly TextBox notes;
        private readonly Label status;
        private readonly List<Image> thumbnails = new List<Image>();

        public ReadingWindow(ReadingSnapshot snapshot, ReadingReport report, ReadingJournal journal)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (report == null) throw new ArgumentNullException("report");
            if (journal == null) throw new ArgumentNullException("journal");
            this.snapshot = snapshot;
            this.report = report;
            this.journal = journal;
            ReadingUi.Configure(this, "Your reading", new Size(1100, 840));

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18),
                ColumnCount = 1, RowCount = 4, BackColor = BackColor };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);
            root.Controls.Add(new Label { Text = snapshot.Spread + "  |  " + snapshot.Topic + "  |  " + snapshot.Focus,
                AutoSize = true, MaximumSize = new Size(820, 0), ForeColor = ForeColor,
                Margin = new Padding(0, 0, 0, 14) }, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill, Margin = Padding.Empty };
            root.Controls.Add(tabs, 0, 1);
            var overview = ReadingUi.Page("Overview");
            var overviewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10) };
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
            overviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var cardStrip = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, AccessibleName = "Your three reading cards" };
            for (int i = 0; i < 3; i++) cardStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            overviewLayout.Controls.Add(cardStrip, 0, 0);
            overviewLayout.Controls.Add(ReadingUi.ReadOnlyText(BuildOverview(report)), 0, 1);
            overview.Controls.Add(overviewLayout);
            tabs.TabPages.Add(overview);

            var cards = snapshot.Cards ?? new ReadingCard[0];
            var positions = ReadingOptions.Positions(snapshot.Spread);
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                int tabIndex = i + 1;
                var tile = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(6) };
                tile.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                tile.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
                var preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
                    Image = LoadThumbnail(card.ImagePath), Cursor = Cursors.Hand, AccessibleName = positions[i] + ": " + card.Name };
                preview.Click += (s, e) => tabs.SelectedIndex = tabIndex;
                tile.Controls.Add(preview, 0, 0);
                var openCard = ReadingUi.Button(positions[i] + " · " + card.Name);
                openCard.Dock = DockStyle.Fill; openCard.AutoSize = false; openCard.AutoEllipsis = true;
                openCard.Margin = new Padding(0, 3, 0, 0); openCard.Padding = Padding.Empty;
                openCard.Click += (s, e) => tabs.SelectedIndex = tabIndex;
                tile.Controls.Add(openCard, 0, 1); cardStrip.Controls.Add(tile, i, 0);
                var page = ReadingUi.Page((i + 1) + ". " + positions[i]);
                var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    Padding = new Padding(12), BackColor = BackColor };
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
                var imageHost = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                imageHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                imageHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var picture = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = BackColor, Margin = new Padding(0, 0, 12, 8), AccessibleName = card.Name };
                picture.Image = LoadThumbnail(card.ImagePath);
                imageHost.Controls.Add(picture, 0, 0);
                imageHost.Controls.Add(new Label { AutoSize = true, ForeColor = ForeColor,
                    Text = positions[i] + "\r\n" + card.Name + "\r\nUpright", Margin = new Padding(0, 0, 12, 0) }, 0, 1);
                split.Controls.Add(imageHost, 0, 0);
                string detail = report.CardDetails != null && i < report.CardDetails.Length
                    ? report.CardDetails[i] : "See the saved interpretation in the Overview tab.";
                split.Controls.Add(ReadingUi.ReadOnlyText(detail), 1, 0);
                page.Controls.Add(split);
                tabs.TabPages.Add(page);
            }

            var notesPage = ReadingUi.Page("Your notes");
            var notesLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12),
                ColumnCount = 1, RowCount = 2 };
            notesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            notesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            notesLayout.Controls.Add(new Label { Text = "Record what resonates and the next step you choose. Notes stay on this desktop and are saved only with Save reading.",
                AutoSize = true, ForeColor = ForeColor, MaximumSize = new Size(760, 0),
                Margin = new Padding(0, 0, 0, 12) }, 0, 0);
            notes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
                MaxLength = 16000, Text = snapshot.Notes ?? "", BackColor = Color.FromArgb(42, 42, 42),
                ForeColor = ForeColor, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Reading journal notes" };
            notesLayout.Controls.Add(notes, 0, 1);
            notesPage.Controls.Add(notesLayout);
            tabs.TabPages.Add(notesPage);

            status = new Label { Dock = DockStyle.Fill, Text = "Offline reading. Symbolic guidance for reflection, not a guaranteed prediction.",
                ForeColor = ForeColor, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(status, 0, 2);
            var actions = ReadingUi.Actions();
            var close = ReadingUi.Button("Close");
            close.Click += (s, e) => Close();
            var export = ReadingUi.Button("Export text...");
            export.Click += ExportClick;
            var save = ReadingUi.Button("Save reading");
            save.Click += SaveClick;
            actions.Controls.Add(close);
            actions.Controls.Add(export);
            actions.Controls.Add(save);
            root.Controls.Add(actions, 0, 3);
            CancelButton = close;
            FormClosed += (s, e) => { foreach (Image image in thumbnails) image.Dispose(); thumbnails.Clear(); };
        }

        private Image LoadThumbnail(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            try
            {
                using (Image source = Image.FromFile(path))
                {
                    var image = new Bitmap(source);
                    thumbnails.Add(image);
                    return image;
                }
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (ArgumentException) { return null; }
        }

        private static string BuildOverview(ReadingReport reading)
        {
            // History intentionally displays its original saved text, not regenerated meanings.
            if (string.IsNullOrWhiteSpace(reading.Summary)) return reading.Text ?? "";
            return (reading.Summary ?? "") + "\r\n\r\nCONNECTIONS\r\n" + Join(reading.Connections)
                + "\r\n\r\nNEXT STEPS\r\n" + Join(reading.Actions)
                + "\r\n\r\nREFLECTION\r\n" + Join(reading.Reflections);
        }

        private static string Join(string[] entries)
        {
            return string.Join("\r\n\r\n", (entries ?? new string[0]).Select(entry => "• " + entry));
        }

        private void SaveClick(object sender, EventArgs e)
        {
            try
            {
                snapshot.Notes = notes.Text;
                snapshot.Interpretation = report.Text;
                snapshot.Report = report;
                journal.Save(snapshot);
                status.Text = "Saved locally. Reopen this reading from Reading history.";
            }
            catch (IOException ex) { status.Text = "Could not save: " + ex.Message; }
            catch (UnauthorizedAccessException ex) { status.Text = "Could not save: " + ex.Message; }
            catch (ArgumentException ex) { status.Text = "Could not save: " + ex.Message; }
        }

        private void ExportClick(object sender, EventArgs e)
        {
            using (var picker = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", DefaultExt = "txt",
                AddExtension = true, FileName = "Tarot reading.txt", OverwritePrompt = true })
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    snapshot.Notes = notes.Text;
                    snapshot.Interpretation = report.Text;
                    snapshot.Report = report;
                    journal.Export(snapshot, picker.FileName);
                    status.Text = "Text exported. Your local journal is updated only when you choose Save reading.";
                }
                catch (IOException ex) { status.Text = "Could not export: " + ex.Message; }
                catch (UnauthorizedAccessException ex) { status.Text = "Could not export: " + ex.Message; }
                catch (ArgumentException ex) { status.Text = "Could not export: " + ex.Message; }
            }
        }
    }

    public sealed class ReadingHistoryWindow : Form
    {
        private readonly ReadingJournal journal;
        private readonly ListView readings;
        private readonly Label status;

        public ReadingHistoryWindow(ReadingJournal journal)
        {
            if (journal == null) throw new ArgumentNullException("journal");
            this.journal = journal;
            ReadingUi.Configure(this, "Reading history — stored on this desktop", new Size(900, 640));
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18),
                ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);
            readings = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
                MultiSelect = false, HideSelection = false, BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = ForeColor, AccessibleName = "Saved readings" };
            readings.Columns.Add("Saved reading", 195);
            readings.Columns.Add("Spread", 260);
            readings.Columns.Add("Topic", 135);
            readings.Columns.Add("Focus", 190);
            readings.DoubleClick += (s, e) => OpenSelected();
            root.Controls.Add(readings, 0, 0);
            status = new Label { Dock = DockStyle.Fill, ForeColor = ForeColor, AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(status, 0, 1);
            var actions = ReadingUi.Actions();
            var close = ReadingUi.Button("Close");
            close.Click += (s, e) => Close();
            var open = ReadingUi.Button("Open reading");
            open.Click += (s, e) => OpenSelected();
            var delete = ReadingUi.Button("Delete...");
            delete.Click += (s, e) => DeleteSelected();
            var refresh = ReadingUi.Button("Refresh");
            refresh.Click += (s, e) => Reload();
            actions.Controls.Add(close);
            actions.Controls.Add(open);
            actions.Controls.Add(delete);
            actions.Controls.Add(refresh);
            root.Controls.Add(actions, 0, 2);
            CancelButton = close;
            Shown += (s, e) => Reload();
        }

        private ReadingSnapshot Selected
        {
            get { return readings.SelectedItems.Count == 0 ? null : readings.SelectedItems[0].Tag as ReadingSnapshot; }
        }

        private void Reload()
        {
            try
            {
                var saved = journal.List();
                readings.Items.Clear();
                foreach (var entry in saved)
                {
                    var item = new ListViewItem(DateTimeOffset.Parse(entry.CreatedAt, System.Globalization.CultureInfo.InvariantCulture).ToLocalTime().ToString("g")) { Tag = entry };
                    item.SubItems.Add(entry.Spread);
                    item.SubItems.Add(entry.Topic);
                    item.SubItems.Add(entry.Focus);
                    readings.Items.Add(item);
                }
                status.Text = saved.Count == 0 ? "No saved readings yet. Use Save reading in a reading window." : saved.Count + " reading(s) saved locally.";
                if (!string.IsNullOrWhiteSpace(journal.LastLoadWarning)) status.Text += " " + journal.LastLoadWarning;
            }
            catch (IOException ex) { status.Text = "Could not load history: " + ex.Message; }
            catch (UnauthorizedAccessException ex) { status.Text = "Could not load history: " + ex.Message; }
            catch (ArgumentException ex) { status.Text = "Could not load history: " + ex.Message; }
        }

        private void OpenSelected()
        {
            var entry = Selected;
            if (entry == null) { status.Text = "Select a saved reading first."; return; }
            try { new ReadingWindow(entry, ReadingReport.FromSnapshot(entry), journal).Show(this); }
            catch (ArgumentException ex) { status.Text = "Could not open this reading: " + ex.Message; }
            catch (IOException ex) { status.Text = "Could not open this reading: " + ex.Message; }
        }

        private void DeleteSelected()
        {
            var entry = Selected;
            if (entry == null) { status.Text = "Select a saved reading first."; return; }
            if (MessageBox.Show(this, "Delete this saved reading and its notes from this desktop?",
                "Delete reading", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try { journal.Delete(entry.Id); Reload(); }
            catch (IOException ex) { status.Text = "Could not delete: " + ex.Message; }
            catch (UnauthorizedAccessException ex) { status.Text = "Could not delete: " + ex.Message; }
        }
    }

    internal static class ReadingUi
    {
        public static void Configure(Form form, string title, Size size)
        {
            form.Text = title;
            form.StartPosition = FormStartPosition.CenterParent;
            form.ClientSize = size;
            form.MinimumSize = new Size(640, 500);
            form.Font = new Font("Segoe UI", 10);
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.BackColor = Color.FromArgb(29, 29, 29);
            form.ForeColor = Color.FromArgb(238, 230, 213);
            form.ShowInTaskbar = false;
            form.Shown += (s, e) =>
            {
                var area = Screen.FromControl(form.Owner ?? form).WorkingArea;
                form.MinimumSize = new Size(Math.Min(640, area.Width), Math.Min(500, area.Height));
                form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
                Rectangle ownerArea = form.Owner == null ? area : Rectangle.Intersect(area, form.Owner.RectangleToScreen(form.Owner.ClientRectangle));
                if (ownerArea.Width <= 0 || ownerArea.Height <= 0) ownerArea = area;
                form.Location = new Point(Math.Max(area.Left, Math.Min(ownerArea.Left + (ownerArea.Width - form.Width) / 2, area.Right - form.Width)),
                    Math.Max(area.Top, Math.Min(ownerArea.Top + (ownerArea.Height - form.Height) / 2, area.Bottom - form.Height)));
            };
        }

        public static TabPage Page(string title)
        {
            return new TabPage(title) { BackColor = Color.FromArgb(29, 29, 29), ForeColor = Color.FromArgb(238, 230, 213) };
        }

        public static RichTextBox ReadOnlyText(string text)
        {
            var box = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(29, 29, 29), ForeColor = Color.FromArgb(238, 230, 213),
                Font = new Font("Segoe UI", 10), Text = text ?? "", DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical, Margin = new Padding(12), TabStop = false };
            using (var heading = new Font(box.Font, FontStyle.Bold))
                foreach (string line in box.Text.Split('\n'))
                {
                    string title = line.Trim();
                    if (title.Length > 2 && title.Any(char.IsLetter) && title == title.ToUpperInvariant() && !title.StartsWith("•"))
                    {
                        int headingStart = box.Find(title, RichTextBoxFinds.MatchCase);
                        box.Select(headingStart, title.Length);
                        box.SelectionFont = heading;
                        box.SelectionColor = Color.FromArgb(225, 197, 133);
                    }
                }
            box.Select(0, 0);
            return box;
        }

        public static FlowLayoutPanel Actions()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = Padding.Empty, Margin = Padding.Empty };
        }

        public static Button Button(string title)
        {
            return new Button { Text = title, AutoSize = true, MinimumSize = new Size(100, 32),
                Height = 34, Padding = new Padding(10, 2, 10, 2), Margin = new Padding(8, 4, 0, 0),
                UseVisualStyleBackColor = true, ForeColor = Color.Black };
        }
    }
}
