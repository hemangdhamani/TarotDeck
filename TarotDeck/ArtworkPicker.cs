using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TarotCardsDeck
{
    public partial class frmTarot
    {
        private void InitializeVisualArtworkSettings(TabPage page)
        {
            foreach (Control control in page.Controls.Cast<Control>().ToArray())
                if (control != autoSpreadCheckBox) control.Visible = false;
            autoSpreadCheckBox.Location = new Point(20, 212);
            AddArtworkSummary(page, artworkSelector, false, 16);
            AddArtworkSummary(page, cardBackSelector, true, 112);
        }

        private static Bitmap ReadPreview(string path)
        {
            if (path == null || !File.Exists(path)) return null;
            try { return CardArtwork.Load(path); }
            catch (Exception error) when (error is ArgumentException || error is IOException || error is OutOfMemoryException)
            { return null; }
        }

        private static string PreviewPath(ArtworkChoice choice, bool back, string card)
        {
            if (choice == null) return null;
            if (back) return choice.Path;
            if (!Directory.Exists(choice.Path)) return null;
            string file;
            return BuildArtworkFileIndex(choice.Path).TryGetValue(card, out file) ? file : null;
        }

        private void AddArtworkSummary(TabPage page, ComboBox selector, bool back, int top)
        {
            var preview = new PictureBox { Location = new Point(20, top), Size = new Size(48, 82), SizeMode = PictureBoxSizeMode.StretchImage };
            var title = new Label { Text = back ? "Card back" : "Artwork deck", AutoSize = true, ForeColor = Color.White, Location = new Point(82, top + 3) };
            var name = new Label { ForeColor = Color.White, AutoEllipsis = true, Location = new Point(82, top + 30), Height = 45 };
            var choose = new Button { Text = "Choose…", Size = new Size(92, 28), Top = top + 26, UseVisualStyleBackColor = true,
                ForeColor = btnDrawCards.ForeColor, AccessibleName = back ? "Choose card back" : "Choose artwork deck" };
            Action refresh = () => {
                var choice = selector.SelectedItem as ArtworkChoice;
                name.Text = choice == null ? "" : choice.Name;
                Image old = preview.Image; preview.Image = ReadPreview(PreviewPath(choice, back, "maj21"));
                if (old != null) old.Dispose();
            };
            Action layout = () => { choose.Left = page.ClientSize.Width - 20 - choose.Width; name.Width = Math.Max(40, choose.Left - 94); };
            selector.SelectedIndexChanged += (s, e) => refresh();
            page.ClientSizeChanged += (s, e) => layout();
            choose.Click += (s, e) => ShowArtworkPicker(selector, back);
            preview.Disposed += (s, e) => { if (preview.Image != null) preview.Image.Dispose(); };
            page.Controls.AddRange(new Control[] { preview, title, name, choose });
            refresh(); layout();
        }

        private void ShowArtworkPicker(ComboBox selector, bool back)
        {
            using (var picker = new Form { Text = back ? "Choose card back" : "Choose artwork deck", StartPosition = FormStartPosition.CenterParent,
                Size = new Size(960, 700), MinimumSize = new Size(640, 480), BackColor = Color.FromArgb(35,35,35), Font = Font,
                AutoScaleMode = AutoScaleMode.None, MinimizeBox = false, ShowInTaskbar = false })
            {
                Rectangle work = Screen.FromControl(this).WorkingArea;
                const int imageMargin = 4;
                const int tileGap = 12;
                const int spacing = 12;
                int nameHeight = Font.Height * 2 + 6;
                // Retain the tile footprint; give the former padding to the artwork.
                Size tileSize = new Size(240, 249 + 144 + nameHeight);
                Size thumbnailSize = new Size(tileSize.Width - imageMargin * 2,
                    tileSize.Height - nameHeight - imageMargin * 3);
                int imageGap = spacing;
                Size previewSize = pcbCard01.Size;
                if (previewSize.Width < 40 || previewSize.Height < 70) previewSize = new Size(175,300);
                // Match the main card exactly unless the monitor cannot contain the dialog.
                double fit = Math.Min(1.0, Math.Min((work.Height - 280.0) / previewSize.Height,
                    (work.Width - 240.0) / previewSize.Width));
                previewSize = new Size(Math.Max(1,(int)(previewSize.Width * fit)), Math.Max(1,(int)(previewSize.Height * fit)));
                int desiredLeft = 3 * tileSize.Width + 3 * tileGap + SystemInformation.VerticalScrollBarWidth;
                picker.ClientSize = new Size(Math.Min(work.Width - 48, desiredLeft + previewSize.Width + imageGap * 2 + spacing * 2),
                    Math.Min(work.Height - 80, previewSize.Height + 58 + 36 + 95 + 52 + spacing * 2));
                var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(spacing) };
                body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty, Padding = Padding.Empty, AccessibleName = "Available artwork" };
                var detail = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Margin = Padding.Empty, Padding = new Padding(12,0,12,0) };
                detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
                var heading = new Label { Dock = DockStyle.Fill, ForeColor = Color.White, AutoEllipsis = true };
                var options = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Preview card", Visible = !back };
                options.DisplayMember = "Name";
                options.MaxDropDownItems = 18;
                options.DropDownWidth = 280;
                options.Items.AddRange(Readings.CardMeaningLibrary.All
                    .OrderBy(card => card.Id == "maj21" ? 0 : card.Id == "maj22" ? 1 : card.Id.StartsWith("maj", StringComparison.Ordinal) ? 2 :
                        card.Id.StartsWith("cups", StringComparison.Ordinal) ? 3 : card.Id.StartsWith("pents", StringComparison.Ordinal) ? 4 :
                        card.Id.StartsWith("swords", StringComparison.Ordinal) ? 5 : 6)
                    .ThenBy(card => card.Id, StringComparer.Ordinal).Cast<object>().ToArray());
                options.SelectedIndex = 0;
                var large = new PictureBox { Anchor = AnchorStyles.None, Size = previewSize, Margin = Padding.Empty, SizeMode = PictureBoxSizeMode.StretchImage };
                var status = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, AutoEllipsis = true, AccessibleName = "Artwork availability and credits" };
                detail.Controls.Add(heading,0,0); detail.Controls.Add(options,0,1); detail.Controls.Add(large,0,2); detail.Controls.Add(status,0,3);
                body.Controls.Add(grid,0,0); body.Controls.Add(detail,1,0);
                var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                var cancel = new Button { Text = "Back", DialogResult = DialogResult.Cancel, Size = new Size(110, 30), BackColor = Color.WhiteSmoke, UseVisualStyleBackColor = false, ForeColor = btnDrawCards.ForeColor };
                var use = new Button { Text = "Select", Size = new Size(110, 30), BackColor = Color.WhiteSmoke, UseVisualStyleBackColor = false, ForeColor = btnDrawCards.ForeColor };
                actions.Controls.Add(cancel); actions.Controls.Add(use); picker.Controls.Add(body); picker.Controls.Add(actions);
                picker.CancelButton = cancel; picker.AcceptButton = use;
                ArtworkChoice selected = selector.SelectedItem as ArtworkChoice;
                Action show = () => {
                    heading.Text = selected == null ? "Choose an item" : selected.Name;
                    string id = ((Readings.CardMeaning)options.SelectedItem).Id;
                    Image old = large.Image; large.Image = ReadPreview(PreviewPath(selected, back, id)); if (old != null) old.Dispose();
                    use.Enabled = selected != null && !selected.IsPlanned && large.Image != null;
                    string availability = selected != null && selected.IsPlanned ? "Coming soon — the complete 78-card deck is not available yet." : "Select returns to Settings. Apply saves your choices; Cancel discards them.";
                    string credits = "";
                    if (!back && selected != null)
                    {
                        foreach (string filename in new[] { "CREDITS.txt", "ATTRIBUTION.txt" })
                        {
                            string creditPath = Path.Combine(selected.Path, filename);
                            if (File.Exists(creditPath)) credits += "\r\n\r\n" + File.ReadAllText(creditPath).Trim();
                        }
                    }
                    status.Text = (large.Image == null ? "This preview is not available yet.\r\n" : "") + availability + credits;
                    foreach (Control tile in grid.Controls) tile.BackColor = tile.Tag == selected ? Color.LightSteelBlue : Color.WhiteSmoke;
                };
                foreach (ArtworkChoice choice in selector.Items)
                {
                    var tile = new Button { Size = tileSize, Margin = Padding.Empty, Tag = choice,
                        Text = "", ForeColor = Color.Black, TextAlign = ContentAlignment.BottomCenter,
                        FlatStyle = FlatStyle.Flat, AccessibleName = choice.ToString() };
                    var thumb = new PictureBox { Location = new Point(imageMargin, imageMargin), Size = thumbnailSize, SizeMode = PictureBoxSizeMode.StretchImage,
                        Image = ReadPreview(PreviewPath(choice,back,"maj21")), Enabled = false };
                    var caption = new Label { Text = choice.ToString(), ForeColor = Color.Black,
                        BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true,
                        Bounds = new Rectangle(imageMargin, thumb.Bottom + imageMargin, thumbnailSize.Width, nameHeight), Enabled = true };
                    caption.Click += (s,e) => { selected = choice; show(); };
                    tile.Controls.Add(caption);
                    tile.Controls.Add(thumb); tile.Click += (s,e) => { selected = choice; show(); };
                    thumb.Disposed += (s,e) => { if (thumb.Image != null) thumb.Image.Dispose(); };
                    grid.Controls.Add(tile);
                }
                // One spacing unit: outer top/left/right, row gaps and column gaps.
                // Fit the left pane to whole tiles instead of centering inside excess space.
                bool layingOut = false;
                Action arrange = () => {
                    if (layingOut || grid.Controls.Count == 0) return;
                    layingOut = true;
                    try
                    {
                        int gap = imageGap;
                        int tileWidth = grid.Controls[0].Width;
                        int available = body.ClientSize.Width - body.Padding.Horizontal;
                        int columns = Math.Max(1, Math.Min(3, (available - previewSize.Width - gap * 2 - SystemInformation.VerticalScrollBarWidth) / (tileWidth + tileGap)));
                        int leftWidth = columns * (tileWidth + tileGap) + SystemInformation.VerticalScrollBarWidth;
                        body.ColumnStyles[0].Width = leftWidth;
                        grid.SuspendLayout();
                        for (int i = 0; i < grid.Controls.Count; i++)
                        {
                            bool last = (i + 1) % columns == 0;
                            grid.Controls[i].Margin = new Padding(0, 0, tileGap, tileGap);
                            grid.SetFlowBreak(grid.Controls[i], last);
                        }
                        detail.Padding = new Padding(gap, 0, gap, 0);
                        int previewWidth = Math.Max(1, detail.ClientSize.Width - gap * 2);
                        double scale = Math.Min(1.0, Math.Min((double)previewWidth / previewSize.Width, (double)Math.Max(1, detail.ClientSize.Height - 189) / previewSize.Height));
                        large.Size = new Size(Math.Max(1, (int)(previewSize.Width * scale)), Math.Max(1, (int)(previewSize.Height * scale)));
                    }
                    finally { grid.ResumeLayout(); layingOut = false; }
                };
                body.ClientSizeChanged += (s,e) => arrange();
                picker.Shown += (s,e) => arrange();
                options.SelectedIndexChanged += (s,e) => show();
                use.Click += (s,e) => { if (use.Enabled) { picker.DialogResult = DialogResult.OK; picker.Close(); } };
                show();
                Form owner = FindForm(); Form overlay;
                if (overlayWindows.TryGetValue(settingsPanel, out overlay) && overlay.Visible) owner = overlay;
                if (picker.ShowDialog(owner) == DialogResult.OK) selector.SelectedItem = selected;
                if (large.Image != null) { large.Image.Dispose(); large.Image = null; }
            }
        }
    }
}
