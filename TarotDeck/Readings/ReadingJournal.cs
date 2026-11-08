using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TarotCardsDeck.Readings
{
    public sealed class ReadingJournal
    {
        private readonly string directory;
        public string LastLoadWarning { get; private set; }
        public ReadingJournal() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TarotDeck", "Readings")) { }
        public ReadingJournal(string directory) { this.directory = Path.GetFullPath(directory); }
        public void Save(ReadingSnapshot snapshot)
        {
            snapshot.Validate();
            if (string.IsNullOrWhiteSpace(snapshot.Interpretation) || snapshot.Report == null || snapshot.Report.CardDetails == null || snapshot.Report.CardDetails.Length != 3 ||
                snapshot.Report.Connections == null || snapshot.Report.Actions == null || snapshot.Report.Reflections == null)
                throw new ArgumentException("Only complete interpretations can be saved.");
            Directory.CreateDirectory(directory);
            string path = PathFor(snapshot.Id), temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, NewLineHandling = NewLineHandling.Entitize }))
                    new XmlSerializer(typeof(ReadingSnapshot)).Serialize(writer, snapshot);
                if (new FileInfo(temporary).Length > 1000000) throw new InvalidDataException("Reading exceeds the supported size.");
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public List<ReadingSnapshot> List()
        {
            var entries = new List<ReadingSnapshot>(); int skipped = 0;
            LastLoadWarning = "";
            if (!Directory.Exists(directory)) return entries;
            foreach (string path in Directory.GetFiles(directory, "*.xml"))
            {
                try {
                    if (new FileInfo(path).Length > 1000000) throw new InvalidDataException("Oversized reading.");
                    using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1000000 }))
                    {
                        var entry = (ReadingSnapshot)new XmlSerializer(typeof(ReadingSnapshot)).Deserialize(reader);
                        entry.Validate();
                        if (Path.GetFileNameWithoutExtension(path) != entry.Id || string.IsNullOrWhiteSpace(entry.Interpretation) || entry.Report == null ||
                            entry.Report.CardDetails == null || entry.Report.CardDetails.Length != 3 || entry.Report.Connections == null || entry.Report.Actions == null || entry.Report.Reflections == null)
                            throw new InvalidDataException("Incomplete reading.");
                        entries.Add(entry);
                    }
                }
                catch (Exception error) when (error is InvalidOperationException || error is ArgumentException || error is XmlException || error is InvalidDataException || error is IOException || error is UnauthorizedAccessException)
                { skipped++; }
            }
            if (skipped > 0) LastLoadWarning = skipped + " unreadable reading file(s) were skipped and left unchanged.";
            return entries.OrderByDescending(entry => entry.CreatedAt, StringComparer.Ordinal).ThenBy(entry => entry.Id, StringComparer.Ordinal).ToList();
        }
        public void Delete(string id) { File.Delete(PathFor(id)); }
        public void Export(ReadingSnapshot snapshot, string path)
        {
            snapshot.Validate();
            File.WriteAllText(path, "Saved: " + snapshot.CreatedAt + Environment.NewLine + "Artwork: " + snapshot.Artwork + Environment.NewLine +
                snapshot.Interpretation + Environment.NewLine + "PERSONAL REFLECTION NOTES" + Environment.NewLine + (snapshot.Notes ?? ""), new UTF8Encoding(false));
        }
        private string PathFor(string id)
        {
            Guid parsed; if (!Guid.TryParseExact(id, "N", out parsed)) throw new ArgumentException("Invalid reading identifier.");
            return Path.Combine(directory, id + ".xml");
        }
    }
}
