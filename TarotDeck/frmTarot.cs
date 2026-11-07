using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TarotCardsDeck
{
    public partial class frmTarot : Form
    {
        public Deck deck, drawn;
        PictureBox[] pcbDeck = new PictureBox[78];
        double dblScrHgtRat = 1;
        double dblScrWidRat = 1;
        int intCurPage, intMaxPage = 0;
        private ComboBox deckSelector;
        private DeckChoice selectedDeck = DeckChoice.CompleteTarot;
        private int DeckSize { get { return deck == null ? 78 : deck.Size; } }

        public frmTarot()
        {
            InitializeComponent();
        }

        private void frmTarot_Load(object sender, EventArgs e)
        {
            deckSelector = new ComboBox();
            deckSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            deckSelector.Items.AddRange(new object[] { "Complete Tarot (78 cards)", "Major Arcana (22 cards)", "Minor Arcana (56 cards)" });
            deckSelector.SelectedIndex = 0;
            deckSelector.Location = new Point(20, 18);
            deckSelector.Width = 210;
            deckSelector.SelectedIndexChanged += DeckSelector_SelectedIndexChanged;
            Controls.Add(deckSelector);
            deckSelector.BringToFront();
            StartNewDeck();
            SetScreenRatio();
            SpreadDeck();
        }

        private void DeckSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            StartNewDeck();
            Shuffle();
            SpreadDeck();
        }

        private void StartNewDeck()
        {
            deck = new Deck(false, selectedDeck);
            drawn = new Deck(true);
            deck.Shuffle();
        }

        private void btnDrawCards_Click(object sender, EventArgs e)
        {
            pcbCard01.Image = null;
            pcbCard02.Image = null;
            pcbCard03.Image = null;
            //string strImg = null;
            Card curCard = null;

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                pcbCard01.Image = LoadCardImage(curCard.CardImg);
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                pcbCard02.Image = LoadCardImage(curCard.CardImg);
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                pcbCard03.Image = LoadCardImage(curCard.CardImg);
                RemoveCardFromDeck(curCard);
            }
            //MessageBox.Show(drawn.cards.Count.ToString());
        }

        private void btnShufCards_Click(object sender, EventArgs e)
        {
            StartNewDeck();

            this.Shuffle();
            this.SpreadDeck();
        }

        /// <summary>
        /// Shuffles the cards in the deck
        /// </summary>
        public void Shuffle()
        {
            SetCurrentPage(0);
            pcbCardFirst.Image = null;
            pcbCardLast.Image = null;

            pcbCard01.Image = null;
            pcbCard02.Image = null;
            pcbCard03.Image = null;

            btnDrawCards.Enabled = true;

            Random random = new Random();
            for (int i = 0; i < deck.cards.Count; i++)
            {
                int index1 = i;
                int index2 = random.Next(deck.cards.Count);
                SwapCard(index1, index2);
            }
        }

        /// <summary>
        /// Swaps the placement of two cards
        /// </summary>
        private void SwapCard(int index1, int index2)
        {
            Card card = deck.cards[index1];

            //string strImg1 = "images\\" + deck.cards[index1].CardImg;
            //string strImg2 = "images\\" + deck.cards[index2].CardImg;
            //string strImg3 = "images\\" + deck.cards[0].CardImg;
            //Bitmap btmImg1 = new Bitmap(strImg1);
            //Bitmap btmImg2 = new Bitmap(strImg2);
            //Bitmap btmImg3 = null;

            //pcbCard01.Image = btmImg1;
            //pcbCard03.Image = btmImg2;
            //this.Refresh();

            ////for (int i = 1; i < 500000000; i++)
            ////{
            ////}

            //pcbCard03.Image = btmImg3;
            //pcbCard02.Image = btmImg2;
            //pcbCard01.Image = btmImg3;
            //pcbCard03.Image = btmImg1;
            //this.Refresh();

            ////for (int i = 1; i < 500000000; i++)
            ////{
            ////}

            //pcbCard02.Image = btmImg3;
            //pcbCard01.Image = btmImg2;
            //this.Refresh();
            ////for (int i = 1; i < 500000000; i++)
            ////{
            ////}

            deck.cards[index1] = deck.cards[index2];
            deck.cards[index1].cardIndex = index1 + 1;
            deck.cards[index2] = card;
            deck.cards[index2].cardIndex = index2 + 1;

        }

        private void pcbDeck_Click(object sender, EventArgs e)
        {
            PictureBox pcbClick = (PictureBox)sender;
            Card curCard = (Card)pcbClick.Tag;
            int cardPosition = deck.cards.IndexOf(curCard);
            //MessageBox.Show(curCard.cardIndex.ToString());

            if (intCurPage != intMaxPage)
                ShowPage(0);

            if (cardPosition == 0)
            {
                pcbCardFirst.Image = LoadCardImage(curCard.CardImg);
                deck.cards.Remove(curCard);
                this.Controls.Remove(pcbClick);
            }
            else if (cardPosition == deck.cards.Count - 1)
            {
                pcbCardLast.Image = LoadCardImage(curCard.CardImg);
                deck.cards.Remove(curCard);
                this.Controls.Remove(pcbClick);
            }
            else
            {
                if (pcbCard01.Image != null && pcbCard02.Image != null && pcbCard03.Image != null)
                {
                    pcbCard01.Image = null;
                    pcbCard02.Image = null;
                    pcbCard03.Image = null;
                }

                if (pcbCard01.Image == null)
                {
                    pcbCard01.Image = LoadCardImage(curCard.CardImg);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard02.Image == null)
                {
                    pcbCard02.Image = LoadCardImage(curCard.CardImg);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard03.Image == null)
                {
                    pcbCard03.Image = LoadCardImage(curCard.CardImg);
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                deck.cards.Remove(curCard);
                this.Controls.Remove(pcbClick);
                drawn.cards.Add(curCard);
                SetCurrentPage(0);

                if (pcbCard01.Image != null && pcbCard02.Image != null && pcbCard03.Image != null)
                {
                    btnDrawCards.Enabled = true;
                }
                else
                {
                    btnDrawCards.Enabled = false;
                }
                //MessageBox.Show(drawn.cards.Count.ToString());
            }

            //MessageBox.Show(pcbClick.Name, "Deck");
        }

        private void btnGoBack_Click(object sender, EventArgs e)
        {
            ShowPage(-1);
        }

        private void btnGoFowd_Click(object sender, EventArgs e)
        {
            ShowPage(+1);
        }

        //public class DeckPictureBox : PictureBox
        //{
        //    public Card myCard;

        //    public Card MyCard
        //    {
        //        get { return myCard; }

        //        set { myCard = value; }
        //    }
        //}

        private void RemoveCardFromDeck(Card pCard)
        {
            drawn.cards.Add(pCard);
            SetCurrentPage(0);
            Card dCard = null;
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                dCard = (Card)pcbDeck[i].Tag;
                if (pCard.Suit == dCard.Suit && pCard.CardVal == dCard.CardVal)
                {
                    this.Controls.Remove(pcbDeck[i]);
                    break;
                }
            }
        }

        private void HideDeck()
        {
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                if (pcbDeck[i] != null)
                {
                    pcbDeck[i].Hide();
                }
            }
            //this.Refresh();
        }

        private void RemoveDeck()
        {
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                if (pcbDeck[i] != null)
                {
                    this.Controls.Remove(pcbDeck[i]);
                }
            }
            //this.Refresh();
        }

        private void ShuffleDeck()
        {
            RemoveDeck();
            Bitmap btmCardBack = LoadCardImage("cardback.jpg");
            //Bitmap btmRotated = new Bitmap(btmCardBack.Width, btmCardBack.Height);
            //btmRotated.SetResolution(btmCardBack.HorizontalResolution, btmCardBack.VerticalResolution);
            //Graphics g = Graphics.FromImage(btmRotated);
            //g.RotateTransform(30);
            //g.DrawImage(btmCardBack, new Point(0, 0));

            //btmCardBack.RotateFlip(RotateFlipType.Rotate270FlipX);
            pcbDeck = new PictureBox[deck.cards.Count];
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                pcbDeck[i] = new PictureBox();
                pcbDeck[i].Size = new Size((int)(300 * dblScrWidRat), (int)(175 * dblScrHgtRat));
                pcbDeck[i].SizeMode = PictureBoxSizeMode.StretchImage;
                pcbDeck[i].Image = btmCardBack;
                pcbDeck[i].Parent = this;
                pcbDeck[i].Tag = (Card)deck.cards[i];
                pcbDeck[i].Click += new EventHandler(pcbDeck_Click);

                if ((i & 1) == 0)
                    pcbDeck[i].Location = new Point((int)(550 * dblScrWidRat) - (0), 100);
                else
                    pcbDeck[i].Location = new Point((int)(750 * dblScrWidRat) + (0), 100);
                pcbDeck[i].BringToFront();
                this.Refresh();
            }
            HideDeck();
        }

        private void SpreadDeck()
        {
            this.ShuffleDeck();
            //Bitmap btmCardBack = new Bitmap("images\\cardback.jpg");
            //pcbDeck = new PictureBox[78];
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                //pcbDeck[i] = new PictureBox();
                //pcbDeck[i].Size = new Size(175, 300);
                //pcbDeck[i].SizeMode = PictureBoxSizeMode.StretchImage;
                //pcbDeck[i].Image = btmCardBack;
                //pcbDeck[i].Parent = this;
                //pcbDeck[i].Tag = (Card)deck.cards[i - 1];
                //pcbDeck[i].Click += new EventHandler(pcbDeck_Click);
                pcbDeck[i].Size = new Size((int)(175 * dblScrWidRat), (int)(300 * dblScrHgtRat));

                if (i < 39)
                    pcbDeck[i].Location = new Point((int)((i * 15 * dblScrWidRat) + 132 * dblScrWidRat), ((39 - i) * 2) + 42);
                else
                    pcbDeck[i].Location = new Point((int)((i * 15 * dblScrWidRat) + 132 * dblScrWidRat), ((i - 39) * 2) + 42);
                pcbDeck[i].Show();
                //pcbDeck[i].BringToFront();
                this.Refresh();
            }
        }

        private void SetScreenRatio()
        {
            Rectangle rct = Screen.GetWorkingArea(this);
            //int intTemp = 0;
            
            //MessageBox.Show(rct.Width.ToString());

            dblScrHgtRat = (double)rct.Height / 860;
            dblScrWidRat = (double)rct.Width / 1600;

            btnShufCards.Top = (int)(btnShufCards.Top * dblScrHgtRat);
            btnShufCards.Left = (int)(btnShufCards.Left * dblScrWidRat);
            btnShufCards.Width = (int)(btnShufCards.Width * dblScrWidRat);

            btnDrawCards.Top = (int)(btnDrawCards.Top * dblScrHgtRat);
            btnDrawCards.Left = (int)(btnDrawCards.Left * dblScrWidRat);
            btnDrawCards.Width = (int)(btnDrawCards.Width * dblScrWidRat);

            btnGoBack.Top = (int)(btnGoBack.Top * dblScrHgtRat);
            btnGoBack.Left = (int)(btnGoBack.Left * dblScrWidRat);
            btnGoBack.Width = (int)(btnGoBack.Width * dblScrWidRat);

            btnGoFowd.Top = (int)(btnGoFowd.Top * dblScrHgtRat);
            btnGoFowd.Left = (int)(btnGoFowd.Left * dblScrWidRat);
            btnGoFowd.Width = (int)(btnGoFowd.Width * dblScrWidRat);

            pcbCard01.Height = (int)(pcbCard01.Height * dblScrHgtRat);
            pcbCard01.Width = (int)(pcbCard01.Width * dblScrWidRat);

            pcbCard02.Height = (int)(pcbCard02.Height * dblScrHgtRat);
            pcbCard02.Width = (int)(pcbCard02.Width * dblScrWidRat);

            pcbCard03.Height = (int)(pcbCard03.Height * dblScrHgtRat);
            pcbCard03.Width = (int)(pcbCard03.Width * dblScrWidRat);

            pcbCardFirst.Height = (int)(pcbCardFirst.Height * dblScrHgtRat);
            pcbCardFirst.Width = (int)(pcbCardFirst.Width * dblScrWidRat);

            pcbCardLast.Height = (int)(pcbCardLast.Height * dblScrHgtRat);
            pcbCardLast.Width = (int)(pcbCardLast.Width * dblScrWidRat);

            flpCard01.Padding = new Padding((int)(flpCard01.Padding.Left * dblScrWidRat), (int)(flpCard01.Padding.Top * dblScrHgtRat), (int)(flpCard01.Padding.Right * dblScrWidRat), (int)(flpCard01.Padding.Bottom * dblScrHgtRat));
            flpCard02.Padding = new Padding((int)(flpCard02.Padding.Left * dblScrWidRat), (int)(flpCard02.Padding.Top * dblScrHgtRat), (int)(flpCard02.Padding.Right * dblScrWidRat), (int)(flpCard02.Padding.Bottom * dblScrHgtRat));
            flpCard03.Padding = new Padding((int)(flpCard03.Padding.Left * dblScrWidRat), (int)(flpCard03.Padding.Top * dblScrHgtRat), (int)(flpCard03.Padding.Right * dblScrWidRat), (int)(flpCard03.Padding.Bottom * dblScrHgtRat));

            flpCardFirst.Padding = new Padding((int)(flpCardFirst.Padding.Left * dblScrWidRat), (int)(flpCardFirst.Padding.Top * dblScrHgtRat), (int)(flpCardFirst.Padding.Right * dblScrWidRat), (int)(flpCardFirst.Padding.Bottom * dblScrHgtRat));
            flpCardLast.Padding = new Padding((int)(flpCardLast.Padding.Left * dblScrWidRat), (int)(flpCardLast.Padding.Top * dblScrHgtRat), (int)(flpCardLast.Padding.Right * dblScrWidRat), (int)(flpCardLast.Padding.Bottom * dblScrHgtRat));

            flpGoBack.Padding = new Padding((int)(flpGoBack.Padding.Left * dblScrWidRat), (int)(flpGoBack.Padding.Top * dblScrHgtRat), (int)(flpGoBack.Padding.Right * dblScrWidRat), (int)(flpGoBack.Padding.Bottom * dblScrHgtRat));
            flpGoFowd.Padding = new Padding((int)(flpGoFowd.Padding.Left * dblScrWidRat), (int)(flpGoFowd.Padding.Top * dblScrHgtRat), (int)(flpGoFowd.Padding.Right * dblScrWidRat), (int)(flpGoFowd.Padding.Bottom * dblScrHgtRat));

            //flpCard01.AutoSize = true;
            //flpCard02.AutoSize = true;
            //flpCard03.AutoSize = true;
            //flpCardFirst.AutoSize = true;
            //flpCardLast.AutoSize = true;
            //flpGoBack.AutoSize = true;
            //flpGoFowd.AutoSize = true;

            this.Refresh();

            flpMain.Width = (int)(flpMain.Width * dblScrWidRat);
            //flpMain.Left = (int)(flpMain.Left * dblScrWidRat);
            flpMain.Top = (int)(flpMain.Top * dblScrHgtRat);
            flpMain.Height = (int)(flpMain.Height * dblScrHgtRat);

            flpMain.Left = (int)((rct.Width-flpMain.Width)/2);

            //MessageBox.Show(this.Width.ToString());
            //MessageBox.Show(flpMain.Width.ToString());
            //MessageBox.Show(flpMain.Left.ToString());

            //MessageBox.Show(dblScrHgtRat.ToString());
            //MessageBox.Show(dblScrWidRat.ToString());
        }

        //private void SetCurrentPage()
        //{
        //    intCurPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
        //    intMaxPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
        //    btnGoBack.Enabled = (intCurPage != 1);
        //    btnGoFowd.Enabled = false;
        //}

        private void SetCurrentPage(int pintMovePage)
        {
            if (pintMovePage == 0)
            {
                intCurPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
                intMaxPage = ((drawn.cards.Count % 3) == 0) ? Convert.ToInt32(drawn.cards.Count / 3) : Convert.ToInt32(drawn.cards.Count / 3) + 1;
                //btnGoBack.Enabled = (intCurPage != 1);
                //btnGoFowd.Enabled = false;
            }
            else
            {
                intCurPage = intCurPage + pintMovePage;
            }
            btnGoBack.Enabled = (intCurPage == 0) ? false : (intCurPage != 1);
            btnGoFowd.Enabled = (intCurPage != intMaxPage);
            btnDrawCards.Enabled = (deck.cards.Count >= 3);
        }

        private void ShowPage(int pintPage)
        {
            SetCurrentPage(pintPage);
            int intCurCard = intCurPage * 3;

            pcbCard01.Image = null;
            pcbCard02.Image = null;
            pcbCard03.Image = null;

            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                pcbCard03.Image = LoadCardImage(drawn.cards[intCurCard - 1].CardImg);
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                pcbCard02.Image = LoadCardImage(drawn.cards[intCurCard - 1].CardImg);
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                pcbCard01.Image = LoadCardImage(drawn.cards[intCurCard - 1].CardImg);
            }
        }

        private static Bitmap LoadCardImage(string fileName)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", fileName);
            using (Image image = Image.FromFile(path)) return new Bitmap(image);
        }
    }
}
