using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Runtime.InteropServices;

namespace HW2
{
    public partial class Form1 : Form
    {
        private List<Image> faceImages = new List<Image>();
        private List<Image> all52Cards = new List<Image>();
        private Image backImage;

        private PictureBox firstClicked = null;
        private PictureBox secondClicked = null;
        private bool isFlipping = false;
        private bool isPreviewing = false;

        private int timeLeft = 120;
        private int previewTimeLeft = 5; //預覽倒數 5 秒
        private int matchedPairs = 0;    //紀錄成功配對組數

        private SoundPlayer clickPlayer;
        private SoundPlayer winPlayer;
        private SoundPlayer failPlayer;

        // 呼叫 Windows 系統底層的播音 API
        private WMPLib.WindowsMediaPlayer bgmPlayer = new WMPLib.WindowsMediaPlayer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupGame();

            //遊戲剛打開時，隱藏預覽倒數的標籤
            label2.Visible = false;
        }

        private void SetupGame()
        {
            backImage = Properties.Resources.back;

            all52Cards.Clear();
            for (int i = 1; i <= 52; i++)
            {
                object obj = Properties.Resources.ResourceManager.GetObject("pic" + i);
                if (obj != null)
                {
                    all52Cards.Add((Image)obj);
                }
            }
            //音效
            clickPlayer = new SoundPlayer(Properties.Resources.poker_flip);
            winPlayer = new SoundPlayer(Properties.Resources.victory);
            failPlayer = new SoundPlayer(Properties.Resources.fail);

           //處理背景音樂
            string bgmPath = Path.Combine(Application.StartupPath, "temp_bgm.wav");
            using (Stream stream = Properties.Resources.background)
            using (FileStream fileStream = new FileStream(bgmPath, FileMode.Create))
            {
                stream.CopyTo(fileStream);
            }
            bgmPlayer.URL = bgmPath;
            bgmPlayer.settings.setMode("loop", true); //設定為無限循環播放
            bgmPlayer.controls.stop(); //按下Start播放

            //預覽計時器
            previewTimer.Interval = 1000;
            previewTimer.Tick -= previewTimer_Tick;
            previewTimer.Tick += previewTimer_Tick;

            flipTimer.Interval = 800;
            flipTimer.Tick -= flipTimer_Tick;
            flipTimer.Tick += flipTimer_Tick;

            gameTimer.Interval = 1000;
            gameTimer.Tick -= GameTimer_Tick;
            gameTimer.Tick += GameTimer_Tick;

            foreach (Control ctrl in tableLayoutPanel1.Controls)
            {
                if (ctrl is PictureBox pb)
                {
                    pb.Click -= pictureBox1_Click;
                    pb.Click += pictureBox1_Click;
                    pb.Image = backImage;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isPreviewing || all52Cards.Count < 52) return;

            //重置所有遊戲數值
            gameTimer.Stop();
            timeLeft = 120;
            matchedPairs = 0; //配對數量歸零
            previewTimeLeft = 5; //預覽時間重置為 5 秒

            label1.Text = "剩餘時間： 120秒";
            label2.Text = $"預覽倒數： {previewTimeLeft}秒";
            label2.Visible = true; //顯示預覽標籤
            bgmPlayer.controls.play();
            Random rnd = new Random();

            List<int> ranks = new List<int>();
            for (int i = 0; i < 13; i++)
            {
                ranks.Add(i);
            }

            ranks = ranks.OrderBy(x => rnd.Next()).ToList();
            List<int> selectedRanks = ranks.Take(8).ToList();

            faceImages.Clear();
            foreach (int rank in selectedRanks)
            {
                int suit = rnd.Next(4);
                int cardIndex = (rank * 4) + suit;
                faceImages.Add(all52Cards[cardIndex]);
            }

            List<int> cardIDs = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                cardIDs.Add(i);
                cardIDs.Add(i);
            }

            cardIDs = cardIDs.OrderBy(x => rnd.Next()).ToList();

            int index = 0;
            foreach (Control ctrl in tableLayoutPanel1.Controls)
            {
                if (ctrl is PictureBox pb)
                {
                    int cardId = cardIDs[index];
                    pb.Tag = cardId;
                    pb.Image = faceImages[cardId];
                    index++;
                }
            }
            isPreviewing = true;
            previewTimer.Start();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (isPreviewing || isFlipping || !(sender is PictureBox clickedBox))
                return;

            if (clickedBox.Image != backImage)
                return;
            clickPlayer.Play();
            int cardId = (int)clickedBox.Tag;
            clickedBox.Image = faceImages[cardId];

            if (firstClicked == null)
            {
                firstClicked = clickedBox;
                return;
            }

            secondClicked = clickedBox;

            if (firstClicked.Tag.ToString() == secondClicked.Tag.ToString())
            {
                firstClicked = null;
                secondClicked = null;

                // 配對成功組數+1
                matchedPairs++;

                //檢查是否8組全滿
                if (matchedPairs >= 8)
                {
                    gameTimer.Stop(); //贏了 停止倒數
                    bgmPlayer.controls.stop();
                    winPlayer.Play();
                    MessageBox.Show($"太厲害了！你用了 {120 - timeLeft} 秒完成遊戲！\n點擊start重新開始", "恭喜過關");
                }
            }
            else
            {
                isFlipping = true;
                flipTimer.Start();
            }
        }

        private void previewTimer_Tick(object sender, EventArgs e)
        {
            //每秒觸發一次，把時間減 1
            previewTimeLeft--;
            label2.Text = $"預覽倒數： {previewTimeLeft}秒";

            //蓋牌
            if (previewTimeLeft <= 0)
            {
                previewTimer.Stop();
                foreach (Control ctrl in tableLayoutPanel1.Controls)
                {
                    if (ctrl is PictureBox pb)
                    {
                        pb.Image = backImage;
                    }
                }

                label2.Visible = false; //預覽結束，隱藏倒數標籤
                isPreviewing = false;
                gameTimer.Start();      //120秒倒數
            }
        }

        private void flipTimer_Tick(object sender, EventArgs e)
        {
            if (firstClicked == null || secondClicked == null) return;

            flipTimer.Stop();

            firstClicked.Image = backImage;
            secondClicked.Image = backImage;

            firstClicked = null;
            secondClicked = null;
            isFlipping = false;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            label1.Text = $"剩餘時間： {timeLeft}秒";

            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                isFlipping = true; // 鎖定畫面，不讓玩家再點
                bgmPlayer.controls.stop();
                failPlayer.Play();
                MessageBox.Show("時間到！挑戰失敗啦！\n點擊start重新開始", "Time Over");
            }
        }
    }
}