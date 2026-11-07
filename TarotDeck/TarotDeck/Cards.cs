
namespace TarotCardsDeck
{
    public enum Suit
    {
        Cups, Pents, Swords, Wands, Maj
    }

    public enum CardValue
    {
        One = 01, Two = 02, Three = 03, Four = 04, Five = 05, Six = 06, Seven = 07, Eight = 08, Nine = 09, Ten = 10, 
        Eleven = 11, Twelve = 12, Thirteen = 13, Fourteen = 14, 
        Fifteen = 15, Sixteen = 16, Seventeen = 17, Eighteen = 18, Nineteen = 19, Twenty = 20, TwentyOne = 21, TwentyTwo = 22
    }

    public class Card
    {
        private readonly Suit suit;
        private readonly CardValue cardVal;
        private readonly string cardImg;
        private bool isShowCard;

        public Suit Suit { get { return suit; } }
        public CardValue CardVal { get { return cardVal; } }
        public string CardImg { get { return cardImg; } }
        public bool IsCardUp { get { return isShowCard; } set { isShowCard = value; } }

        public Card(Suit suit, CardValue cardVal, string cardImg, bool isShowCard)
        {
            this.suit = suit;
            this.cardVal = cardVal;
            this.cardImg = cardImg;
            this.isShowCard = isShowCard;
        }

        /// <summary>
        /// Return the card as a string (i.e. "The Ace of Spades")
        /// </summary>
        public override string ToString()
        {
            return "The " + cardVal + " of " + suit;
        }
    }
}
