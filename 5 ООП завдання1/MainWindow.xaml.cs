using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp10
{
    public partial class MainWindow : Window
    {
        private const int HorseCount = 4;
        private const int FrameCount = 12;
        private const string ImagePath = @"C:\\Users\\User\\source\\repos\\5 ООП завдання1\\5 ООП завдання1\\images\\";
        private const int TrackWidth = 1500;
        private bool raceFinished = false;

        private List<Horse> horses = new List<Horse>();
        private DispatcherTimer timer;
        private Random rand = new Random();
        private int balance = 250000;
        private int betAmount = 20;
        private int selectedHorseIndex = 0;
        private DateTime raceStartTime;

        public MainWindow()
        {
            InitializeComponent();

            LoadBackground();
            InitHorses();
            UpdateUI();
        }

        private void LoadBackground()
        {
            string trackPath = System.IO.Path.Combine(ImagePath, @"Background\\Track.png");

            if (System.IO.File.Exists(trackPath))
            {
                var trackImage = new BitmapImage(new Uri(trackPath, UriKind.Absolute));
                RaceCanvas.Background = new ImageBrush(trackImage)
                {
                    TileMode = TileMode.Tile,
                    Viewport = new Rect(0, 0, 200, 220),
                    ViewportUnits = BrushMappingMode.Absolute,
                };
            }
            else
            {
                MessageBox.Show("❌ Не знайдено зображення треку за шляхом:\n" + trackPath);
            }
        }

        private void InitHorses()
        {
            RaceCanvas.Children.Clear();
            LoadBackground();
            horses.Clear();

            string[] horseNames = { "Lucky", "Ranger", "Willow", "Tucker" };
            SolidColorBrush[] colors = { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.DarkCyan };

            for (int i = 0; i < HorseCount; i++)
            {
                Horse horse = new Horse
                {
                    Name = horseNames[i],
                    Speed = rand.Next(5, 9),
                    Position = 0,
                    FrameIndex = 0,
                    Y = 10 + i * 50,
                    ColorName = colors[i].Color.ToString(),
                    Coefficient = 1.25,
                    Money = 0,
                    ColorBrush = colors[i],
                    ImageControl = new Image { Width = 51, Height = 51 }
                };

                // Додаємо текст з ім’ям коня
                TextBlock nameLabel = new TextBlock
                {
                    Text = horse.Name,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                    FontWeight = FontWeights.Bold,
                    Visibility = Visibility.Hidden,
                    Padding = new Thickness(2),
                    FontSize = 12
                };

                horse.NameLabel = nameLabel;
                RaceCanvas.Children.Add(horse.NameLabel);
                Canvas.SetTop(horse.NameLabel, horse.Y - 20);
                Canvas.SetLeft(horse.NameLabel, horse.Position);

                // Події наведення
                horse.ImageControl.MouseEnter += (s, e) =>
                {
                    horse.NameLabel.Visibility = Visibility.Visible;
                };

                horse.ImageControl.MouseLeave += (s, e) =>
                {
                    horse.NameLabel.Visibility = Visibility.Hidden;
                };

                horses.Add(horse);
                RaceCanvas.Children.Add(horse.ImageControl);
                Canvas.SetTop(horse.ImageControl, horse.Y);
                Canvas.SetLeft(horse.ImageControl, horse.Position);
            }

            HorseSelector.ItemsSource = horses.Select((h, i) => $"{i + 1}. {h.Name}");
            HorseSelector.SelectedIndex = 0;
        }


        private void StartAnimation()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            raceStartTime = DateTime.Now;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            bool allFinished = true;

            foreach (var horse in horses)
            {
                if (horse.Finished) continue;

                // Рух
                horse.Position += horse.Speed;
                Canvas.SetLeft(horse.ImageControl, horse.Position);
                Canvas.SetTop(horse.ImageControl, horse.Y);
                Canvas.SetLeft(horse.NameLabel, horse.Position);
                Canvas.SetTop(horse.NameLabel, horse.Y - 20);

                // Анімація
                horse.FrameIndex = (horse.FrameIndex + 1) % FrameCount;
                UpdateHorseImage(horse);

                // Перевірка фінішу
                if (horse.Position >= TrackWidth - 100)
                {
                    horse.Finished = true;
                    horse.FinishTime = DateTime.Now;
                    horse.Time = (horse.FinishTime - raceStartTime).ToString(@"mm\:ss\:fff");
                }
                else
                {
                    allFinished = false;
                }
            }

            RaceScrollViewer.ScrollToHorizontalOffset(horses.Max(h => h.Position) - 200);

            // Коли всі фінішували
            if (!raceFinished && allFinished)
            {
                timer.Stop();
                raceFinished = true;

                var winner = horses.OrderBy(h => h.FinishTime).First();

                if (horses.IndexOf(winner) == selectedHorseIndex)
                {
                    double win = betAmount * winner.Coefficient;
                    balance += (int)win;
                    winner.Money = (int)win;

                    MessageBox.Show($"🏆 Переміг {winner.Name}! Ви виграли {win}$!");
                }
                else
                {
                    MessageBox.Show($"🏍️ Переміг {winner.Name}. Ви програли {betAmount}$.");
                }

                UpdateUI();
            }

            UpdateDetailsGrid();
        }


        private void UpdateHorseImage(Horse horse)
        {
            string frameFile = $"WithOutBorder_00{horse.FrameIndex:D2}.png";
            string maskFile = $"mask_00{horse.FrameIndex:D2}.png";

            var frameBitmap = new BitmapImage(new Uri(System.IO.Path.Combine(ImagePath, "Horses", frameFile)));
            var maskBitmap = new BitmapImage(new Uri(System.IO.Path.Combine(ImagePath, "HorsesMask", maskFile)));

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(frameBitmap, new Rect(0, 0, 64, 64));
                dc.PushOpacityMask(new ImageBrush(maskBitmap));
                dc.DrawRectangle(horse.ColorBrush, null, new Rect(0, 0, 64, 64));
            }

            var rtb = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            horse.ImageControl.Source = rtb;
        }

        private void UpdateUI()
        {
            BalanceText.Text = $"{balance}$";
            BetAmountText.Text = $"{betAmount}$";
        }

        private void UpdateDetailsGrid()
        {
            DetailsGrid.ItemsSource = horses.Select(h => new
            {
                h.Name,
                Колір = h.ColorName, // можна також зробити кольоровий квадратик, якщо треба
                Швидкість = h.Speed,
                h.Coefficient,
                h.Position,
                h.Time,
                Виграш = h.Money
            }).ToList();
        }


        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            InitHorses();
            raceFinished = false;
            UpdateUI();
            StartAnimation();
        }

        private void DecreaseBet_Click(object sender, RoutedEventArgs e)
        {
            if (betAmount > 10)
            {
                betAmount -= 10;
                UpdateUI();
            }
        }

        private void IncreaseBet_Click(object sender, RoutedEventArgs e)
        {
            if (betAmount + 10 <= balance)
            {
                betAmount += 10;
                UpdateUI();
            }
        }

        private void PlaceBet_Click(object sender, RoutedEventArgs e)
        {
            if (HorseSelector.SelectedIndex < 0)
            {
                MessageBox.Show("Виберіть коня для ставки.");
                return;
            }

            if (betAmount > balance)
            {
                MessageBox.Show("Недостатньо коштів.");
                return;
            }

            selectedHorseIndex = HorseSelector.SelectedIndex;
            balance -= betAmount;
            MessageBox.Show($"Ставка {betAmount}$ на {horses[selectedHorseIndex].Name} прийнята.");
            UpdateUI();
        }
    }

    public class Horse
    {
        public string Name { get; set; }
        public int Speed { get; set; }
        public int Position { get; set; }
        public int Y { get; set; }
        public int FrameIndex { get; set; }
        public Image ImageControl { get; set; }
        public SolidColorBrush ColorBrush { get; set; }
        public string ColorName { get; set; }
        public double Coefficient { get; set; }
        public int Money { get; set; }
        public string Time { get; set; }
        public TextBlock NameLabel { get; set; }

        public bool Finished { get; set; } = false;
        public DateTime FinishTime { get; set; }
    }

}
