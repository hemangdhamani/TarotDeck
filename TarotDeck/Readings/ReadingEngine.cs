using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TarotCardsDeck.Readings
{
    public static class ReadingOptions
    {
        public static readonly string[] Spreads = { "Situation / Advice / Outcome", "Situation / Challenge / Advice", "Past / Present / Possible direction", "Mind / Body / Spirit" };
        public static readonly string[] Topics = { "General", "Relationships", "Career", "Personal growth" };
        public static string[] Focuses(string topic)
        {
            switch (topic)
            {
                case "General": return new[] { "Overview", "Making a decision", "Understanding an obstacle", "Choosing a next step" };
                case "Relationships": return new[] { "Overview", "Improving communication", "Rebuilding trust", "Healthy boundaries" };
                case "Career": return new[] { "Overview", "Making a decision", "Working through an obstacle", "Choosing a next step" };
                case "Personal growth": return new[] { "Overview", "Building confidence", "Changing a habit", "Finding balance" };
                default: throw new ArgumentException("Choose a supported reading topic.");
            }
        }
        public static string[] Positions(string spread)
        {
            if (!Spreads.Contains(spread)) throw new ArgumentException("Choose a supported reading spread.");
            return spread.Split(new[] { " / " }, StringSplitOptions.None);
        }
        public static void Validate(string spread, string topic, string focus)
        {
            Positions(spread);
            if (!Focuses(topic).Contains(focus)) throw new ArgumentException("Choose a focus for this topic.");
        }
    }

    public sealed class ReadingCard
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string Orientation { get; set; }
        public ReadingCard() { Orientation = "Upright"; }
        public ReadingCard Copy() { return new ReadingCard { Id = Id, Name = Name, ImagePath = ImagePath, Orientation = Orientation }; }
    }

    public sealed class ReadingSnapshot
    {
        public string Id { get; set; }
        public string CreatedAt { get; set; }
        public string Spread { get; set; }
        public string Topic { get; set; }
        public string Focus { get; set; }
        public string Artwork { get; set; }
        public string ContentVersion { get; set; }
        public ReadingCard[] Cards { get; set; }
        public string Notes { get; set; }
        public string Interpretation { get; set; }
        public ReadingReport Report { get; set; }
        public ReadingSnapshot() { Cards = new ReadingCard[0]; Notes = ""; }

        public static ReadingSnapshot Create(IEnumerable<ReadingCard> cards, string spread, string topic, string focus, string artwork)
        {
            var result = new ReadingSnapshot { Id = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Spread = spread, Topic = topic, Focus = focus, Artwork = artwork,
                ContentVersion = CardMeaningLibrary.ContentVersion, Cards = cards.Select(card => card.Copy()).ToArray() };
            result.Validate();
            return result;
        }
        public void Validate()
        {
            Guid parsed; DateTimeOffset date;
            if (!Guid.TryParseExact(Id, "N", out parsed) || !DateTimeOffset.TryParse(CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
                throw new ArgumentException("Invalid reading identifier or date.");
            ReadingOptions.Validate(Spread, Topic, Focus);
            if (Cards == null || Cards.Length != 3 || Cards.Any(card => card == null) || Cards.Select(card => card.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
                throw new ArgumentException("A reading requires three distinct cards.");
            foreach (var card in Cards)
            {
                var meaning = CardMeaningLibrary.Find(card.Id);
                if (card.Id != meaning.Id || card.Name != meaning.Name || card.Orientation != "Upright") throw new ArgumentException("Invalid card identity or unsupported orientation.");
            }
            if (string.IsNullOrWhiteSpace(ContentVersion) || ContentVersion.Length > 80 || (Notes ?? "").Length > 16000)
                throw new ArgumentException("Invalid content version or notes longer than 16,000 characters.");
        }
    }

    public sealed class ReadingReport
    {
        public string Summary { get; set; }
        public string[] CardDetails { get; set; }
        public string[] Connections { get; set; }
        public string[] Actions { get; set; }
        public string[] Reflections { get; set; }
        public string Text { get; set; }
        public ReadingReport() { CardDetails = new string[0]; Connections = new string[0]; Actions = new string[0]; Reflections = new string[0]; }
        public static ReadingReport FromSnapshot(ReadingSnapshot snapshot)
        {
            snapshot.Validate();
            // Reopen historical words as saved, without rerunning a newer engine.
            return snapshot.Report ?? new ReadingReport { Summary = "Saved reading", Text = snapshot.Interpretation ?? "" };
        }
    }

    public static class ReadingEngine
    {
        public static ReadingReport Analyze(ReadingSnapshot snapshot)
        {
            snapshot.Validate();
            if (snapshot.ContentVersion != CardMeaningLibrary.ContentVersion)
                throw new ArgumentException("Reopen the saved interpretation, or create a new reading with the current content version.");
            var meanings = snapshot.Cards.Select(card => CardMeaningLibrary.Find(card.Id)).ToArray();
            var positions = ReadingOptions.Positions(snapshot.Spread);
            var connections = Connections(meanings, positions).Take(3).ToArray();
            var details = meanings.Select((card, index) => Explain(card, positions[index], snapshot.Topic, snapshot.Focus)).ToArray();
            int adviceIndex = Array.IndexOf(positions, "Advice");
            int practicalIndex = adviceIndex >= 0 ? adviceIndex : 2;
            var actions = new[] { TopicAction(meanings[practicalIndex].Action, snapshot.Topic), FocusAction(snapshot.Focus, snapshot.Topic), TopicAction(meanings[0].Action, snapshot.Topic) }
                .Distinct(StringComparer.Ordinal).Take(3).ToArray();
            var report = new ReadingReport {
                Summary = string.Join(Environment.NewLine + Environment.NewLine, meanings.Select((card, index) =>
                    positions[index] + " — " + card.Name + ": " + card.Keywords + ".")) +
                    Environment.NewLine + Environment.NewLine + "Explore these themes through " + snapshot.Topic.ToLowerInvariant() +
                    (snapshot.Focus == "Overview" ? "." : ", focusing on " + snapshot.Focus.ToLowerInvariant() + "."),
                CardDetails = details, Connections = connections, Actions = actions,
                Reflections = new[] { meanings[0].Reflection, meanings[2].Reflection }.Distinct().ToArray()
            };
            var text = new StringBuilder();
            text.AppendLine("TAROT READING — OFFLINE").AppendLine(snapshot.Spread)
                .AppendLine(snapshot.Topic + " · " + snapshot.Focus).AppendLine("Upright meanings · Content " + snapshot.ContentVersion)
                .AppendLine().AppendLine("OVERVIEW").AppendLine(report.Summary);
            for (int i = 0; i < 3; i++) text.AppendLine().AppendLine(positions[i].ToUpperInvariant() + " — " + meanings[i].Name).AppendLine(details[i]);
            Append(text, "CONNECTIONS", report.Connections); Append(text, "NEXT STEPS", report.Actions); Append(text, "REFLECTION", report.Reflections);
            text.AppendLine().AppendLine("Symbolic guidance for reflection. Possible directions depend on your choices and circumstances.");
            report.Text = text.ToString();
            return report;
        }
        private static void Append(StringBuilder text, string title, IEnumerable<string> lines)
        { text.AppendLine().AppendLine(title); foreach (string line in lines) text.AppendLine("• " + line); }

        private static string Explain(CardMeaning card, string position, string topic, string focus)
        {
            string role;
            switch (position)
            {
                case "Situation": role = "Use this card to name a pattern active in the situation, rather than as proof of an external fact."; break;
                case "Challenge": role = "In the challenge position, examine this card's difficult expression: " + card.Challenge; break;
                case "Advice": role = "In the advice position, try this constructive response: " + card.Action; break;
                case "Outcome": role = "In the outcome position, consider a possible direction if the current pattern continues. This is conditional on your choices and circumstances, rather than a fixed prediction."; break;
                case "Past": role = "Look back for an experience or learned pattern that fits this theme. You decide whether it resonates."; break;
                case "Present": role = "Notice where this theme is active now, especially in something you can influence."; break;
                case "Possible direction": role = "This is a possible pattern to explore if present habits continue, not a fixed prediction. " + card.Action; break;
                case "Mind": role = "Examine the thoughts and assumptions associated with this theme."; break;
                case "Body": role = "Consider your daily rhythm, rest, and practical habits through this theme. This position does not assess physical health."; break;
                default: role = "Consider how this theme relates to meaning, values, and a sense of connection."; break;
            }
            return "KEYWORDS" + Environment.NewLine + card.Keywords + Environment.NewLine + Environment.NewLine +
                "CORE MEANING" + Environment.NewLine + card.Meaning + Environment.NewLine + Environment.NewLine +
                "YOUR " + position.ToUpperInvariant() + " POSITION" + Environment.NewLine + role +
                Environment.NewLine + Environment.NewLine + "TOPIC AND FOCUS" + Environment.NewLine + TopicLens(topic) + " " + FocusLens(focus) +
                Environment.NewLine + Environment.NewLine + "WATCH FOR" + Environment.NewLine + card.Challenge +
                Environment.NewLine + Environment.NewLine + "PRACTICE" + Environment.NewLine + TopicAction(card.Action, topic) +
                Environment.NewLine + Environment.NewLine + "REFLECTION" + Environment.NewLine + card.Reflection;
        }
        private static IEnumerable<string> Connections(CardMeaning[] cards, string[] positions)
        {
            if (positions.SequenceEqual(new[] { "Situation", "Advice", "Outcome" }))
                yield return "Start with " + cards[0].Name + " to identify the present pattern. Use " + cards[1].Name +
                    " as the response to practice, then consider " + cards[2].Name +
                    " as a possible direction to review after taking that step. The outcome can change as your response changes.";
            var commonSuit = cards.Where(card => card.Suit != "Major Arcana").GroupBy(card => card.Suit)
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).FirstOrDefault();
            if (commonSuit != null && commonSuit.Count() >= 2)
                yield return string.Join(" and ", commonSuit.Select(card => card.Name)) + " share " + commonSuit.Key + ": " + SuitTheme(commonSuit.Key) +
                    " Compare how that shared theme appears in their different spread positions.";
            var major = cards.Where(card => card.Suit == "Major Arcana").ToArray();
            if (major.Length >= 2) yield return string.Join(" and ", major.Select(card => card.Name)) + " are Major Arcana: give extra attention to values and recurring life patterns, without assuming an inevitable event.";
            var change = cards.Where(card => new[] { "maj10", "maj13", "maj16", "cups08", "swords06", "wands08" }.Contains(card.Id)).ToArray();
            if (change.Length >= 2) yield return string.Join(" and ", change.Select(card => card.Name)) + " both invite a change of approach. Distinguish what can be released from what needs deliberate action.";
            var pause = cards.FirstOrDefault(card => new[] { "maj09", "maj12", "swords04", "pents07" }.Contains(card.Id));
            var action = cards.FirstOrDefault(card => new[] { "maj07", "wands01", "wands08", "swords12" }.Contains(card.Id));
            if (pause != null && action != null) yield return pause.Name + " invites reflection while " + action.Name + " emphasizes movement. Decide what you need to clarify before taking one bounded step.";
            if (!positions.SequenceEqual(new[] { "Situation", "Advice", "Outcome" }) &&
                (commonSuit == null || commonSuit.Count() < 2) && major.Length < 2 && change.Length < 2 && (pause == null || action == null))
                yield return cards[0].Name + " opens the reading, " + cards[1].Name + " offers a second perspective, and " + cards[2].Name + " completes it. Compare the position meanings before deciding which pattern fits your experience.";
        }
        private static string SuitTheme(string suit)
        {
            switch (suit) {
                case "Cups": return "feelings, connection, and what matters emotionally.";
                case "Pentacles": return "practical resources, daily habits, and sustainable effort.";
                case "Swords": return "thoughts, communication, and the assumptions behind a decision.";
                default: return "motivation, initiative, and how you direct your energy.";
            }
        }
        private static string TopicLens(string topic)
        {
            switch (topic) {
                case "Relationships": return "Apply the theme to communication, consent, and reciprocity in your own actions; this does not reveal another person's private feelings.";
                case "Career": return "Apply the theme to your work habits, responsibilities, and options; check practical facts before making a commitment.";
                case "Personal growth": return "Apply the theme to habits, self-understanding, and values you want to practice.";
                default: return "Apply the theme to a current everyday situation where you have some agency.";
            }
        }
        private static string TopicAction(string action, string topic)
        {
            switch(topic) {
                case "Relationships": return "In an interaction you can influence: " + action;
                case "Career": return "For one work task or decision: " + action;
                case "Personal growth": return "As a small personal practice: " + action;
                default: return action;
            }
        }
        private static string FocusLens(string focus) { return "Focus: " + focus.ToLowerInvariant() + ". " + FocusAction(focus, "General"); }
        private static string FocusAction(string focus, string topic)
        {
            switch(focus) {
                case "Making a decision": return "List two realistic options and one verifiable fact you need before choosing.";
                case "Understanding an obstacle": case "Working through an obstacle": return "Separate the obstacle you can influence from what is outside your control, then choose one manageable response.";
                case "Choosing a next step": return "Choose a reversible action you can complete this week and a simple way to review its effect.";
                case "Improving communication": return "Prepare one clear observation, one feeling, and one respectful request; leave room for the other person's answer.";
                case "Rebuilding trust": return "Identify a small agreement that both people freely accept and can consistently keep.";
                case "Healthy boundaries": return "Name one limit and communicate what you will do to maintain it, rather than trying to control another person.";
                case "Building confidence": return "Record one recent example of effort or learning and choose a small challenge that builds on it.";
                case "Changing a habit": return "Identify one cue and replace one small response; review what happened without judging your character.";
                case "Finding balance": return "Compare your commitments with your actual capacity and decide what can wait.";
                default: return topic == "Career" ? "Choose one work priority and a concrete sign of progress." : topic == "Relationships" ? "Notice one place where giving, receiving, and expectations could be discussed more clearly." : "Choose the theme that resonates most and record one concrete example from your own experience.";
            }
        }
    }
}
