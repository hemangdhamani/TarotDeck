using System;
using System.Collections.Generic;

namespace TarotCardsDeck
{
    public enum DeckChoice
    {
        CompleteTarot,
        MajorArcana,
        MinorArcana
    }

    public class Deck
    {
        private static readonly Random Random = new Random();
        public readonly List<Card> cards = new List<Card>();
        public int Size { get; private set; }

        public Card this[int position] { get { return cards[position]; } }

        public Deck(bool isEmpty) : this(isEmpty, DeckChoice.CompleteTarot) { }

        public Deck(bool isEmpty, DeckChoice choice)
        {
            if (isEmpty) return;
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                if (choice == DeckChoice.MajorArcana && suit != Suit.Maj) continue;
                if (choice == DeckChoice.MinorArcana && suit == Suit.Maj) continue;
                foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
                {
                    if (suit != Suit.Maj && (int)value > 14) continue;
                    string image = suit.ToString().ToLowerInvariant() + ((int)value).ToString("00") + ".jpg";
                    cards.Add(new Card(suit, value, cards.Count + 1, image, true));
                }
            }
            Size = cards.Count;
        }

        public Card Draw()
        {
            if (cards.Count == 0) return null;
            Card card = cards[0];
            cards.RemoveAt(0);
            UpdateIndexes();
            return card;
        }

        public Card DrawRandom()
        {
            if (cards.Count == 0) return null;
            int index = Random.Next(cards.Count);
            Card card = cards[index];
            cards.RemoveAt(index);
            UpdateIndexes();
            return card;
        }

        public void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Next(i + 1);
                Card card = cards[i];
                cards[i] = cards[j];
                cards[j] = card;
            }
            UpdateIndexes();
        }

        private void UpdateIndexes()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].cardIndex = i + 1;
        }
    }
}
