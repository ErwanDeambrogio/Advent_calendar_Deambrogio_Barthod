using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Advent_calendar_Deambrogio_Barthod
{
    public partial class MainWindow : Window
    {
        private readonly List<DayCard> _cards = new List<DayCard>();
        private int _currentIndex = 0;
        private readonly DateTime _today;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _snowTimer;
        private readonly Random _rand = new Random();
        private readonly string _userPrenom;
        private readonly List<Color> _themeColors = new List<Color>
        {
            (Color)ColorConverter.ConvertFromString("#C41E3A"),
            (Color)ColorConverter.ConvertFromString("#1B5E20"),
            (Color)ColorConverter.ConvertFromString("#8B0000"),
            (Color)ColorConverter.ConvertFromString("#2E4057")
        };

        public MainWindow(string prenom)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur InitializeComponent: {ex.Message}\n\n{ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _userPrenom = prenom;
            _today = DateTime.Now;

            // Charger ou créer les données
            LoadOrCreateProgress();

            // Mettre à jour l'affichage avec le prénom
            this.Title = $"Calendrier de l'Avent - {_userPrenom}";
            DateText.Text = $"Bonjour {_userPrenom} ! {_today.ToString("dddd dd MMMM yyyy")}";

            UpdateCentralCard();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _snowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _snowTimer.Tick += SnowTimer_Tick;
            _snowTimer.Start();

            AnimateCardEntrance();
        }

        private void LoadOrCreateProgress()
        {
            var saveData = SaveManager.LoadProgress(_userPrenom);

            if (saveData != null && saveData.Cards != null && saveData.Cards.Count == 25)
            {
                // Charger la progression existante
                MessageBox.Show($"Bon retour {_userPrenom} !\nDernière visite : {saveData.LastSave:dd/MM/yyyy à HH:mm}",
                    "Bienvenue", MessageBoxButton.OK, MessageBoxImage.Information);

                string[] messages = GetMessages();

                for (int i = 0; i < saveData.Cards.Count; i++)
                {
                    var cardData = saveData.Cards[i];
                    DateTime availableDate = new DateTime(_today.Year, 12, cardData.Day);

                    _cards.Add(new DayCard
                    {
                        Day = cardData.Day,
                        AvailableDate = availableDate,
                        Message = messages[cardData.Day - 1],
                        BgColor = cardData.GetColor(),
                        IsRevealed = cardData.IsRevealed,
                        RevealedEmoji = cardData.RevealedEmoji
                    });
                }
            }
            else
            {
                // Créer une nouvelle progression
                MessageBox.Show($"Bienvenue {_userPrenom} !\nC'est votre premier calendrier de l'Avent 🎄",
                    "Bienvenue", MessageBoxButton.OK, MessageBoxImage.Information);

                GenerateCards();
                SaveProgress();
            }
        }

        private void GenerateCards()
        {
            string[] messages = GetMessages();

            for (int i = 1; i <= 25; i++)
            {
                DateTime availableDate = new DateTime(_today.Year, 12, i);
                _cards.Add(new DayCard
                {
                    Day = i,
                    AvailableDate = availableDate,
                    Message = messages[i - 1],
                    BgColor = _themeColors[_rand.Next(_themeColors.Count)]
                });
            }
        }

        private string[] GetMessages()
        {
            return new string[]
            {
                "Un chocolat chaud vous attend ! ☕",
                "Moment magique à savourer ! ✨",
                "Joyeux instant festif ! 🎁",
                "Douceur de l'Avent ! 🍪",
                "Étoile filante de bonheur ! ⭐",
                "Cadeau surprise du jour ! 🎀",
                "Flocons de joie ! ❄️",
                "Lumière de Noël ! 🕯️",
                "Bonheur hivernal ! 🌨️",
                "Magie de décembre ! 🎄",
                "Tradition festive ! 🔔",
                "Instant de paix ! 🕊️",
                "Délice de saison ! 🥮",
                "Rêve de Noël ! 💫",
                "Chant des anges ! 👼",
                "Merveille givrée ! ⛄",
                "Câlin chaleureux ! 🧣",
                "Surprise pétillante ! 🎊",
                "Biscuit au gingembre ! 🍯",
                "Couronne de l'Avent ! 🌿",
                "Éclat doré ! ✨",
                "Conte de fées ! 📖",
                "Nuit étoilée ! 🌟",
                "Veille de Noël ! 🎅",
                "Joyeux Noël ! 🎄🎁"
            };
        }

        private void SaveProgress()
        {
            SaveManager.SaveProgress(_userPrenom, _cards);
        }

        private void UpdateCentralCard()
        {
            var card = _cards[_currentIndex];

            if (card.IsRevealed)
            {
                DayNumberText.Text = card.RevealedEmoji;
                CardMessageText.Text = card.Message;
                CardMessageText.FontSize = 24;

                var floconBorder = FindFloconBorder();
                if (floconBorder != null) floconBorder.Visibility = Visibility.Collapsed;

                var clickText = FindClickText();
                if (clickText != null) clickText.Visibility = Visibility.Collapsed;
            }
            else
            {
                DayNumberText.Text = card.Day.ToString();

                var floconBorder = FindFloconBorder();
                if (floconBorder != null) floconBorder.Visibility = Visibility.Visible;

                var clickText = FindClickText();
                if (clickText != null) clickText.Visibility = Visibility.Visible;

                if (_today.Date >= card.AvailableDate.Date)
                {
                    CardMessageText.Text = "✨ Surprise disponible ✨";
                    CardMessageText.FontSize = 20;
                }
                else
                {
                    TimeSpan remaining = card.AvailableDate - _today;
                    int days = remaining.Days;
                    int hours = remaining.Hours;
                    int minutes = remaining.Minutes;

                    if (days > 0)
                        CardMessageText.Text = $"🎁 Encore {days} jour{(days > 1 ? "s" : "")} et {hours}h";
                    else if (hours > 0)
                        CardMessageText.Text = $"⏰ Plus que {hours}h et {minutes}min";
                    else
                        CardMessageText.Text = $"⏰ Plus que {minutes} minute{(minutes > 1 ? "s" : "")}";

                    CardMessageText.FontSize = 20;
                }
            }

            var colorAnimation = new ColorAnimation
            {
                To = card.BgColor,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var brush = new LinearGradientBrush();
            brush.GradientStops.Add(new GradientStop(card.BgColor, 0));
            brush.GradientStops.Add(new GradientStop(DarkenColor(card.BgColor, 0.3), 1));

            CentralCard.Background = brush;
            brush.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, colorAnimation);
        }

        private void AnimateCardEntrance()
        {
            var scaleTransform = new ScaleTransform(0.8, 0.8);
            CentralCard.RenderTransform = scaleTransform;
            CentralCard.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 3 }
            };

            var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600));

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            CentralCard.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdateCentralCard();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _cards.Count - 1)
            {
                _currentIndex++;
                UpdateCentralCard();
            }
        }

        private void FloconImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var card = _cards[_currentIndex];
            if (_today.Date < card.AvailableDate.Date) return;

            AnimateNeonBorder(() => { });
        }

        private void AnimateNeonBorder(Action onComplete)
        {
            Point cardPosition = CentralCard.TransformToAncestor(MainGrid).Transform(new Point(0, 0));
            cardPosition.Y -= 10;

            Canvas neonCanvas = new Canvas
            {
                Width = MainGrid.ActualWidth,
                Height = MainGrid.ActualHeight,
                ClipToBounds = false
            };
            MainGrid.Children.Add(neonCanvas);

            double width = CentralCard.ActualWidth;
            double height = CentralCard.ActualHeight;
            double cornerRadius = 30;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = new Point(cardPosition.X + cornerRadius, cardPosition.Y)
            };

            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + width - cornerRadius, cardPosition.Y), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + width, cardPosition.Y + cornerRadius), new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + width, cardPosition.Y + height - cornerRadius), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + width - cornerRadius, cardPosition.Y + height), new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + cornerRadius, cardPosition.Y + height), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X, cardPosition.Y + height - cornerRadius), new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X, cardPosition.Y + cornerRadius), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + cornerRadius, cardPosition.Y), new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathGeometry.Figures.Add(pathFigure);

            Path neonPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 8,
                Data = pathGeometry,
                StrokeDashArray = new DoubleCollection { 30, 270 },
                StrokeDashCap = PenLineCap.Round,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 30,
                    ShadowDepth = 0,
                    Opacity = 1
                }
            };
            neonCanvas.Children.Add(neonPath);

            DispatcherTimer neonTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            double dashOffset = 0;
            int cycles = 0;

            neonTimer.Tick += (s, ev) =>
            {
                dashOffset += 8;
                neonPath.StrokeDashOffset = -dashOffset;

                if (dashOffset >= 1200)
                {
                    cycles++;
                    if (cycles >= 2)
                    {
                        neonTimer.Stop();
                        MainGrid.Children.Remove(neonCanvas);
                        AnimateCardFlip(() => onComplete?.Invoke());
                    }
                }
            };

            neonTimer.Start();
        }

        private void AnimateCardFlip(Action onComplete)
        {
            var card = _cards[_currentIndex];

            var transform3D = new ScaleTransform(1, 1);
            CentralCard.RenderTransform = transform3D;
            CentralCard.RenderTransformOrigin = new Point(0.5, 0.5);

            var shrinkAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            shrinkAnimation.Completed += (s, e) =>
            {
                card.IsRevealed = true;

                string[] revealedEmojis = { "🎁", "🎄", "❄️", "⭐", "🎅", "🔔", "🕯️", "🎀" };
                card.RevealedEmoji = revealedEmojis[_rand.Next(revealedEmojis.Length)];

                DayNumberText.Text = card.RevealedEmoji;
                CardMessageText.Text = card.Message;
                CardMessageText.FontSize = 24;

                // ⭐ SAUVEGARDE AUTOMATIQUE quand on ouvre une carte
                SaveProgress();

                var expandAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                expandAnimation.Completed += (ss, ee) => onComplete?.Invoke();

                transform3D.BeginAnimation(ScaleTransform.ScaleXProperty, expandAnimation);
            };

            transform3D.BeginAnimation(ScaleTransform.ScaleXProperty, shrinkAnimation);
        }

        private void SnowTimer_Tick(object? sender, EventArgs e)
        {
            if (_rand.Next(0, 3) == 0) CreateSnowflake();

            List<UIElement> toRemove = new List<UIElement>();
            foreach (UIElement element in SnowCanvas.Children)
            {
                if (element is Ellipse && Canvas.GetTop((Ellipse)element) > ActualHeight)
                {
                    toRemove.Add(element);
                }
            }

            foreach (var element in toRemove) SnowCanvas.Children.Remove(element);
        }

        private void CreateSnowflake()
        {
            Ellipse snowflake = new Ellipse
            {
                Width = _rand.Next(3, 8),
                Height = _rand.Next(3, 8),
                Fill = new SolidColorBrush(Color.FromArgb((byte)_rand.Next(150, 255), 255, 255, 255)),
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 2 }
            };

            double startX = _rand.NextDouble() * ActualWidth;
            Canvas.SetLeft(snowflake, startX);
            Canvas.SetTop(snowflake, -10);
            SnowCanvas.Children.Add(snowflake);

            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = -10,
                To = ActualHeight + 10,
                Duration = TimeSpan.FromSeconds(_rand.Next(5, 12)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn }
            };

            DoubleAnimation swayAnimation = new DoubleAnimation
            {
                From = startX,
                To = startX + _rand.Next(-50, 50),
                Duration = TimeSpan.FromSeconds(_rand.Next(2, 4)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            snowflake.BeginAnimation(Canvas.TopProperty, fallAnimation);
            snowflake.BeginAnimation(Canvas.LeftProperty, swayAnimation);
        }

        private Border FindFloconBorder()
        {
            return FindVisualChild<Border>(CardContent, b => b.Cursor == Cursors.Hand);
        }

        private TextBlock FindClickText()
        {
            return FindVisualChild<TextBlock>(CardContent, t => t.Text == "Cliquez pour ouvrir !");
        }

        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && predicate(typedChild))
                    return typedChild;

                var result = FindVisualChild(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        private Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCentralCard();
        }

        protected override void OnClosed(EventArgs e)
        {
            // ⭐ SAUVEGARDE FINALE avant de fermer
            SaveProgress();
            _timer?.Stop();
            _snowTimer?.Stop();
            base.OnClosed(e);
        }
    }

    public class DayCard
    {
        public int Day { get; set; }
        public DateTime AvailableDate { get; set; }
        public string Message { get; set; } = string.Empty;
        public Color BgColor { get; set; }
        public bool IsRevealed { get; set; } = false;
        public string RevealedEmoji { get; set; } = "🎁";
    }
}