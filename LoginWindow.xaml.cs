using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls;

namespace Advent_calendar_Deambrogio_Barthod
{
    public partial class LoginWindow : Window
    {
        private readonly DispatcherTimer _snowTimer;
        private readonly Random _rand = new Random();
        public string UserPrenom { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();

            // Animation neige
            _snowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _snowTimer.Tick += SnowTimer_Tick;
            _snowTimer.Start();

            // Focus sur le TextBox au démarrage
            Loaded += (s, e) => PrenomTextBox.Focus();

            // Afficher les utilisateurs existants
            ShowExistingUsers();
        }

        private void ShowExistingUsers()
        {
            try
            {
                var users = SaveManager.GetExistingUsers();
                if (users != null && users.Count > 0)
                {
                    ExistingUsersText.Text = $"Utilisateurs existants : {string.Join(", ", users)}";
                    ExistingUsersText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // Si erreur de chargement des utilisateurs, on continue sans afficher
                System.Diagnostics.Debug.WriteLine($"Erreur chargement utilisateurs: {ex.Message}");
            }
        }

        private void ContinuerButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndContinue();
        }

        private void PrenomTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidateAndContinue();
            }
            else if (e.Key == Key.Escape)
            {
                // Permettre de fermer avec Echap
                DialogResult = false;
                Close();
            }
        }

        private void ValidateAndContinue()
        {
            string prenom = PrenomTextBox.Text.Trim();

            // Validation du prénom
            if (string.IsNullOrEmpty(prenom))
            {
                ShowError("Veuillez entrer votre prénom !");
                return;
            }

            if (prenom.Length < 2)
            {
                ShowError("Le prénom doit contenir au moins 2 caractères !");
                return;
            }

            // Vérifier que le prénom contient uniquement des lettres, espaces ou tirets
            if (!prenom.All(c => char.IsLetter(c) || c == ' ' || c == '-'))
            {
                ShowError("Le prénom ne peut contenir que des lettres !");
                return;
            }

            // Validation réussie
            UserPrenom = prenom;
            DialogResult = true;

            // Arrêter les animations avant de fermer
            _snowTimer?.Stop();

            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;

            // Animation de secousse sur le TextBox
            try
            {
                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 10,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };

                var transform = new TranslateTransform();
                PrenomTextBox.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch
            {
                // Si l'animation échoue, ce n'est pas grave
            }

            // Remettre le focus sur le TextBox
            PrenomTextBox.SelectAll();
            PrenomTextBox.Focus();
        }

        private void SnowTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Créer des flocons de neige aléatoirement
                if (_rand.Next(0, 3) == 0)
                {
                    CreateSnowflake();
                }

                // Nettoyer les flocons qui sont sortis de l'écran
                List<UIElement> toRemove = new List<UIElement>();
                foreach (UIElement element in SnowCanvas.Children)
                {
                    if (element is Ellipse ellipse)
                    {
                        double top = Canvas.GetTop(ellipse);
                        if (double.IsNaN(top) || top > ActualHeight)
                        {
                            toRemove.Add(element);
                        }
                    }
                }

                foreach (var element in toRemove)
                {
                    SnowCanvas.Children.Remove(element);
                }
            }
            catch
            {
                // En cas d'erreur dans l'animation, on continue
            }
        }

        private void CreateSnowflake()
        {
            try
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
            catch
            {
                // Si la création du flocon échoue, on continue
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Arrêter le timer de neige
            _snowTimer?.Stop();

            // Si l'utilisateur ferme la fenêtre sans valider, DialogResult sera null
            // ce qui fera que App.xaml.cs fermera l'application

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _snowTimer?.Stop();
            base.OnClosed(e);
        }
    }
}