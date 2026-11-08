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
            DeterminismAndOptions();
            InvalidInputs();
            JournalRoundTrips();
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
        foreach (string folder in new[] { "woodcut-marseille", "vieville", "lequart-besancon" })
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", folder);
            Assert(Directory.Exists(directory), "Missing bundled deck: " + folder);
            var files = Directory.GetFiles(directory).Where(path => new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(path).ToLowerInvariant())).ToArray();
            Assert(files.Length == 78, "Deck must contain exactly 78 faces: " + folder);
            Assert(new HashSet<string>(files.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase)
                .SetEquals(CardMeaningLibrary.All.Select(card => card.Id)), "Noncanonical artwork IDs: " + folder);
            foreach (string file in files)
                using (var image = Image.FromFile(file)) Assert(image.Width >= 750 && image.Height >= 1400, "Invalid or undersized image: " + file);
            Assert(File.Exists(Path.Combine(directory, "LICENSE.txt")) && File.Exists(Path.Combine(directory, "SOURCE-MANIFEST.json")), "Missing provenance: " + folder);
        }
        Console.WriteLine("PASS bundled decks: 234 high-resolution faces decode with exact canonical IDs and provenance.");
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
        return ReadingSnapshot.Create(ids.Select(Card), ReadingOptions.Spreads[0], "General", "Overview", "Classic Tarot");
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
        Assert(combinations == 3744, "Expected all 78 × 3 × 4 × 4 structured combinations.");
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
                int originalArtwork = artwork.SelectedIndex;
                typeof(frmTarot).GetField("loadingSelections", Private).SetValue(form, true);
                foreach (string title in new[] { "Woodcut Marseille", "Tarot de Vieville", "Lequart Besançon" })
                {
                    int index = Enumerable.Range(0, artwork.Items.Count).FirstOrDefault(i => artwork.Items[i].ToString() == title);
                    Assert(artwork.Items[index].ToString() == title, "Bundled deck not discovered: " + title);
                    artwork.SelectedIndex = index;
                    Invoke(form, "RefreshArtworkFileIndex");
                    foreach (var meaning in CardMeaningLibrary.All)
                        Assert(File.Exists((string)Invoke(form, "FindArtworkFile", meaning.Id + ".jpg")), "Bundled image not resolved: " + title + " / " + meaning.Id);
                    Assert(!((Button)Field(form, "creditsButton")).Visible, "Public-domain deck should not require credits display.");
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
