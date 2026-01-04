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
        // Liste de toutes les cartes du calendrier (1 à 25)
        private readonly List<DayCard> _cards = new List<DayCard>();

        // Index de la carte actuellement affichée
        private int _currentIndex = 0;

        // Date actuelle pour déterminer quelles cartes sont disponibles
        private readonly DateTime _today;

        // Timer pour mettre à jour l'affichage en temps réel
        private readonly DispatcherTimer _timer;

        // Timer pour l'animation des flocons de neige
        private readonly DispatcherTimer _snowTimer;

        // Générateur de nombres aléatoires pour les animations
        private readonly Random _rand = new Random();

        // Prénom de l'utilisateur connecté
        private readonly string _userPrenom;

        // ⭐ Lecteur de musique pour la musique d'arrière-plan
        private readonly MediaPlayer _musicPlayer = new MediaPlayer();

        // Palette de couleurs thématiques pour les cartes
        private readonly List<Color> _themeColors = new List<Color>
        {
            (Color)ColorConverter.ConvertFromString("#8B1538"),  // Bordeaux profond
            (Color)ColorConverter.ConvertFromString("#1B4332"),  // Vert forêt
            (Color)ColorConverter.ConvertFromString("#B8860B"),  // Or foncé
            (Color)ColorConverter.ConvertFromString("#2C3E50"),  // Bleu ardoise
        };

        /// <summary>
        /// Constructeur de la fenêtre principale
        /// </summary>
        /// <param name="prenom">Prénom de l'utilisateur</param>
        public MainWindow(string prenom)
        {
            try
            {
                // Initialise les composants XAML
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // En cas d'erreur critique lors de l'initialisation
                MessageBox.Show($"Erreur InitializeComponent: {ex.Message}\n\n{ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _userPrenom = prenom;
            _today = DateTime.Now;

            // ⭐ Configuration de la musique d'arrière-plan
            try
            {
                // Chemin vers le fichier audio dans le dossier "Son"
                string musicPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Son",
                    "musique.mp3"
                );

                // Si le fichier existe, le charger
                if (System.IO.File.Exists(musicPath))
                {
                    _musicPlayer.Open(new Uri(musicPath));
                    _musicPlayer.Volume = 0.3; // Volume par défaut à 30%
                    _musicPlayer.MediaEnded += (s, e) =>
                    {
                        // Recommencer la musique quand elle se termine (lecture en boucle)
                        _musicPlayer.Position = TimeSpan.Zero;
                        _musicPlayer.Play();
                    };
                    _musicPlayer.Play(); // Démarrer la lecture
                }
            }
            catch (Exception ex)
            {
                // Si erreur de chargement de la musique, continuer sans musique
                System.Diagnostics.Debug.WriteLine($"Erreur chargement musique: {ex.Message}");
            }

            // ⭐ Configuration du slider de volume
            if (VolumeSlider != null)
            {
                VolumeSlider.Value = 30; // Valeur initiale à 30%
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            }

            // Charger ou créer la progression du calendrier
            LoadOrCreateProgress();

            // Personnaliser l'affichage avec le prénom de l'utilisateur
            this.Title = $"Calendrier de l'Avent - {_userPrenom}";
            DateText.Text = $"Bonjour {_userPrenom} ! {_today.ToString("dddd dd MMMM yyyy")}";

            // Afficher la première carte
            UpdateCentralCard();

            // Timer pour mettre à jour le compte à rebours toutes les secondes
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Timer pour l'animation de neige (toutes les 100ms)
            _snowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _snowTimer.Tick += SnowTimer_Tick;
            _snowTimer.Start();

            // Animation d'entrée de la carte
            AnimateCardEntrance();
        }

        /// <summary>
        /// ⭐ Gestionnaire de changement de volume
        /// Appelé quand l'utilisateur déplace le slider
        /// </summary>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Convertir la valeur du slider (0-100) en volume (0.0-1.0)
            if (_musicPlayer != null)
            {
                _musicPlayer.Volume = e.NewValue / 100.0;
            }
        }

        /// <summary>
        /// Charge la progression sauvegardée ou crée un nouveau calendrier
        /// </summary>
        private void LoadOrCreateProgress()
        {
            // Tente de charger une sauvegarde existante
            var saveData = SaveManager.LoadProgress(_userPrenom);

            // Si une sauvegarde existe avec 25 cartes
            if (saveData != null && saveData.Cards != null && saveData.Cards.Count == 25)
            {
                // Message de bienvenue pour un utilisateur de retour
                MessageBox.Show($"Bon retour {_userPrenom} !\nDernière visite : {saveData.LastSave:dd/MM/yyyy à HH:mm}",
                    "Bienvenue", MessageBoxButton.OK, MessageBoxImage.Information);

                string[] messages = GetMessages();

                // Recréer les cartes à partir de la sauvegarde
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
                // Créer un nouveau calendrier pour un nouvel utilisateur
                MessageBox.Show($"Bienvenue {_userPrenom} !\nC'est votre premier calendrier de l'Avent 🎄",
                    "Bienvenue", MessageBoxButton.OK, MessageBoxImage.Information);

                GenerateCards();
                SaveProgress(); // Sauvegarder immédiatement
            }
        }

        /// <summary>
        /// Génère les 25 cartes du calendrier avec des couleurs aléatoires
        /// </summary>
        private void GenerateCards()
        {
            string[] messages = GetMessages();

            // Créer une carte pour chaque jour de décembre (1 à 25)
            for (int i = 1; i <= 25; i++)
            {
                DateTime availableDate = new DateTime(_today.Year, 12, i);
                _cards.Add(new DayCard
                {
                    Day = i,
                    AvailableDate = availableDate,
                    Message = messages[i - 1],
                    BgColor = _themeColors[_rand.Next(_themeColors.Count)] // Couleur aléatoire
                });
            }
        }

        /// <summary>
        /// Retourne les 25 messages du calendrier de l'Avent
        /// Un message unique pour chaque jour
        /// </summary>
        private string[] GetMessages()
        {
            return new string[]
            {
                "Un délicieux chocolat chaud vous attend pour réchauffer votre cœur et vos mains ! ☕",
                "Profitez d'un moment magique à savourer, rempli de douceur et de lumière ! ✨",
                "Un joyeux instant festif vous invite à sourire et partager le bonheur ! 🎁",
                "Douceur de l'Avent à savourer, pour réchauffer vos pensées et votre journée ! 🍪",
                "Que cette étoile filante de bonheur illumine vos rêves les plus chers ! ⭐",
                "Un cadeau surprise du jour pour illuminer votre journée et vous faire sourire ! 🎀",
                "Flocons de joie tourbillonnent autour de vous, emplissant l'air de magie ! ❄️",
                "Que la lumière de Noël réchauffe votre âme et illumine vos moments précieux ! 🕯️",
                "Bonheur hivernal à partager avec ceux que vous aimez, un vrai câlin pour l'âme ! 🌨️",
                "Magie de décembre à savourer pleinement, avec rires et souvenirs précieux ! 🎄",
                "Une belle tradition festive vous entoure de chaleur et de bonheur ! 🔔",
                "Instant de paix pour votre cœur et votre esprit, laissez la sérénité vous envelopper ! 🕊️",
                "Un délice de saison à savourer lentement, pour éveiller vos papilles et vos souvenirs ! 🥮",
                "Rêve de Noël qui réchauffe le cœur et inspire de doux moments enchantés ! 💫",
                "Chant des anges qui accompagne votre journée d'harmonie et de joie ! 👼",
                "Une merveille givrée vous invite à contempler la beauté de l'hiver avec émerveillement ! ⛄",
                "Un câlin chaleureux qui enveloppe votre esprit et vous rappelle que vous êtes aimé(e) ! 🧣",
                "Surprise pétillante qui illumine votre journée et éveille la magie autour de vous ! 🎊",
                "Biscuit au gingembre pour éveiller vos sens et rappeler la douceur des fêtes ! 🍯",
                "Couronne de l'Avent décorée de lumière et de chaleur pour embellir vos journées ! 🌿",
                "Éclat doré qui réchauffe le cœur et fait briller la magie de Noël dans vos yeux ! ✨",
                "Conte de fées qui transforme votre journée en aventure merveilleuse et lumineuse ! 📖",
                "Nuit étoilée pour rêver, se détendre et ressentir la magie du ciel hivernal ! 🌟",
                "Veille de Noël pleine d'attente joyeuse, de rires et de doux souvenirs partagés ! 🎅",
                "Joyeux Noël rempli de rires, de chaleur, de cadeaux et de moments inoubliables ! 🎄🎁"
            };
        }

        /// <summary>
        /// Sauvegarde la progression actuelle du calendrier
        /// </summary>
        private void SaveProgress()
        {
            SaveManager.SaveProgress(_userPrenom, _cards);
        }

        /// <summary>
        /// Met à jour l'affichage de la carte centrale
        /// Affiche le message, le compte à rebours ou le contenu révélé
        /// </summary>
        private void UpdateCentralCard()
        {
            var card = _cards[_currentIndex];

            // Si la carte est déjà révélée
            if (card.IsRevealed)
            {
                // Afficher l'emoji et le message complet
                DayNumberText.Text = card.RevealedEmoji;
                CardMessageText.Text = card.Message;
                CardMessageText.FontSize = 24;

                // Cacher le flocon cliquable
                var floconBorder = FindFloconBorder();
                if (floconBorder != null) floconBorder.Visibility = Visibility.Collapsed;

                var clickText = FindClickText();
                if (clickText != null) clickText.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Afficher le numéro du jour
                DayNumberText.Text = card.Day.ToString();

                // Afficher le flocon cliquable
                var floconBorder = FindFloconBorder();
                if (floconBorder != null) floconBorder.Visibility = Visibility.Visible;

                var clickText = FindClickText();
                if (clickText != null) clickText.Visibility = Visibility.Visible;

                // Si la carte est disponible (date atteinte)
                if (_today.Date >= card.AvailableDate.Date)
                {
                    CardMessageText.Text = "✨ Surprise disponible ✨";
                    CardMessageText.FontSize = 20;
                }
                else
                {
                    // Calculer et afficher le temps restant
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

            // Animation de transition de couleur
            var colorAnimation = new ColorAnimation
            {
                To = card.BgColor,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Créer un dégradé avec la couleur de la carte
            var brush = new LinearGradientBrush();
            brush.GradientStops.Add(new GradientStop(card.BgColor, 0));
            brush.GradientStops.Add(new GradientStop(DarkenColor(card.BgColor, 0.3), 1));

            CentralCard.Background = brush;
            brush.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, colorAnimation);
        }

        /// <summary>
        /// Animation d'entrée de la carte avec effet élastique
        /// </summary>
        private void AnimateCardEntrance()
        {
            // Commencer avec une échelle réduite
            var scaleTransform = new ScaleTransform(0.8, 0.8);
            CentralCard.RenderTransform = scaleTransform;
            CentralCard.RenderTransformOrigin = new Point(0.5, 0.5);

            // Animation d'agrandissement avec rebond
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new ElasticEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Oscillations = 1,
                    Springiness = 3
                }
            };

            // Animation d'opacité (fondu)
            var opacityAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600));

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            CentralCard.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Gestionnaire du bouton "Précédent"
        /// </summary>
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            // Aller à la carte précédente si possible
            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdateCentralCard();
            }
        }

        /// <summary>
        /// Gestionnaire du bouton "Suivant"
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Aller à la carte suivante si possible
            if (_currentIndex < _cards.Count - 1)
            {
                _currentIndex++;
                UpdateCentralCard();
            }
        }

        /// <summary>
        /// Gestionnaire du clic sur le flocon
        /// Lance l'animation d'ouverture si la carte est disponible
        /// </summary>
        private void FloconImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var card = _cards[_currentIndex];

            // Vérifier si la date est atteinte
            if (_today.Date < card.AvailableDate.Date) return;

            // Lancer l'animation néon puis le retournement
            AnimateNeonBorder(() => { });
        }

        /// <summary>
        /// Animation d'une bordure néon lumineuse autour de la carte
        /// </summary>
        private void AnimateNeonBorder(Action onComplete)
        {
            // Obtenir la position de la carte
            Point cardPosition = CentralCard.TransformToAncestor(MainGrid).Transform(new Point(0, 0));
            cardPosition.Y -= 10;

            // Créer un canvas pour l'effet néon
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

            // Créer le contour de la carte avec des coins arrondis
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = new Point(cardPosition.X + cornerRadius, cardPosition.Y)
            };

            // Dessiner chaque côté et coin arrondi
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + width - cornerRadius, cardPosition.Y), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + width, cardPosition.Y + cornerRadius),
                new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + width, cardPosition.Y + height - cornerRadius), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + width - cornerRadius, cardPosition.Y + height),
                new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X + cornerRadius, cardPosition.Y + height), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X, cardPosition.Y + height - cornerRadius),
                new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathFigure.Segments.Add(new LineSegment(new Point(cardPosition.X, cardPosition.Y + cornerRadius), true));
            pathFigure.Segments.Add(new ArcSegment(new Point(cardPosition.X + cornerRadius, cardPosition.Y),
                new Size(cornerRadius, cornerRadius), 0, false, SweepDirection.Clockwise, true));
            pathGeometry.Figures.Add(pathFigure);

            // Créer le chemin avec effet lumineux
            Path neonPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 8,
                Data = pathGeometry,
                StrokeDashArray = new DoubleCollection { 30, 270 }, // Tirets pour l'animation
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

            // Animation de la bordure qui tourne
            DispatcherTimer neonTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            double dashOffset = 0;
            int cycles = 0;

            neonTimer.Tick += (s, ev) =>
            {
                dashOffset += 8;
                neonPath.StrokeDashOffset = -dashOffset;

                // Après 2 cycles complets
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

        /// <summary>
        /// Animation de retournement de la carte pour révéler le contenu
        /// </summary>
        private void AnimateCardFlip(Action onComplete)
        {
            var card = _cards[_currentIndex];

            // Transformation pour l'effet de retournement
            var transform3D = new ScaleTransform(1, 1);
            CentralCard.RenderTransform = transform3D;
            CentralCard.RenderTransformOrigin = new Point(0.5, 0.5);

            // Animation de rétrécissement horizontal (carte qui se retourne)
            var shrinkAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            shrinkAnimation.Completed += (s, e) =>
            {
                // Marquer la carte comme révélée
                card.IsRevealed = true;

                // Choisir un emoji aléatoire
                string[] revealedEmojis = { "🎁", "🎄", "❄️", "⭐", "🎅", "🔔", "🕯️", "🎀" };
                card.RevealedEmoji = revealedEmojis[_rand.Next(revealedEmojis.Length)];

                // Mettre à jour l'affichage avec le contenu révélé
                DayNumberText.Text = card.RevealedEmoji;
                CardMessageText.Text = card.Message;
                CardMessageText.FontSize = 24;

                // ⭐ SAUVEGARDE AUTOMATIQUE quand on ouvre une carte
                SaveProgress();

                // Animation d'agrandissement (fin du retournement)
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

        /// <summary>
        /// Tick du timer de neige - crée et nettoie les flocons
        /// </summary>
        private void SnowTimer_Tick(object? sender, EventArgs e)
        {
            // Créer un nouveau flocon aléatoirement (33% de chance)
            if (_rand.Next(0, 3) == 0) CreateSnowflake();

            // Nettoyer les flocons sortis de l'écran
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

        /// <summary>
        /// Crée un flocon de neige animé avec propriétés aléatoires
        /// </summary>
        private void CreateSnowflake()
        {
            // Créer une ellipse blanche semi-transparente
            Ellipse snowflake = new Ellipse
            {
                Width = _rand.Next(3, 8),
                Height = _rand.Next(3, 8),
                Fill = new SolidColorBrush(Color.FromArgb((byte)_rand.Next(150, 255), 255, 255, 255)),
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 2 }
            };

            // Position de départ aléatoire en haut
            double startX = _rand.NextDouble() * ActualWidth;
            Canvas.SetLeft(snowflake, startX);
            Canvas.SetTop(snowflake, -10);
            SnowCanvas.Children.Add(snowflake);

            // Animation de chute verticale
            DoubleAnimation fallAnimation = new DoubleAnimation
            {
                From = -10,
                To = ActualHeight + 10,
                Duration = TimeSpan.FromSeconds(_rand.Next(5, 12)),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn }
            };

            // Animation de balancement horizontal
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

        /// <summary>
        /// Trouve le border contenant le flocon cliquable
        /// </summary>
        private Border FindFloconBorder()
        {
            return FindVisualChild<Border>(CardContent, b => b.Cursor == Cursors.Hand);
        }

        /// <summary>
        /// Trouve le texte "Cliquez pour ouvrir !"
        /// </summary>
        private TextBlock FindClickText()
        {
            return FindVisualChild<TextBlock>(CardContent, t => t.Text == "Cliquez pour ouvrir !");
        }

        /// <summary>
        /// Méthode utilitaire pour parcourir l'arbre visuel
        /// Recherche un élément enfant d'un type spécifique qui correspond au prédicat
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            // Parcourir tous les enfants
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Si l'enfant correspond au type et au prédicat
                if (child is T typedChild && predicate(typedChild))
                    return typedChild;

                // Recherche récursive dans les sous-enfants
                var result = FindVisualChild(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Assombrit une couleur d'un certain pourcentage
        /// </summary>
        private Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }

        /// <summary>
        /// Tick du timer principal - met à jour l'affichage toutes les secondes
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCentralCard();
        }

        /// <summary>
        /// Appelé à la fermeture de la fenêtre
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // ⭐ SAUVEGARDE FINALE avant de fermer
            SaveProgress();

            // ⭐ Arrêter et libérer le lecteur de musique
            _musicPlayer?.Stop();
            _musicPlayer?.Close();

            // Arrêter les timers
            _timer?.Stop();
            _snowTimer?.Stop();

            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Classe représentant une carte du calendrier
    /// </summary>
    public class DayCard
    {
        /// <summary>Numéro du jour (1 à 25)</summary>
        public int Day { get; set; }

        /// <summary>Date à partir de laquelle la carte devient disponible</summary>
        public DateTime AvailableDate { get; set; }

        /// <summary>Message secret de la carte</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Couleur de fond de la carte</summary>
        public Color BgColor { get; set; }

        /// <summary>Indique si la carte a été ouverte</summary>
        public bool IsRevealed { get; set; } = false;

        /// <summary>Emoji affiché quand la carte est révélée</summary>
        public string RevealedEmoji { get; set; } = "🎁";
        }
    }