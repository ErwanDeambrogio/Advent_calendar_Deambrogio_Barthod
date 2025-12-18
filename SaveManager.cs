using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace Advent_calendar_Deambrogio_Barthod
{
    public static class SaveManager
    {
        // Dossier où seront sauvegardés les fichiers TXT
        private static readonly string SaveDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Sauvegardes"
        );

        static SaveManager()
        {
            // Créer le dossier "Sauvegardes" s'il n'existe pas
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        /// <summary>
        /// Récupère la liste des prénoms ayant des sauvegardes
        /// </summary>
        public static List<string> GetExistingUsers()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    return new List<string>();

                // Cherche tous les fichiers .txt dans le dossier Sauvegardes
                return Directory.GetFiles(SaveDirectory, "*.txt")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(n => n)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Sauvegarde la progression de l'utilisateur dans un fichier TXT
        /// </summary>
        public static void SaveProgress(string prenom, List<DayCard> cards)
        {
            try
            {
                string fileName = GetSafeFileName(prenom);
                string filePath = Path.Combine(SaveDirectory, $"{fileName}.txt");

                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    // Écrire le prénom
                    writer.WriteLine($"PRENOM:{prenom}");

                    // Écrire la date de sauvegarde
                    writer.WriteLine($"DATE:{DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                    // Séparateur
                    writer.WriteLine("===CARTES===");

                    // Écrire chaque carte (une ligne par carte)
                    foreach (var card in cards)
                    {
                        // Format: Jour|Ouvert(Oui/Non)|Emoji|CouleurR|CouleurG|CouleurB
                        string isRevealed = card.IsRevealed ? "Oui" : "Non";
                        writer.WriteLine($"{card.Day}|{isRevealed}|{card.RevealedEmoji}|{card.BgColor.R}|{card.BgColor.G}|{card.BgColor.B}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la sauvegarde : {ex.Message}",
                    "Erreur de sauvegarde",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Charge la progression d'un utilisateur depuis son fichier TXT
        /// </summary>
        public static SaveData LoadProgress(string prenom)
        {
            try
            {
                string fileName = GetSafeFileName(prenom);
                string filePath = Path.Combine(SaveDirectory, $"{fileName}.txt");

                // Si le fichier n'existe pas, retourner null
                if (!File.Exists(filePath))
                    return null;

                SaveData saveData = new SaveData
                {
                    Cards = new List<CardSaveData>()
                };

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    bool readingCards = false;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("PRENOM:"))
                        {
                            saveData.Prenom = line.Substring(7);
                        }
                        else if (line.StartsWith("DATE:"))
                        {
                            string dateStr = line.Substring(5);
                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                            {
                                saveData.LastSave = parsedDate;
                            }
                        }
                        else if (line == "===CARTES===")
                        {
                            readingCards = true;
                        }
                        else if (readingCards && !string.IsNullOrWhiteSpace(line))
                        {
                            // Parser: Jour|Ouvert|Emoji|R|G|B
                            string[] parts = line.Split('|');
                            if (parts.Length == 6)
                            {
                                CardSaveData card = new CardSaveData
                                {
                                    Day = int.Parse(parts[0]),
                                    IsRevealed = parts[1] == "Oui",
                                    RevealedEmoji = parts[2],
                                    BgColorR = byte.Parse(parts[3]),
                                    BgColorG = byte.Parse(parts[4]),
                                    BgColorB = byte.Parse(parts[5])
                                };
                                saveData.Cards.Add(card);
                            }
                        }
                    }
                }

                return saveData;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement : {ex.Message}",
                    "Erreur de chargement",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                return null;
            }
        }

        /// <summary>
        /// Vérifie si un utilisateur a déjà une sauvegarde
        /// </summary>
        public static bool UserExists(string prenom)
        {
            string fileName = GetSafeFileName(prenom);
            string filePath = Path.Combine(SaveDirectory, $"{fileName}.txt");
            return File.Exists(filePath);
        }

        /// <summary>
        /// Convertit un prénom en nom de fichier valide
        /// </summary>
        private static string GetSafeFileName(string prenom)
        {
            string safe = prenom.Trim();

            // Remplacer les caractères interdits par des underscores
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }

            return safe;
        }

        /// <summary>
        /// Retourne le chemin du dossier de sauvegarde
        /// </summary>
        public static string GetSaveLocation()
        {
            return SaveDirectory;
        }
    }

    // Classe pour stocker les données chargées
    public class SaveData
    {
        public string Prenom { get; set; }
        public DateTime LastSave { get; set; }
        public List<CardSaveData> Cards { get; set; }
    }

    // Classe pour stocker les données d'une carte
    public class CardSaveData
    {
        public int Day { get; set; }
        public bool IsRevealed { get; set; }
        public string RevealedEmoji { get; set; }
        public byte BgColorR { get; set; }
        public byte BgColorG { get; set; }
        public byte BgColorB { get; set; }

        public Color GetColor()
        {
            return Color.FromRgb(BgColorR, BgColorG, BgColorB);
        }
    }
}