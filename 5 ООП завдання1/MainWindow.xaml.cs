using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp10
{
    public partial class MainWindow : Window
    {
        private int horseCount = 4; // не const — тепер змінна
        private const int FrameCount = 12;
        private const string ImagePath = @"C:\\Users\\User\\source\\repos\\5-12\\5 ООП завдання1\\images\\";
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
            InitializeComponent(); // СПОЧАТКУ ініціалізуємо всі елементи XAML

            LoadBackground();

            // Генеруємо комбобокси для кольорів
            if (int.TryParse(((ComboBoxItem)HorseCountSelector.SelectedItem)?.Content.ToString(), out int count))
            {
                GenerateColorSelectors(count);
            }

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

        private void GenerateColorSelectors(int count)
        {
            if (ColorPickersPanel == null)
            {
                return; // Просто завершуємо метод, якщо панель немає
            }

            ColorPickersPanel.Children.Clear();

            string[] colors = { "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Cyan", "Magenta" };

            for (int i = 0; i < count; i++)
            {
                var cb = new ComboBox
                {
                    ItemsSource = colors.ToList(),
                    SelectedIndex = i % colors.Length,
                    Width = 130,
                    Margin = new Thickness(2),
                    ItemContainerStyle = (Style)FindResource("ColorComboBoxItemStyle"),
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                // Налаштування шаблону самого ComboBox
                cb.ItemTemplate = new DataTemplate();
                var stack = new FrameworkElementFactory(typeof(StackPanel));
                stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

                var rect = new FrameworkElementFactory(typeof(Rectangle));
                rect.SetBinding(Rectangle.FillProperty, new Binding());
                rect.SetValue(Rectangle.WidthProperty, 16.0);
                rect.SetValue(Rectangle.HeightProperty, 16.0);
                rect.SetValue(MarginProperty, new Thickness(0, 0, 5, 0));

                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetBinding(TextBlock.TextProperty, new Binding());

                stack.AppendChild(rect);
                stack.AppendChild(text);

                cb.ItemTemplate.VisualTree = stack;

                ColorPickersPanel.Children.Add(cb); // Додаємо комбобокс до панелі
            }

            // ВИДАЛЕННЯ ЛОГУВАННЯ
            // MessageBox.Show($"Generated {ColorPickersPanel.Children.Count} color selectors.");
        }
        private void HorseCountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (int.TryParse(((ComboBoxItem)HorseCountSelector.SelectedItem)?.Content.ToString(), out int count))
            {
                GenerateColorSelectors(count);
                InitHorses(); // Оновлюємо коней
            }
        }

        private void InitHorses()
        {
            if (RaceCanvas == null)
            {
                return;
            }
            RaceCanvas.Children.Clear();
            LoadBackground();
            horses.Clear();

            if (!int.TryParse(((ComboBoxItem)HorseCountSelector.SelectedItem)?.Content.ToString(), out horseCount))
                horseCount = 4;

            List<SolidColorBrush> selectedColors = new List<SolidColorBrush>();
            foreach (ComboBox cb in ColorPickersPanel.Children)
            {
                var colorStr = cb.SelectedItem?.ToString();
                if (colorStr != null && TryGetBrushFromName(colorStr, out var brush))
                    selectedColors.Add(brush);
                else
                    selectedColors.Add(Brushes.Gray); // за замовчуванням
            }

            string[] horseNames = { "Lucky", "Ranger", "Willow", "Tucker" };

            for (int i = 0; i < horseCount; i++)
            {
                Horse horse = new Horse
                {
                    Name = horseNames[i % horseNames.Length] + $" #{i + 1}",
                    Speed = rand.Next(5, 9),
                    Position = 0,
                    FrameIndex = 0,
                    Y = 10 + i * 50,
                    ColorName = selectedColors[i].Color.ToString(),
                    Coefficient = 1.25 + i * 0.1,
                    Money = 0,
                    ColorBrush = selectedColors[i],
                    ImageControl = new Image { Width = 51, Height = 51 }
                };

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

            // Оновлюємо список коней у HorseSelector
            HorseSelector.ItemsSource = horses.Select((h, i) => $"{i + 1}. {h.Name}");
            HorseSelector.SelectedIndex = 0;
        }

        private bool TryGetBrushFromName(string name, out SolidColorBrush brush)
        {
            try
            {
                var converter = new System.Windows.Media.BrushConverter();
                brush = (SolidColorBrush)converter.ConvertFromString(name);
                return true;
            }
            catch
            {
                brush = Brushes.Gray;
                return false;
            }
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

                horse.Position += horse.Speed;
                Canvas.SetLeft(horse.ImageControl, horse.Position);
                Canvas.SetTop(horse.ImageControl, horse.Y);
                Canvas.SetLeft(horse.NameLabel, horse.Position);
                Canvas.SetTop(horse.NameLabel, horse.Y - 20);

                horse.FrameIndex = (horse.FrameIndex + 1) % FrameCount;
                UpdateHorseImage(horse);

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
                Колір = h.ColorName,
                Швидкість = h.Speed,
                h.Coefficient,
                h.Position,
                h.Time,
                Виграш = h.Money
            }).ToList();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            InitHorses(); // Оновлюємо коней перед стартом
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