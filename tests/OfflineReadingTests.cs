using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using TarotCardsDeck;
using TarotCardsDeck.Readings;

internal static class OfflineReadingTests
{
    private static int assertions;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private sealed class BlockedNetwork : IWebRequestCreate
    {
        public WebRequest Create(Uri uri) { throw new InvalidOperationException("HTTP access is disabled in offline tests."); }
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            WebRequest.RegisterPrefix("http://", new BlockedNetwork());
            WebRequest.RegisterPrefix("https://", new BlockedNetwork());
            Reject<InvalidOperationException>(() => WebRequest.Create("https://example.invalid/"), "Test HTTP network guard unavailable.");
            ContentCoverage();
            BundledDecks();
            ContextReadingChecks();
            DeterminismAndOptions();
            InvalidInputs();
            JournalRoundTrips();
            ReadingPresentation();
            UnifiedSettings();
            Integration(false);
            Integration(true);
            Console.WriteLine("PASS: " + assertions + " assertions; content, deterministic engine, journal, automatic and manual draw integration.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: " + error);
            return 1;
        }
    }

    private static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    private static void BundledDecks()
    {
        foreach (string folder in new[] { "soimoi", "woodcut-marseille", "vieville", "lequart-besancon", "botanical-art-nouveau", "japanese-ink", "celestial-oil", "rose-watercolor", "geometric-deco", "illuminated-engraving", "mauryan-sandstone", "gupta-era-mural", "ramayana-forest", "mahabharata-dharma", "the-illustrated-arcana" })
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", folder);
            Assert(Directory.Exists(directory), "Missing bundled deck: " + folder);
            var files = Directory.GetFiles(directory).Where(path => new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(path).ToLowerInvariant()) &&
                System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(path), "^(maj|cups|pents|swords|wands)[0-9]{2}$")).ToArray();
            Assert(files.Length == 78, "Deck must contain exactly 78 faces: " + folder);
            Assert(new HashSet<string>(files.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase)
                .SetEquals(CardMeaningLibrary.All.Select(card => card.Id)), "Noncanonical artwork IDs: " + folder);
            foreach (string file in files)
                using (var image = Image.FromFile(file)) Assert(image.Width >= (folder == "classic" ? 350 : folder == "soimoi" ? 720 : 750) &&
                    image.Height >= (folder == "classic" ? 600 : folder == "soimoi" ? 1080 : 1400), "Invalid or undersized image: " + file);
            if (File.Exists(Path.Combine(directory, "DISPLAY-BOUNDS.txt")))
                Assert(File.Exists(Path.Combine(directory, "SOURCE-MANIFEST.json")), "Missing original artwork provenance: " + folder);
            else if (folder != "classic" && folder != "soimoi")
                Assert(File.Exists(Path.Combine(directory, "LICENSE.txt")) && File.Exists(Path.Combine(directory, "SOURCE-MANIFEST.json")), "Missing provenance: " + folder);
        }
        Assert(!Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images")).Any(path =>
            System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(path), "^(maj|cups|pents|swords|wands)[0-9]{2}$")), "Loose card fronts remain in images root.");
        Console.WriteLine("PASS bundled decks: all 1170 faces decode in fifteen respective folders with exact canonical IDs.");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void ReadingPresentation()
    {
        var input = Snapshot("maj01", "cups02", "swords03");
        var report = ReadingEngine.Analyze(input);
        using (var window = new ReadingWindow(input, report, new ReadingJournal(Path.GetTempPath())))
        {
            var boxes = Descendants(window).OfType<RichTextBox>().ToArray();
            Assert(boxes.Length == 4, "Missing overview or detailed reading panels.");
            var overview = boxes.Single(box => box.Text.Contains("CONNECTIONS"));
            overview.Find("CONNECTIONS", RichTextBoxFinds.MatchCase);
            Assert(overview.SelectionFont != null && overview.SelectionFont.Bold, "Overview heading is not emphasized.");
            overview.Find("Explore", RichTextBoxFinds.MatchCase);
            Assert(overview.SelectionFont != null && !overview.SelectionFont.Bold, "Heading formatting leaked into body text.");
            foreach (var box in boxes.Where(box => box.Text.Contains("CORE MEANING")))
            {
                box.Find("CORE MEANING", RichTextBoxFinds.MatchCase);
                Assert(box.SelectionFont != null && box.SelectionFont.Bold, "Card detail heading is not emphasized.");
            }
            var tabs = Descendants(window).OfType<TabControl>().Single();
            var advice = Descendants(window).OfType<Button>().Single(button => button.Text.StartsWith("Advice ·"));
            typeof(Button).GetMethod("OnClick", Private).Invoke(advice, new object[] { EventArgs.Empty });
            Assert(tabs.SelectedIndex == 2, "Preview card did not open its correct detail tab.");
        }
        Console.WriteLine("PASS reading presentation: emphasized headings, plain body text, and preview navigation.");
    }

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control descendant in AllControls(child)) yield return descendant;
        }
    }

    private static void UnifiedSettings()
    {
        var preferencesType = typeof(frmTarot).Assembly.GetType("TarotDeck.Properties.Settings");
        object preferences = preferencesType.GetProperty("Default").GetValue(null, null);
        var names = new[] { "ReadingSpread", "ReadingTopic", "ReadingFocus", "AutoSpread", "ArtworkName", "CardBackName", "CardSet" };
        var saved = names.ToDictionary(name => name, name => preferencesType.GetProperty(name).GetValue(preferences, null));
        using (var form = new frmTarot())
        {
            try
            {
                Invoke(form, "frmTarot_Load", null, EventArgs.Empty); StopTimers(form);
                var panel = (Panel)Field(form, "settingsPanel");
                var tabs = panel.Controls.OfType<TabControl>().Single();
                Assert(tabs.TabPages.Count == 2 && tabs.TabPages[1].Text == "Reading setup", "Reading setup is not in Settings.");
                Assert(panel.Controls.OfType<Button>().Count(button => button.Text == "Apply") == 1 &&
                    panel.Controls.OfType<Button>().Count(button => button.Text == "Cancel") == 1, "Settings needs one shared Apply/Cancel pair.");
                Assert(!form.Controls.OfType<Button>().Any(button => button.Text == "Reading setup"), "Separate Reading setup button remains.");
                var spread = (ComboBox)Field(form, "readingSpreadSelector");
                var topic = (ComboBox)Field(form, "readingTopicSelector");
                var focus = (ComboBox)Field(form, "readingFocusSelector");
                foreach (int width in new[] { 360, 480, 720 })
                {
                    tabs.TabPages[1].Size = new Size(width, 300);
                    foreach (var selector in new[] { spread, topic, focus })
                        Assert(selector.Left == tabs.TabPages[1].ClientSize.Width - selector.Right && selector.Right < tabs.TabPages[1].ClientSize.Width,
                            "Reading dropdown overflow or unequal horizontal margins.");
                }
                foreach (bool back in new[] { false, true })
                {
                    var selection = (ComboBox)Field(form, back ? "cardBackSelector" : "artworkSelector");
                    var pending = selection.SelectedItem;
                    Exception pickerError = null;
                    using (var timer = new Timer { Interval = 100 })
                    {
                        timer.Tick += (sender, args) => {
                            timer.Stop();
                            var dialog = Application.OpenForms.Cast<Form>().Single(window => window.Text == (back ? "Choose card back" : "Choose artwork deck"));
                            try
                            {
                                using (var capture = new Bitmap(dialog.Width, dialog.Height))
                                {
                                    dialog.DrawToBitmap(capture, new Rectangle(Point.Empty, dialog.Size));
                                    capture.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, back ? "picker-back-check.png" : "picker-front-check.png"), System.Drawing.Imaging.ImageFormat.Png);
                                }
                                var controls = AllControls(dialog).ToArray();
                                var grid = controls.OfType<FlowLayoutPanel>().Single(control => control.AccessibleName == "Available artwork");
                                Assert(grid.Controls.Count == selection.Items.Count, "Picker omitted available artwork.");
                                Assert(grid.Controls.Cast<Button>().All(tile => tile.Controls.OfType<PictureBox>().Single().Image != null), "Missing artwork thumbnail.");
                                var previewChoice = controls.OfType<ComboBox>().Single();
                                if (!back)
                                {
                                    Assert(previewChoice.Items.Count == 79 && previewChoice.Items.OfType<CardMeaning>().Select(card => card.Id).Distinct().Count() == 78, "Need All plus 78 distinct preview cards.");
                                    foreach (int index in new[] { 1, 2, 21, 22, 35, 49, 63, 77, 0 }) previewChoice.SelectedIndex = index;
                                    var gallery = Descendants(dialog).OfType<FlowLayoutPanel>().Single(galleryPanel => galleryPanel.AccessibleName == "All card previews");
                                    Assert(gallery.Controls.Count == 78, "All preview omitted cards.");
                                    dialog.Width += 650; Application.DoEvents(); gallery.PerformLayout();
                                    Assert(gallery.Controls[0].Top == gallery.Controls[1].Top, "All preview does not wrap into more columns when widened.");
                                    previewChoice.SelectedIndex = 1;
                                    Assert(!gallery.Visible, "Individual preview did not hide All gallery.");
                                }
                                var planned = grid.Controls.Cast<Button>().FirstOrDefault(tile => tile.Text.Contains("Coming soon"));
                                if (planned != null)
                                {
                                    typeof(Button).GetMethod("OnClick", Private).Invoke(planned, new object[] { EventArgs.Empty });
                                    Assert(!controls.OfType<Button>().Single(button => button.Text == "Select").Enabled, "Incomplete deck can be selected.");
                                }
                            }
                            catch (Exception error) { pickerError = error; }
                            finally { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); }
                        };
                        timer.Start();
                        Invoke(form, "ShowArtworkPicker", selection, back);
                    }
                    if (pickerError != null) throw pickerError;
                    Assert(selection.SelectedItem == pending, "Picker Cancel changed pending selection.");
                }
                var auto = (CheckBox)Field(form, "autoSpreadCheckBox");
                string originalSpread = (string)Field(form, "readingSpread"), originalTopic = (string)Field(form, "readingTopic");
                bool originalAuto = auto.Checked;
                spread.SelectedItem = ReadingOptions.Spreads.First(value => value != originalSpread);
                topic.SelectedItem = originalTopic == "Career" ? "Relationships" : "Career";
                auto.Checked = !originalAuto;
                Assert((string)Field(form, "readingSpread") == originalSpread && (string)Field(form, "readingTopic") == originalTopic,
                    "Pending settings changed active reading before Apply.");
                Assert((string)focus.SelectedItem == "Overview", "Topic change did not reset dependent focus.");
                Invoke(form, "ResetPendingSettings");
                Assert((string)spread.SelectedItem == originalSpread && (string)topic.SelectedItem == originalTopic && auto.Checked == originalAuto,
                    "Cancel did not reset both tabs.");
                Invoke(form, "btnDrawCards_Click", null, EventArgs.Empty);
                var images = Pictures(form).Select(picture => picture.Image).ToArray();
                var bounds = Pictures(form).Select(picture => picture.Bounds).ToArray();
                spread.SelectedItem = ReadingOptions.Spreads.First(value => value != originalSpread);
                string newSpread = (string)spread.SelectedItem;
                topic.SelectedItem = "Career"; focus.SelectedItem = "Making a decision"; auto.Checked = !originalAuto;
                Invoke(form, "ApplySelectedSettings");
                Assert((string)Field(form, "readingSpread") == newSpread && (string)Field(form, "readingFocus") == "Making a decision",
                    "Shared Apply did not activate reading preferences.");
                Assert((bool)preferencesType.GetProperty("AutoSpread").GetValue(preferences, null) == !originalAuto &&
                    (string)preferencesType.GetProperty("ReadingSpread").GetValue(preferences, null) == newSpread, "Shared Apply did not save both tabs.");
                Assert(images.SequenceEqual(Pictures(form).Select(picture => picture.Image)) && bounds.SequenceEqual(Pictures(form).Select(picture => picture.Bounds)),
                    "Reading-only settings moved or replaced drawn cards.");
            }
            finally
            {
                StopTimers(form); Invoke(form, "frmTarot_FormClosed", null, new FormClosedEventArgs(CloseReason.None));
                foreach (var pair in saved) preferencesType.GetProperty(pair.Key).SetValue(preferences, pair.Value, null);
                preferencesType.GetMethod("Save").Invoke(preferences, null);
            }
        }
        Console.WriteLine("PASS unified settings: two tabs, shared Apply/Cancel, pending-state isolation, persistence, and preserved drawn cards.");
    }

    private static void Reject<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (TargetInvocationException error) { if (error.InnerException is T) { assertions++; return; } throw; }
        catch (T) { assertions++; return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + message);
    }

    private static ReadingCard Card(string id)
    {
        var meaning = CardMeaningLibrary.Find(id);
        return new ReadingCard { Id = id, Name = meaning.Name, ImagePath = "missing-artwork-is-allowed.png" };
    }

    private static ReadingSnapshot Snapshot(params string[] ids)
    {
        return ReadingSnapshot.Create(ids.Select(Card), ReadingOptions.Spreads[0], "General", "Overview", "The Illustrated Arcana");
    }

    private static void ContextReadingChecks()
    {
        var snapshot = Snapshot("maj01", "cups01", "pents06");
        Assert(ReadingEngine.Analyze(snapshot).Context.Length == 0, "Old three-card readings gained context.");
        snapshot.FirstCard = Card("cups13"); snapshot.LastCard = Card("cups06");
        var report = ReadingEngine.Analyze(snapshot);
        Assert(report.Context.Any(line => line.Contains("OPENING THEME")) && report.Context.Any(line => line.Contains("UNDERLYING THEME")), "Outer meanings missing.");
        Assert(report.Text.Contains("OUTER CARD CONTEXT") && report.CardDetails.Length == 3, "Context displaced core spread.");
        Assert(report.Text == ReadingEngine.Analyze(snapshot).Text, "Context reading is nondeterministic.");
        foreach (int outerCount in new[] { 2, 1, 0 })
        {
            snapshot.FirstCard = outerCount > 0 ? Card("cups13") : null;
            snapshot.LastCard = outerCount == 2 ? Card("cups06") : null;
            var current = ReadingEngine.Analyze(snapshot);
            using (var window = new ReadingWindow(snapshot, current, new ReadingJournal(Path.Combine(Path.GetTempPath(), "TarotOverviewChecks"))))
            {
                var controls = Descendants(window).ToArray();
                var strip = controls.OfType<TableLayoutPanel>().Single(control => control.AccessibleName == "Your three reading cards");
                Assert(strip.ColumnCount == 3 + outerCount && strip.Controls.Count == 3 + outerCount, "Overview left empty outer-card slots.");
                var previews = Descendants(strip).OfType<PictureBox>().ToArray();
                Assert(previews.Select(picture => picture.Size).Distinct().Count() == 1, "Overview thumbnails differ in size.");
                if (outerCount > 0)
                {
                    var open = Descendants(strip).OfType<Button>().Single(button => button.Text.StartsWith("First ·"));
                    typeof(Button).GetMethod("OnClick", Private).Invoke(open,new object[]{EventArgs.Empty});
                    Assert(controls.OfType<TabControl>().Single().SelectedTab.Text.StartsWith("First ·"), "First card preview navigation failed.");
                }
            }
        }
        snapshot.FirstCard = Card("cups13"); snapshot.LastCard = Card("cups06");
        using (var stream = new MemoryStream())
        {
            var serializer = new XmlSerializer(typeof(ReadingSnapshot)); serializer.Serialize(stream,snapshot); stream.Position=0;
            var copy = (ReadingSnapshot)serializer.Deserialize(stream); copy.Validate();
            Assert(copy.FirstCard.Id == snapshot.FirstCard.Id && copy.LastCard.Id == snapshot.LastCard.Id, "Outer cards lost during persistence.");
        }
        snapshot.LastCard = Card("maj01");
        Reject<ArgumentException>(() => snapshot.Validate(), "Duplicate outer card accepted.");
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"images","the-illustrated-arcana","pents06.png");
        var loader = typeof(frmTarot).Assembly.GetType("TarotCardsDeck.CardArtwork").GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic);
        using (var image = (Bitmap)loader.Invoke(null,new object[]{path}))
            Assert(image.Width == 958 && image.Height < 1642 && image.Height > 1500, "Packaging padding was not removed from Six of Pentacles display.");
        Console.WriteLine("PASS outer context: optional roles, distinct cards, deterministic text, persistence, and artwork display bounds.");
    }

    private static void ContentCoverage()
    {
        Assert(CardMeaningLibrary.All.Count == 78, "Expected 78 meanings.");
        Assert(CardMeaningLibrary.All.Select(card => card.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 78, "Duplicate meaning IDs.");
        var expected = new Deck(false).cards.Select(card => Path.GetFileNameWithoutExtension(card.CardImg)).OrderBy(id => id).ToArray();
        Assert(expected.SequenceEqual(CardMeaningLibrary.All.Select(card => card.Id).OrderBy(id => id)), "Meaning IDs differ from real deck IDs.");
        foreach (var card in CardMeaningLibrary.All)
        {
            Assert(new[] { card.Id, card.Name, card.Suit, card.Keywords, card.Meaning, card.Challenge, card.Action, card.Reflection }
                .All(value => !string.IsNullOrWhiteSpace(value)), "Incomplete content for " + card.Id);
            Assert(card.Meaning.Length >= 45 && card.Action.Length >= 20 && card.Reflection.EndsWith("?"), "Unfinished editorial content for " + card.Id);
            Assert(!card.Meaning.Contains("TODO") && !card.Meaning.Contains("TBD"), "Placeholder meaning for " + card.Id);
        }
        Assert(CardMeaningLibrary.Find("maj01").Name == "The Magician", "maj01 mapping drift.");
        Assert(CardMeaningLibrary.Find("maj08").Name == "Strength", "maj08 mapping drift.");
        Assert(CardMeaningLibrary.Find("maj11").Name == "Justice", "maj11 mapping drift.");
        Assert(CardMeaningLibrary.Find("maj21").Name == "The World", "maj21 mapping drift.");
        Assert(CardMeaningLibrary.Find("maj22").Name == "The Fool", "maj22 mapping drift.");
        Assert(CardMeaningLibrary.Find("cups11").Name.Contains("Page"), "Court rank mapping drift.");
        Assert(CardMeaningLibrary.Find("pents14").Name.Contains("King"), "Court rank mapping drift.");
        Console.WriteLine("PASS content: 78 canonical IDs and complete original entries, including asset-specific Major Arcana mapping.");
    }

    private static void DeterminismAndOptions()
    {
        Assert(ReadingOptions.Spreads[0] == "Situation / Advice / Outcome", "Default spread differs from requested choice.");
        Assert(ReadingOptions.Positions(ReadingOptions.Spreads[0]).SequenceEqual(new[] { "Situation", "Advice", "Outcome" }), "Default spread position order incorrect.");
        var defaultReport = ReadingEngine.Analyze(Snapshot("maj01", "cups02", "swords03"));
        Assert(defaultReport.CardDetails[1].Contains("In the advice position"), "Second card does not use Advice guidance.");
        Assert(defaultReport.CardDetails[2].Contains("In the outcome position") && defaultReport.CardDetails[2].Contains("conditional"), "Third card lacks conditional Outcome guidance.");
        var cards = CardMeaningLibrary.All.ToArray();
        int combinations = 0;
        for (int i = 0; i < cards.Length; i++)
        foreach (string spread in ReadingOptions.Spreads)
        foreach (string topic in ReadingOptions.Topics)
        foreach (string focus in ReadingOptions.Focuses(topic))
        {
            var input = ReadingSnapshot.Create(new[] { Card(cards[i].Id), Card(cards[(i + 1) % 78].Id), Card(cards[(i + 17) % 78].Id) }, spread, topic, focus, "Any front artwork");
            string[] before = input.Cards.Select(card => card.Id).ToArray();
            var first = ReadingEngine.Analyze(input);
            var second = ReadingEngine.Analyze(input);
            Assert(first.Text == second.Text, "Non-deterministic reading.");
            Assert(first.CardDetails.Length == 3 && first.CardDetails.All(detail => detail.Length > 100), "Missing per-card detail.");
            Assert(first.Actions.Length >= 2 && first.Actions.Length <= 3 && first.Actions.Distinct().Count() == first.Actions.Length, "Invalid or duplicated actions.");
            Assert(first.Connections.Length > 0 && first.Reflections.Length > 0, "Missing synthesis.");
            Assert(before.SequenceEqual(input.Cards.Select(card => card.Id)), "Analysis changed order.");
            input.Notes = "A private personal reflection that must not become analysis input.";
            Assert(first.Text == ReadingEngine.Analyze(input).Text, "Journal notes affected interpretation.");
            combinations++;
        }
        Assert(combinations == cards.Length * ReadingOptions.Spreads.Length * ReadingOptions.Topics.Sum(topic => ReadingOptions.Focuses(topic).Length), "Not all structured combinations were covered.");
        Assert(!typeof(ReadingSnapshot).GetProperties().Any(property => property.Name.IndexOf("Question", StringComparison.OrdinalIgnoreCase) >= 0 || property.Name.IndexOf("Context", StringComparison.OrdinalIgnoreCase) >= 0), "Free-form question input appeared in model.");
        var copies = new[] { Card("maj01"), Card("cups02"), Card("wands03") };
        var snapshot = ReadingSnapshot.Create(copies, ReadingOptions.Spreads[0], "General", "Overview", "Artwork");
        copies[0].Id = "changed";
        Assert(snapshot.Cards[0].Id == "maj01", "Snapshot retains source card reference.");
        Console.WriteLine("PASS deterministic engine: " + combinations + " spread/topic/focus combinations, notes isolated, copied input cards.");
    }

    private static void InvalidInputs()
    {
        Reject<ArgumentException>(() => Snapshot("cups01", "cups01", "cups02"), "Duplicate cards accepted.");
        Reject<ArgumentException>(() => Snapshot("cups01", "CUPS01", "cups02"), "Case-variant duplicate cards accepted.");
        Reject<ArgumentException>(() => Snapshot("CUPS01", "cups02", "cups03"), "Noncanonical ID accepted.");
        Reject<ArgumentException>(() => Snapshot("cups01", "cups02"), "Incomplete spread accepted.");
        Reject<ArgumentException>(() => Snapshot("cups01", "cups02", "not-a-card"), "Unknown card accepted.");
        Reject<ArgumentException>(() => ReadingOptions.Validate("Unknown", "General", "Overview"), "Unknown spread accepted.");
        Reject<ArgumentException>(() => ReadingOptions.Validate(ReadingOptions.Spreads[0], "Unknown", "Overview"), "Unknown topic accepted.");
        Reject<ArgumentException>(() => ReadingOptions.Validate(ReadingOptions.Spreads[0], "Career", "Healthy boundaries"), "Cross-topic focus accepted.");
        var input = Snapshot("maj01", "cups02", "swords03");
        input.Cards[0].Orientation = "Reversed";
        Reject<ArgumentException>(() => ReadingEngine.Analyze(input), "Unsupported reversal accepted.");
        input.Cards[0].Orientation = "Upright";
        input.Cards[0].Name = "Incorrect name";
        Reject<ArgumentException>(() => ReadingEngine.Analyze(input), "Mismatched card name accepted.");
        input.Cards[0] = Card("maj01"); input.Notes = new string('x', 16001);
        Reject<ArgumentException>(() => input.Validate(), "Oversized notes accepted.");
        Console.WriteLine("PASS input validation: duplicates, missing/unknown cards, unsupported orientation and structured options rejected.");
    }

    private static void JournalRoundTrips()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TarotDeck-reading-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var journal = new ReadingJournal(Path.Combine(directory, "journal"));
            var input = Snapshot("maj13", "cups08", "swords06");
            input.Report = ReadingEngine.Analyze(input); input.Interpretation = input.Report.Text; input.Notes = "My reflection: café & <change>.";
            journal.Save(input);
            var loaded = journal.List();
            Assert(loaded.Count == 1, "Saved reading missing.");
            Assert(loaded[0].Notes == input.Notes && loaded[0].Interpretation == input.Interpretation, "XML text round trip changed content.");
            Assert(loaded[0].Cards.Select(card => card.Id).SequenceEqual(input.Cards.Select(card => card.Id)), "Round trip changed order.");
            Assert(loaded[0].Report.CardDetails.SequenceEqual(input.Report.CardDetails), "Round trip lost detailed report.");
            loaded[0].ContentVersion = "future-editorial-version";
            loaded[0].Report.Summary = "HISTORICAL SUMMARY MUST REMAIN";
            loaded[0].Interpretation = "HISTORICAL ORIGINAL TEXT";
            Assert(ReadingReport.FromSnapshot(loaded[0]).Summary == "HISTORICAL SUMMARY MUST REMAIN", "History regenerated with new content version.");
            journal.Save(loaded[0]);
            Assert(journal.List().Single().Report.Summary == "HISTORICAL SUMMARY MUST REMAIN", "Stored original summary replaced.");
            input.Notes = "Updated reflection"; journal.Save(input);
            Assert(journal.List().Count == 1 && journal.List()[0].Notes == "Updated reflection", "Overwrite created duplicate or lost update.");
            Assert(Directory.GetFiles(Path.Combine(directory, "journal"), "*.tmp").Length == 0, "Temporary save file leaked.");
            var second = Snapshot("pents01", "wands02", "cups03"); second.Report = ReadingEngine.Analyze(second); second.Interpretation = second.Report.Text; journal.Save(second);
            string outside = Path.Combine(directory, "outside.xml"); File.WriteAllText(outside, "keep");
            Reject<ArgumentException>(() => journal.Delete("../outside"), "Traversal ID accepted.");
            Assert(File.Exists(outside), "Traversal deleted unrelated file.");
            Reject<ArgumentException>(() => journal.Delete(Path.GetFullPath(outside)), "Absolute path accepted as ID.");
            string historyDirectory = Path.Combine(directory, "journal");
            File.WriteAllText(Path.Combine(historyDirectory, "malformed.xml"), "<ReadingSnapshot>");
            File.WriteAllText(Path.Combine(historyDirectory, "oversized.xml"), new string('x', 1000001));
            File.WriteAllText(Path.Combine(historyDirectory, "external-entity.xml"), "<!DOCTYPE ReadingSnapshot [<!ENTITY x SYSTEM 'file:///must-not-read'>]><ReadingSnapshot>&x;</ReadingSnapshot>");
            Assert(journal.List().Count == 2 && journal.LastLoadWarning.Contains("3 unreadable"), "Malformed/oversized/DTD files not skipped with warning.");
            Assert(File.Exists(Path.Combine(historyDirectory, "malformed.xml")), "Corrupt history was silently deleted.");
            string export = Path.Combine(directory, "export.txt"); journal.Export(input, export);
            string exported = File.ReadAllText(export);
            Assert(exported.Contains(input.Interpretation) && exported.Contains(input.Notes) && exported.Contains("PERSONAL REFLECTION NOTES"), "Text export omitted interpretation or notes.");
            journal.Delete(input.Id);
            Assert(journal.List().Count == 1 && journal.List()[0].Id == second.Id, "Delete affected another saved reading.");
            Assert(File.Exists(outside), "Unrelated local file changed.");
            var unsaved = Snapshot("maj01", "maj02", "maj03");
            Reject<ArgumentException>(() => journal.Save(unsaved), "Incomplete interpretation saved.");
            Console.WriteLine("PASS journal: XML, overwrite/delete isolation, historical text, UTF-8 export, traversal and corrupt-file handling.");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static object Field(object instance, string name)
    {
        var field = instance.GetType().GetField(name, Private);
        if (field == null) throw new Exception("Missing integration field " + name);
        return field.GetValue(instance);
    }

    private static object Invoke(object instance, string name, params object[] arguments)
    {
        return instance.GetType().GetMethod(name, Private).Invoke(instance, arguments);
    }

    private static void StopTimers(frmTarot form)
    {
        foreach (string name in new[] { "spreadRevealTimer", "shufflePileTimer" })
        {
            var timer = Field(form, name) as Timer;
            if (timer != null) { timer.Stop(); timer.Dispose(); form.GetType().GetField(name, Private).SetValue(form, null); }
        }
    }

    private static PictureBox[] Pictures(frmTarot form)
    {
        return new[] { "pcbCard01", "pcbCard02", "pcbCard03" }.Select(name => (PictureBox)Field(form, name)).ToArray();
    }

    private static string[] VisibleIds(frmTarot form)
    {
        return Pictures(form).Select(picture => picture.Tag as Card).Select(card => card == null ? null : Path.GetFileNameWithoutExtension(card.CardImg).ToLowerInvariant()).ToArray();
    }

    private static Dictionary<int, RectangleF> FanBounds(frmTarot form)
    {
        var result = new Dictionary<int, RectangleF>();
        var slots = (PictureBox[])Field(form, "pcbDeck");
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) result[i] = (RectangleF)Invoke(form, "FanCardBounds", i, 0f);
        return result;
    }

    private static void Integration(bool manual)
    {
        using (var form = new frmTarot())
        {
            try
            {
                Invoke(form, "frmTarot_Load", null, EventArgs.Empty); StopTimers(form);
                var artwork = (ComboBox)Field(form, "artworkSelector");
                var artworkNames = artwork.Items.Cast<object>().Select(item => item.ToString()).ToArray();
                Assert(artworkNames.SequenceEqual(artworkNames.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)), "Artwork deck dropdown is not alphabetical.");
                int originalArtwork = artwork.SelectedIndex;
                typeof(frmTarot).GetField("loadingSelections", Private).SetValue(form, true);
                foreach (string title in new[] { "The Illustrated Arcana", "Soimoi Tarot", "Woodcut Marseille", "Tarot de Vieville", "Lequart Besançon" })
                {
                    int index = Enumerable.Range(0, artwork.Items.Count).FirstOrDefault(i => artwork.Items[i].ToString() == title);
                    Assert(artwork.Items[index].ToString() == title, "Bundled deck not discovered: " + title);
                    artwork.SelectedIndex = index;
                    Invoke(form, "RefreshArtworkFileIndex");
                    foreach (var meaning in CardMeaningLibrary.All)
                        Assert(File.Exists((string)Invoke(form, "FindArtworkFile", meaning.Id + ".jpg")), "Bundled image not resolved: " + title + " / " + meaning.Id);
                    if (title != "Soimoi Tarot") Assert(!((Button)Field(form, "creditsButton")).Visible, "Public-domain deck should not require credits display.");
                }
                artwork.SelectedIndex = originalArtwork;
                Invoke(form, "RefreshArtworkFileIndex");
                typeof(frmTarot).GetField("loadingSelections", Private).SetValue(form, false);
                var readButton = (Button)Field(form, "readCardsButton");
                Assert(!readButton.Enabled, "Reading available before any draw.");
                Reject<InvalidOperationException>(() => Invoke(form, "CaptureDisplayedReading"), "Empty reading accepted.");
                if (manual)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Card chosen = form.deck.cards[1];
                        var picture = ((PictureBox[])Field(form, "pcbDeck")).Single(slot => slot != null && ReferenceEquals(slot.Tag, chosen));
                        Invoke(form, "pcbDeck_Click", picture, EventArgs.Empty);
                        Assert(readButton.Enabled == (i == 2), "Manual reading availability does not match filled slots.");
                        if (i < 2) Reject<InvalidOperationException>(() => Invoke(form, "CaptureDisplayedReading"), "Partial manual reading accepted.");
                    }
                }
                else Invoke(form, "btnDrawCards_Click", null, EventArgs.Empty);
                Assert(readButton.Enabled, "Read Cards disabled for full spread.");
                Assert(Pictures(form).All(picture => picture.Image != null && picture.Tag is Card), "Displayed image lacks canonical Card tag.");
                var deckList = form.deck.cards;
                var deckBefore = deckList.ToArray(); var drawnBefore = form.drawn.cards.ToArray();
                var images = Pictures(form).Select(picture => picture.Image).ToArray();
                var bounds = Pictures(form).Select(picture => picture.Bounds).ToArray(); var fanBounds = FanBounds(form);
                var first = (ReadingSnapshot)Invoke(form, "CaptureDisplayedReading");
                var second = (ReadingSnapshot)Invoke(form, "CaptureDisplayedReading");
                Assert(first.Cards.Select(card => card.Id).SequenceEqual(VisibleIds(form)), "Captured order disagrees with left-to-right display.");
                Assert(ReadingEngine.Analyze(first).Text == ReadingEngine.Analyze(second).Text, "Same display produced different readings.");
                Assert(ReferenceEquals(deckList, form.deck.cards) && deckBefore.SequenceEqual(form.deck.cards) && drawnBefore.SequenceEqual(form.drawn.cards), "Capture or analysis changed live deck state.");
                Assert(images.SequenceEqual(Pictures(form).Select(picture => picture.Image)), "Reading replaced drawn images.");
                Assert(bounds.SequenceEqual(Pictures(form).Select(picture => picture.Bounds)), "Reading moved drawn cards.");
                Assert(fanBounds.All(pair => FanBounds(form)[pair.Key] == pair.Value), "Reading changed fan geometry.");
                Invoke(form, "btnDrawCards_Click", null, EventArgs.Empty);
                Assert(form.drawn.cards.Count == 6, "Second draw failed to form next page.");
                Invoke(form, "ShowPage", -1);
                Assert(first.Cards.Select(card => card.Id).SequenceEqual(((ReadingSnapshot)Invoke(form, "CaptureDisplayedReading")).Cards.Select(card => card.Id)), "Earlier page no longer maps to its visible cards.");
                Invoke(form, "ShowPage", 1);
                Assert(((ReadingSnapshot)Invoke(form, "CaptureDisplayedReading")).Cards.Select(card => card.Id).SequenceEqual(VisibleIds(form)), "Later page capture order mismatch.");
                Invoke(form, "btnShufCards_Click", null, EventArgs.Empty); StopTimers(form);
                Assert(!readButton.Enabled && Pictures(form).All(picture => picture.Image == null && picture.Tag == null), "Shuffle left stale reading metadata.");
                Reject<InvalidOperationException>(() => Invoke(form, "CaptureDisplayedReading"), "Reading accepted after shuffle cleared cards.");
                Console.WriteLine("PASS " + (manual ? "manual fan" : "Draw Cards") + " integration: order, availability, snapshots, state preservation, paging and shuffle.");
            }
            finally
            {
                StopTimers(form);
                Invoke(form, "frmTarot_FormClosed", null, new FormClosedEventArgs(CloseReason.None));
            }
        }
    }
}
