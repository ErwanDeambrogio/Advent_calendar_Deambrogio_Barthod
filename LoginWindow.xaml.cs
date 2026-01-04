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
    /// <summary>
    /// Fenêtre de connexion pour le calendrier de l'Avent
    /// Permet à l'utilisateur d'entrer son prénom avec une animation de neige décorative
    /// </summary>
    public partial class LoginWindow : Window
    {
        // Timer pour gérer l'animation des flocons de neige
        private readonly DispatcherTimer _snowTimer;

        // Générateur de nombres aléatoires pour les animations
        private readonly Random _rand = new Random();

        // Propriété publique qui stocke le prénom de l'utilisateur après validation
        public string UserPrenom { get; private set; }

        /// <summary>
        /// Constructeur de la fenêtre de login
        /// </summary>
        public LoginWindow()
        {
            // Initialise les composants XAML
            InitializeComponent();

            // Configuration du timer pour l'animation de neige
            // Se déclenche toutes les 100 millisecondes
            _snowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _snowTimer.Tick += SnowTimer_Tick; // Associe l'événement à la méthode
            _snowTimer.Start(); // Démarre l'animation

            // Met le focus sur le champ de texte dès que la fenêtre est chargée
            // Permet à l'utilisateur de taper directement sans cliquer
            Loaded += (s, e) => PrenomTextBox.Focus();

            // Affiche la liste des utilisateurs existants (s'il y en a)
            ShowExistingUsers();
        }

        /// <summary>
        /// Récupère et affiche les utilisateurs existants depuis le système de sauvegarde
        /// </summary>
        private void ShowExistingUsers()
        {
            try
            {
                // Récupère la liste des prénoms déjà enregistrés
                var users = SaveManager.GetExistingUsers();

                // Si des utilisateurs existent
                if (users != null && users.Count > 0)
                {
                    // Affiche la liste sous forme de texte séparé par des virgules
                    ExistingUsersText.Text = $"Utilisateurs existants : {string.Join(", ", users)}";
                    ExistingUsersText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur (fichier corrompu, etc.), on continue sans afficher
                // L'erreur est juste tracée pour le debug
                System.Diagnostics.Debug.WriteLine($"Erreur chargement utilisateurs: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire du clic sur le bouton "Continuer"
        /// </summary>
        private void ContinuerButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndContinue();
        }

        /// <summary>
        /// Gestionnaire des touches clavier dans le champ de texte
        /// </summary>
        private void PrenomTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Si l'utilisateur appuie sur Entrée, valider et continuer
            if (e.Key == Key.Enter)
            {
                ValidateAndContinue();
            }
            // Si l'utilisateur appuie sur Échap, fermer l'application
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        /// <summary>
        /// Valide le prénom saisi et ferme la fenêtre si valide
        /// </summary>
        private void ValidateAndContinue()
        {
            // Récupère le texte et enlève les espaces avant/après
            string prenom = PrenomTextBox.Text.Trim();

            // Validation 1 : Le champ ne doit pas être vide
            if (string.IsNullOrEmpty(prenom))
            {
                ShowError("Veuillez entrer votre prénom !");
                return;
            }

            // Validation 2 : Le prénom doit avoir au moins 2 caractères
            if (prenom.Length < 2)
            {
                ShowError("Le prénom doit contenir au moins 2 caractères !");
                return;
            }

            // Validation 3 : Le prénom ne peut contenir que des lettres, espaces ou tirets
            // Permet des prénoms composés comme "Jean-Pierre" ou "Marie Anne"
            if (!prenom.All(c => char.IsLetter(c) || c == ' ' || c == '-'))
            {
                ShowError("Le prénom ne peut contenir que des lettres !");
                return;
            }

            // Si toutes les validations passent :
            UserPrenom = prenom; // Stocke le prénom
            DialogResult = true;  // Indique que la fenêtre s'est fermée avec succès

            // Arrête l'animation de neige pour libérer les ressources
            _snowTimer?.Stop();

            // Ferme la fenêtre
            Close();
        }

        /// <summary>
        /// Affiche un message d'erreur avec une animation de secousse
        /// </summary>
        /// <param name="message">Le message d'erreur à afficher</param>
        private void ShowError(string message)
        {
            // Affiche le message d'erreur
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;

            // Crée une animation de secousse sur le champ de texte pour attirer l'attention
            try
            {
                // Animation qui bouge le TextBox de gauche à droite
                var shakeAnimation = new DoubleAnimation
                {
                    From = 0,        // Position de départ
                    To = 10,         // Position d'arrivée (10 pixels à droite)
                    Duration = TimeSpan.FromMilliseconds(50), // Durée rapide
                    AutoReverse = true, // Retour automatique à la position initiale
                    RepeatBehavior = new RepeatBehavior(3) // Répète 3 fois (6 mouvements au total)
                };

                // Applique la transformation au TextBox
                var transform = new TranslateTransform();
                PrenomTextBox.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
            }
            catch
            {
                // Si l'animation échoue, ce n'est pas critique
            }

            // Sélectionne tout le texte et remet le focus pour faciliter la correction
            PrenomTextBox.SelectAll();
            PrenomTextBox.Focus();
        }

        /// <summary>
        /// Méthode appelée à chaque tick du timer (toutes les 100ms)
        /// Gère la création et le nettoyage des flocons de neige
        /// </summary>
        private void SnowTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Crée aléatoirement un nouveau flocon (33% de chance à chaque tick)
                if (_rand.Next(0, 3) == 0)
                {
                    CreateSnowflake();
                }

                // Nettoie les flocons qui sont sortis de l'écran (en bas)
                List<UIElement> toRemove = new List<UIElement>();

                // Parcourt tous les éléments du canvas de neige
                foreach (UIElement element in SnowCanvas.Children)
                {
                    if (element is Ellipse ellipse)
                    {
                        double top = Canvas.GetTop(ellipse);

                        // Si le flocon est en dessous de la fenêtre, le marquer pour suppression
                        if (double.IsNaN(top) || top > ActualHeight)
                        {
                            toRemove.Add(element);
                        }
                    }
                }

                // Supprime les flocons marqués pour libérer la mémoire
                foreach (var element in toRemove)
                {
                    SnowCanvas.Children.Remove(element);
                }
            }
            catch
            {
                // En cas d'erreur, on continue sans planter l'application
            }
        }

        /// <summary>
        /// Crée un nouveau flocon de neige avec des propriétés aléatoires
        /// </summary>
        private void CreateSnowflake()
        {
            try
            {
                // Crée un flocon (ellipse blanche)
                Ellipse snowflake = new Ellipse
                {
                    // Taille aléatoire entre 3 et 8 pixels
                    Width = _rand.Next(3, 8),
                    Height = _rand.Next(3, 8),

                    // Couleur blanche avec opacité aléatoire (effet de profondeur)
                    Fill = new SolidColorBrush(Color.FromArgb((byte)_rand.Next(150, 255), 255, 255, 255)),

                    // Effet de flou pour un rendu plus doux
                    Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 2 }
                };

                // Position horizontale aléatoire en haut de la fenêtre
                double startX = _rand.NextDouble() * ActualWidth;
                Canvas.SetLeft(snowflake, startX);
                Canvas.SetTop(snowflake, -10); // Commence au-dessus de la fenêtre

                // Ajoute le flocon au canvas
                SnowCanvas.Children.Add(snowflake);

                // Animation de chute verticale
                DoubleAnimation fallAnimation = new DoubleAnimation
                {
                    From = -10, // Commence en haut
                    To = ActualHeight + 10, // Tombe jusqu'en bas
                    Duration = TimeSpan.FromSeconds(_rand.Next(5, 12)), // Durée aléatoire (vitesse variable)
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } // Accélération progressive
                };

                // Animation de balancement horizontal (effet de vent)
                DoubleAnimation swayAnimation = new DoubleAnimation
                {
                    From = startX, // Position de départ
                    To = startX + _rand.Next(-50, 50), // Balancement aléatoire
                    Duration = TimeSpan.FromSeconds(_rand.Next(2, 4)), // Durée du balancement
                    AutoReverse = true, // Va-et-vient
                    RepeatBehavior = RepeatBehavior.Forever // Répète indéfiniment
                };

                // Applique les animations au flocon
                snowflake.BeginAnimation(Canvas.TopProperty, fallAnimation);
                snowflake.BeginAnimation(Canvas.LeftProperty, swayAnimation);
            }
            catch
            {
                // Si la création échoue, on continue sans ce flocon
            }
        }

        /// <summary>
        /// Appelé quand la fenêtre est sur le point de se fermer
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Arrête le timer de neige pour libérer les ressources
            _snowTimer?.Stop();

            // Si DialogResult est null (fermeture sans validation),
            // l'application se fermera complètement (géré dans App.xaml.cs)

            base.OnClosing(e);
        }

        /// <summary>
        /// Appelé après la fermeture de la fenêtre
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Double sécurité pour arrêter le timer
            _snowTimer?.Stop();
            base.OnClosed(e);
        }
    }
}