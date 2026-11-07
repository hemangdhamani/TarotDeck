using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.IO;
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
        private ComboBox artworkSelector;
        private Button addArtworkButton;
        private Button creditsButton;
        private bool loadingSelections;
        private Bitmap deckBackImage;
        private Dictionary<string, string> activeArtworkFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private DeckChoice selectedDeck = DeckChoice.CompleteTarot;

        public frmTarot()
        {
            InitializeComponent();
            FormClosed += frmTarot_FormClosed;
        }

        private void frmTarot_FormClosed(object sender, FormClosedEventArgs e)
        {
            RemoveDeck();
            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);
            SetPictureImage(pcbCardFirst, null);
            SetPictureImage(pcbCardLast, null);
        }

        private void frmTarot_Load(object sender, EventArgs e)
        {
            loadingSelections = true;
            deckSelector = new ComboBox();
            deckSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            deckSelector.Items.AddRange(new object[] { "Complete Tarot (78 cards)", "Major Arcana (22 cards)", "Minor Arcana (56 cards)" });
            deckSelector.SelectedIndex = Math.Max(0, Math.Min(2, TarotDeck.Properties.Settings.Default.CardSet));
            deckSelector.Location = new Point(20, 24);
            deckSelector.Width = 210;
            deckSelector.SelectedIndexChanged += DeckSelector_SelectedIndexChanged;
            Controls.Add(deckSelector);
            deckSelector.BringToFront();

            Label cardSetLabel = new Label();
            cardSetLabel.AutoSize = true;
            cardSetLabel.ForeColor = Color.White;
            cardSetLabel.Text = "Card selection";
            cardSetLabel.Location = new Point(20, 7);
            Controls.Add(cardSetLabel);

            artworkSelector = new ComboBox();
            artworkSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            artworkSelector.Location = new Point(20, 81);
            artworkSelector.Width = 210;
            artworkSelector.SelectedIndexChanged += ArtworkSelector_SelectedIndexChanged;
            Controls.Add(artworkSelector);

            Label artworkLabel = new Label();
            artworkLabel.AutoSize = true;
            artworkLabel.ForeColor = Color.White;
            artworkLabel.Text = "Artwork deck";
            artworkLabel.Location = new Point(20, 64);
            Controls.Add(artworkLabel);

            addArtworkButton = new Button();
            addArtworkButton.Text = "Add deck...";
            addArtworkButton.Location = new Point(236, 80);
            addArtworkButton.Size = new Size(90, 23);
            addArtworkButton.UseVisualStyleBackColor = true;
            addArtworkButton.Click += AddArtworkButton_Click;
            Controls.Add(addArtworkButton);

            creditsButton = new Button();
            creditsButton.Text = "Credits";
            creditsButton.Location = new Point(332, 80);
            creditsButton.Size = new Size(70, 23);
            creditsButton.UseVisualStyleBackColor = true;
            creditsButton.Click += CreditsButton_Click;
            Controls.Add(creditsButton);

            PopulateArtworkChoices(TarotDeck.Properties.Settings.Default.ArtworkName);
            RefreshArtworkFileIndex();
            loadingSelections = false;
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            StartNewDeck();
            SetScreenRatio();
            Shuffle();
            SpreadDeck();
        }

        private void DeckSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSelections) return;
            selectedDeck = (DeckChoice)deckSelector.SelectedIndex;
            SaveSelection();
            StartNewDeck();
            Shuffle();
            SpreadDeck();
        }

        private void ArtworkSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loadingSelections || artworkSelector.SelectedIndex < 0) return;
            RefreshArtworkFileIndex();
            SaveSelection();
            StartNewDeck();
            Shuffle();
            SpreadDeck();
        }

        private void SaveSelection()
        {
            TarotDeck.Properties.Settings.Default.CardSet = deckSelector.SelectedIndex;
            TarotDeck.Properties.Settings.Default.ArtworkName = ((ArtworkChoice)artworkSelector.SelectedItem).Name;
            TarotDeck.Properties.Settings.Default.Save();
        }

        private void PopulateArtworkChoices(string preferredName)
        {
            string appImages = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            string userDecks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TarotDeck", "Decks");
            List<ArtworkChoice> choices = new List<ArtworkChoice>();
            choices.Add(new ArtworkChoice("Classic Tarot", appImages));
            string bundledSoimoi = Path.Combine(appImages, "soimoi");
            if (Directory.Exists(bundledSoimoi)) choices.Add(new ArtworkChoice("Soimoi Tarot", bundledSoimoi));
            if (Directory.Exists(userDecks))
            {
                foreach (string directory in Directory.GetDirectories(userDecks))
                    if (HasCompleteArtwork(directory)) choices.Add(new ArtworkChoice(Path.GetFileName(directory), directory));
            }

            artworkSelector.Items.Clear();
            foreach (ArtworkChoice choice in choices) artworkSelector.Items.Add(choice);
            int selected = choices.FindIndex(x => string.Equals(x.Name, preferredName, StringComparison.OrdinalIgnoreCase));
            artworkSelector.SelectedIndex = selected < 0 ? 0 : selected;
        }

        private void AddArtworkButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog picker = new FolderBrowserDialog())
            {
                picker.Description = "Choose a folder containing cardback.jpg and all 78 tarot card images.";
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                Dictionary<string, string> sourceArtwork = BuildArtworkFileIndex(picker.SelectedPath);
                if (!HasCompleteArtwork(sourceArtwork))
                {
                    MessageBox.Show(this, "This folder is missing one or more required cards. See README.md for the required filenames.", "Incomplete tarot deck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TarotDeck", "Decks");
                Directory.CreateDirectory(root);
                string deckName = Path.GetFileName(picker.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrWhiteSpace(deckName)) deckName = "Custom Tarot";
                string destination = Path.Combine(root, deckName);
                int suffix = 2;
                while (Directory.Exists(destination) && !string.Equals(Path.GetFullPath(destination), Path.GetFullPath(picker.SelectedPath), StringComparison.OrdinalIgnoreCase))
                    destination = Path.Combine(root, deckName + " " + suffix++);

                if (!string.Equals(Path.GetFullPath(destination), Path.GetFullPath(picker.SelectedPath), StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(destination);
                    foreach (string file in GetRequiredImageNames())
                        File.Copy(sourceArtwork[Path.GetFileNameWithoutExtension(file)], Path.Combine(destination, file), true);
                    File.Copy(sourceArtwork["cardback"], Path.Combine(destination, "cardback.jpg"), true);
                    foreach (string creditFile in new[] { "LICENSE", "LICENSE.txt", "LICENSE.md", "CREDITS.txt", "ATTRIBUTION.txt" })
                    {
                        string creditPath = Path.Combine(picker.SelectedPath, creditFile);
                        if (File.Exists(creditPath)) File.Copy(creditPath, Path.Combine(destination, creditFile), true);
                    }
                }

                loadingSelections = true;
                PopulateArtworkChoices(Path.GetFileName(destination));
                RefreshArtworkFileIndex();
                loadingSelections = false;
                SaveSelection();
                StartNewDeck();
                Shuffle();
                SpreadDeck();
            }
        }

        private void CreditsButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "Soimoi Tarot artwork by Mike Koz (https://koz.tv/), licensed under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/). The artwork was adapted to this app's card filename format. The custom card back was created for TarotDeck.",
                "Artwork credits", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static bool HasCompleteArtwork(string directory)
        {
            if (!Directory.Exists(directory)) return false;
            return HasCompleteArtwork(BuildArtworkFileIndex(directory));
        }

        private static bool HasCompleteArtwork(Dictionary<string, string> files)
        {
            foreach (string file in GetRequiredImageNames())
                if (!files.ContainsKey(Path.GetFileNameWithoutExtension(file))) return false;
            return files.ContainsKey("cardback");
        }

        private static IEnumerable<string> GetRequiredImageNames()
        {
            Deck completeDeck = new Deck(false);
            return completeDeck.cards.Select(card => card.CardImg);
        }

        private static Dictionary<string, string> BuildArtworkFileIndex(string directory)
        {
            Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] preferredExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                string extension = Path.GetExtension(path);
                if (!preferredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                if (!files.ContainsKey(name)) files.Add(name, path);
            }
            return files;
        }

        private void RefreshArtworkFileIndex()
        {
            ArtworkChoice choice = artworkSelector.SelectedItem as ArtworkChoice;
            string directory = choice == null ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images") : choice.Path;
            activeArtworkFiles = BuildArtworkFileIndex(directory);
        }

        private string FindArtworkFile(string expectedName)
        {
            string path;
            activeArtworkFiles.TryGetValue(Path.GetFileNameWithoutExtension(expectedName), out path);
            return path;
        }

        private sealed class ArtworkChoice
        {
            public string Name { get; private set; }
            public string Path { get; private set; }
            public ArtworkChoice(string name, string path) { Name = name; Path = path; }
            public override string ToString() { return Name; }
        }

        private void StartNewDeck()
        {
            deck = new Deck(false, selectedDeck);
            drawn = new Deck(true);
        }

        private void btnDrawCards_Click(object sender, EventArgs e)
        {
            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);
            //string strImg = null;
            Card curCard = null;

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard01, LoadCardImage(curCard.CardImg));
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard02, LoadCardImage(curCard.CardImg));
                RemoveCardFromDeck(curCard);
            }

            curCard = deck.DrawRandom();
            if (curCard != null)
            {
                SetPictureImage(pcbCard03, LoadCardImage(curCard.CardImg));
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
            SetPictureImage(pcbCardFirst, null);
            SetPictureImage(pcbCardLast, null);

            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);

            btnDrawCards.Enabled = true;

            deck.Shuffle();
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
                SetPictureImage(pcbCardFirst, LoadCardImage(curCard.CardImg));
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
            }
            else if (cardPosition == deck.cards.Count - 1)
            {
                SetPictureImage(pcbCardLast, LoadCardImage(curCard.CardImg));
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
            }
            else
            {
                if (pcbCard01.Image != null && pcbCard02.Image != null && pcbCard03.Image != null)
                {
                    SetPictureImage(pcbCard01, null);
                    SetPictureImage(pcbCard02, null);
                    SetPictureImage(pcbCard03, null);
                }

                if (pcbCard01.Image == null)
                {
                    SetPictureImage(pcbCard01, LoadCardImage(curCard.CardImg));
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard02.Image == null)
                {
                    SetPictureImage(pcbCard02, LoadCardImage(curCard.CardImg));
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                else if (pcbCard03.Image == null)
                {
                    SetPictureImage(pcbCard03, LoadCardImage(curCard.CardImg));
                    //drawn.cards.Add(curCard);
                    //this.Controls.Remove(pcbClick);
                }
                deck.cards.Remove(curCard);
                RemoveDeckPictureBox(pcbClick);
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
            for (int i = 0; i < pcbDeck.Length; i++)
            {
                if (pcbDeck[i] == null) continue;
                Card dCard = pcbDeck[i].Tag as Card;
                if (dCard == null) continue;
                if (pCard.Suit == dCard.Suit && pCard.CardVal == dCard.CardVal)
                {
                    RemoveDeckPictureBox(pcbDeck[i]);
                    break;
                }
            }
        }

        private void RemoveDeckPictureBox(PictureBox pictureBox)
        {
            if (pictureBox == null) return;
            pictureBox.Image = null;
            pictureBox.Click -= pcbDeck_Click;
            Controls.Remove(pictureBox);
            pictureBox.Dispose();
            for (int i = 0; i < pcbDeck.Length; i++)
                if (ReferenceEquals(pcbDeck[i], pictureBox)) pcbDeck[i] = null;
        }

        private void RemoveDeck()
        {
            SuspendLayout();
            try
            {
                for (int i = 0; i < pcbDeck.Length; i++)
                {
                    PictureBox pictureBox = pcbDeck[i];
                    if (pictureBox == null) continue;
                    pictureBox.Image = null;
                    pictureBox.Click -= pcbDeck_Click;
                    Controls.Remove(pictureBox);
                    pictureBox.Dispose();
                    pcbDeck[i] = null;
                }
            }
            finally
            {
                ResumeLayout(false);
            }
            if (deckBackImage != null)
            {
                deckBackImage.Dispose();
                deckBackImage = null;
            }
        }

        private void ShuffleDeck()
        {
            RemoveDeck();
            deckBackImage = LoadCardImage("cardback.jpg");
            pcbDeck = new PictureBox[deck.cards.Count];
            SuspendLayout();
            try
            {
                for (int i = 0; i < pcbDeck.Length; i++)
                {
                    PictureBox pictureBox = new PictureBox();
                    pcbDeck[i] = pictureBox;
                    pictureBox.Size = new Size((int)(300 * dblScrWidRat), (int)(175 * dblScrHgtRat));
                    pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBox.Image = deckBackImage;
                    pictureBox.Tag = deck.cards[i];
                    pictureBox.Click += pcbDeck_Click;
                    pictureBox.Visible = false;

                    pictureBox.Location = new Point((int)(550 * dblScrWidRat), 100);
                    Controls.Add(pictureBox);
                    pictureBox.BringToFront();
                }
            }
            finally
            {
                ResumeLayout(false);
            }
        }

        private void SpreadDeck()
        {
            ShuffleDeck();
            int count = pcbDeck.Length;
            if (count == 0) return;

            int cardWidth = Math.Max(1, (int)(175 * dblScrWidRat));
            int cardHeight = Math.Max(1, (int)(300 * dblScrHgtRat));
            double defaultSpacing = 15 * dblScrWidRat;
            double availableWidth = Math.Max(0, ClientSize.Width - cardWidth);
            double spacing = count > 1 ? Math.Min(defaultSpacing, availableWidth / (count - 1)) : 0;
            double spreadWidth = cardWidth + spacing * (count - 1);
            double startX = Math.Max(0, (ClientSize.Width - spreadWidth) / 2);
            double center = (count - 1) / 2.0;

            SuspendLayout();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    PictureBox pictureBox = pcbDeck[i];
                    pictureBox.Size = new Size(cardWidth, cardHeight);
                    int x = (int)Math.Round(startX + i * spacing);
                    int y = (int)Math.Round(130 * dblScrHgtRat + Math.Abs(i - center) * 2 * dblScrHgtRat);
                    pictureBox.Location = new Point(x, y);
                    pictureBox.Visible = true;
                }
            }
            finally
            {
                ResumeLayout(false);
            }
            // Keep the last card in the shuffled deck visually on top at the right edge.
            for (int i = 0; i < pcbDeck.Length; i++)
                Controls.SetChildIndex(pcbDeck[i], 0);
            Invalidate();
            Update();
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
            btnShufCards.Height = (int)(btnShufCards.Height * dblScrHgtRat);

            btnDrawCards.Top = (int)(btnDrawCards.Top * dblScrHgtRat);
            btnDrawCards.Left = (int)(btnDrawCards.Left * dblScrWidRat);
            btnDrawCards.Width = (int)(btnDrawCards.Width * dblScrWidRat);
            btnDrawCards.Height = (int)(btnDrawCards.Height * dblScrHgtRat);

            btnGoBack.Top = (int)(btnGoBack.Top * dblScrHgtRat);
            btnGoBack.Left = (int)(btnGoBack.Left * dblScrWidRat);
            btnGoBack.Width = (int)(btnGoBack.Width * dblScrWidRat);
            btnGoBack.Height = (int)(btnGoBack.Height * dblScrHgtRat);

            btnGoFowd.Top = (int)(btnGoFowd.Top * dblScrHgtRat);
            btnGoFowd.Left = (int)(btnGoFowd.Left * dblScrWidRat);
            btnGoFowd.Width = (int)(btnGoFowd.Width * dblScrWidRat);
            btnGoFowd.Height = (int)(btnGoFowd.Height * dblScrHgtRat);

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

            SetPictureImage(pcbCard01, null);
            SetPictureImage(pcbCard02, null);
            SetPictureImage(pcbCard03, null);

            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard03, LoadCardImage(drawn.cards[intCurCard - 1].CardImg));
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard02, LoadCardImage(drawn.cards[intCurCard - 1].CardImg));
            }

            intCurCard--;
            if (intCurCard >= 1 && intCurCard <= drawn.cards.Count)
            {
                SetPictureImage(pcbCard01, LoadCardImage(drawn.cards[intCurCard - 1].CardImg));
            }
        }

        private Bitmap LoadCardImage(string fileName)
        {
            string path = FindArtworkFile(fileName);
            if (path == null) throw new FileNotFoundException("Missing artwork image " + fileName);
            using (Image image = Image.FromFile(path)) return new Bitmap(image);
        }

        private void SetPictureImage(PictureBox pictureBox, Image image)
        {
            Image previous = pictureBox.Image;
            if (ReferenceEquals(previous, image)) return;
            pictureBox.Image = image;
            if (previous != null && !ReferenceEquals(previous, deckBackImage)) previous.Dispose();
        }
    }
}
