using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Kursova.MainForm;
using LibVLCSharp.Shared;
using YoutubeExplode;
using SpotifyExplode;
using SpotifyExplode.Search;
using System.IO;

namespace Kursova
{

    public partial class PlaylistMusic : Form
    {
        private MainForm _mainForm;
        private List<Song> favoriteSongs;
        private MediaPlayer mediaPlayer;
        private readonly SpotifyClient _spotifyClient;
        private List<Song> selectedSongs; // Доданий список обраних пісень
        private const string SelectedSongsFilePath = "C:\\Vstanovleni\\Programs\\проекти\\Kursova\\selected_songs.txt";

        public PlaylistMusic(MainForm mainForm, List<Song> favoriteSongs)
        {
            InitializeComponent();
            _mainForm = mainForm; // Ініціалізуємо _mainForm
            this.favoriteSongs = favoriteSongs;
            this.panel3.Paint += new PaintEventHandler(this.panel3_Paint);
            this.panel4.Paint += new PaintEventHandler(this.panel4_Paint);
            _spotifyClient = new SpotifyClient();
            selectedSongs = new List<Song>(); // Ініціалізуємо список обраних пісень
            LoadSelectedSongs(); // Завантажуємо обрані пісні з файлу при створенні форми
            LoadFavoriteSongs();
        }

        private void label3_Click(object sender, EventArgs e)
        {
            // Показати головну форму
            _mainForm.Show();

            // Закрити поточну форму
            this.Close();
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Color borderColor = System.Drawing.Color.Black;
            int borderWidth = 1;

            // Малюємо обрамлення
            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                Rectangle rect = new Rectangle(0, 0, panel3.ClientSize.Width - 1, panel3.ClientSize.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Color borderColor = System.Drawing.Color.Black;
            int borderWidth = 1;

            // Малюємо обрамлення
            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                Rectangle rect = new Rectangle(0, 0, panel4.ClientSize.Width - 1, panel4.ClientSize.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void LoadFavoriteSongs()
        {
            listBox1.Items.Clear();
            foreach (var song in favoriteSongs)
            {
                listBox1.Items.Add($"{song.Title} - {song.Artist}");
            }

        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string selectedSong = listBox1.SelectedItem.ToString();
                string[] songInfo = selectedSong.Split(new string[] { " - " }, StringSplitOptions.None);
                string title = songInfo[0];
                string artist = songInfo[1];

                // Знайти обрану пісню у списку favoriteSongs
                Song selectedSongObject = favoriteSongs.FirstOrDefault(song => song.Title == title && song.Artist == artist);

                if (selectedSongObject != null && !selectedSongs.Contains(selectedSongObject))
                {
                    selectedSongs.Add(selectedSongObject);
                }

            }
        }

        // Зберігаємо список обраних пісень при закритті форми
        private void PlaylistMusic_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSelectedSongs();
        }

        // Збереження списку обраних пісень
        private void SaveSelectedSongs()
        {
            // Виведення даних для перевірки
            foreach (var song in selectedSongs)
            {
                Console.WriteLine($"Title: {song.Title}, Artist: {song.Artist}");
            }

            // Збереження списку обраних пісень у файл
            using (StreamWriter writer = new StreamWriter(SelectedSongsFilePath))
            {
                foreach (Song song in selectedSongs)
                {
                    writer.WriteLine($"{song.Title} - {song.Artist}");
                }
            }
        }


        // Метод для завантаження списку обраних пісень з файлу
        private void LoadSelectedSongs()
        {
            if (File.Exists(SelectedSongsFilePath))
            {
                // Очищаємо поточний список обраних пісень перед завантаженням
                selectedSongs.Clear();

                // Зчитуємо обрані пісні з файлу
                string[] lines = File.ReadAllLines(SelectedSongsFilePath);
                foreach (string line in lines)
                {
                    string[] songInfo = line.Split(new string[] { " - " }, StringSplitOptions.None);
                    string title = songInfo[0];
                    string artist = songInfo[1];
                    Song song = favoriteSongs.FirstOrDefault(s => s.Title == title && s.Artist == artist);
                    if (song != null)
                    {
                        selectedSongs.Add(song);
                    }
                }

            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            // Очищаємо поточий список обраних пісень перед збереженням
            selectedSongs.Clear();

            // Додаємо всі пісні з listBox1 до списку обраних пісень
            foreach (var item in listBox1.Items)
            {
                string[] songInfo = item.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                string title = songInfo[0];
                string artist = songInfo[1];
                Song selectedSongObject = favoriteSongs.FirstOrDefault(song => song.Title == title && song.Artist == artist);
                if (selectedSongObject != null && !selectedSongs.Contains(selectedSongObject))
                {
                    selectedSongs.Add(selectedSongObject);
                }
            }

            // Збереження списку обраних пісень у файл
            SaveSelectedSongs();
            MessageBox.Show("Список обраних пісень збережено у файлі.");
        }

        private void loadButton_Click_1(object sender, EventArgs e)
        {
            // Очищаємо listBox1 перед завантаженням нових даних
            listBox1.Items.Clear();

            if (File.Exists(SelectedSongsFilePath))
            {
                // Зчитуємо всі рядки з файлу
                string[] lines = File.ReadAllLines(SelectedSongsFilePath);

                // Додаємо кожен рядок з файлу до listBox1
                foreach (string line in lines)
                {
                    listBox1.Items.Add(line);
                }

                MessageBox.Show("Список обраних пісень завантажено з файлу.");
            }
            else
            {
                MessageBox.Show("Файл не знайдено.");
            }
        }


    }
}