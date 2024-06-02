using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpotifyExplode;
using SpotifyExplode.Search;
using SpotifyExplode.Albums;
using SpotifyExplode.Artists;
using SpotifyExplode.Tracks;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Media;
using System.Windows.Controls;
using YoutubeExplode;



namespace Kursova
{
    public partial class MainForm : Form
    {
        private readonly SpotifyClient _spotifyClient;
        private Song _currentTrack;
        private Timer _trackTimer;
        private MediaPlayer mediaPlayer;
        private int previousVolume = 100; // Оголошення змінної для зберігання попереднього рівня гучності
        private List<Song> favoriteSongs = new List<Song>(); // Список для збереження обраних пісень
        private MainForm _mainForm;
        private long currentPosition;
        private long duration;

        public int UserId { get; set; }



        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);

            // Ініціалізація таймера для оновлення слайдера музики
            _trackTimer = new Timer();
            _trackTimer.Interval = 1000; // Оновлення кожну секунду
            _trackTimer.Tick += TrackTimer_Tick;
            _spotifyClient = new SpotifyClient();
            SongStreamingService musicPlayer = new SongStreamingService(this); // Передача поточного екземпляру MainForm

            UserId = 0;
            
        }

        public MainForm(int userId)
        {
            UserId = userId;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Приховати button1 та textBox1 при завантаженні форми
            pictureBox4.Hide();
            textBox1.Hide();

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Переконайтеся, що користувач справді хоче закрити програму
                DialogResult result = MessageBox.Show("Ви впевнені, що хочете закрити програму?", "Підтвердження закриття", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Зупиняємо відтворення музики, якщо воно запущене і mediaPlayer не null
                    if (mediaPlayer != null && mediaPlayer.IsPlaying)
                    {
                        mediaPlayer.Stop();
                    }

                    // Якщо користувач підтвердив закриття програми, закриваємо програму
                    Application.Exit();
                }
                else
                {
                    // Якщо користувач відмінив закриття, скасовуємо закриття форми
                    e.Cancel = true;
                }
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {
            ShowMainPanel();
        }

        private void ShowMainPanel()
        {
            // Приховати всі інші панелі та показати головну панель
            mainPanel.BringToFront();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            pictureBox4.Show();
            textBox1.Show();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Приховати pictureBox4 та textBox1 при завантаженні форми
            pictureBox4.Hide();
            textBox1.Hide();
        }

        

        private async void textBox1_TextChanged(object sender, EventArgs e)
        {
            string query = textBox1.Text;
            if (string.IsNullOrWhiteSpace(query))
                return;

            try
            {
                // Виклик GetResultsAsync з правильним типом пошуку
                var searchResults = await _spotifyClient.Search.GetResultsAsync(query);

                // Оновлення результатів пошуку
                UpdateSearchResults(searchResults);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private void UpdateSearchResults(IEnumerable<ISearchResult> results)
        {
            listBox1.Items.Clear();

            foreach (var result in results)
            {
                // Перевірка типу результату та додавання відповідної інформації до listBox1
                switch (result)
                {
                    case TrackSearchResult track:
                        listBox1.Items.Add(new ListBoxItem { Text = $"Трек: {track.Title}, Виконавець: {track.Artists.FirstOrDefault()?.Name ?? "Невідомий виконавець"}", Track = track });
                        break;
                    case AlbumSearchResult album:
                        listBox1.Items.Add(new ListBoxItem { Text = $"Альбом: {album.Name}, Виконавець: {string.Join(", ", album.Artists)}", Album = album });
                        break;
                    case ArtistSearchResult artist:
                        listBox1.Items.Add(new ListBoxItem { Text = $"Виконавець: {artist.Name}", Artist = artist });
                        break;
                    case PlaylistSearchResult playlist:
                        listBox1.Items.Add(new ListBoxItem { Text = $"Плейлист: {playlist.Name}", Playlist = playlist });
                        break;
                    default:
                        break;
                }
            }

            listBox1.DisplayMember = "Text";
        }

        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem is ListBoxItem selectedItem)
            {
                if (selectedItem.Track != null)
                {
                    var track = await _spotifyClient.Tracks.GetAsync(selectedItem.Track.Id);

                    SongStreamingService songService = new SongStreamingService(this);

                    _currentTrack = new Song
                    {
                        Id = track.Id,
                        Title = track.Title,
                        Artist = track.Artists.FirstOrDefault()?.Name,
                        Album = track.Album.Name,
                        DurationMs = (int)track.DurationMs,
                        StreamUrl = await GetTrackStreamUrlAsync(track.Id)
                    };
                    PlayTrack(_currentTrack);

                    // Задати текст мітки nowPlayingLabel
                    nowPlayingLabel.Text = _currentTrack.Title;
                    nowPlayingLabel2.Text = _currentTrack.Artist;

                    // Оновити значення та максимальне значення musicSlider
                    musicSlider.Maximum = (int)_currentTrack.DurationMs;
                    musicSlider.Value = 0;
                }
            }
        }

        private void PlayAudio(string streamUrl)
        {
            try
            {
                LibVLCSharp.Shared.Core.Initialize();
                var libVLC = new LibVLCSharp.Shared.LibVLC();
                mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVLC);
                mediaPlayer.Play(new LibVLCSharp.Shared.Media(libVLC, streamUrl, LibVLCSharp.Shared.FromType.FromLocation));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка під час відтворення аудіо: {ex.Message}");
            }
        }



        private void PlayTrack(Song track)
        {
            // Зупинити поточний відтворення, якщо він вже існує
            if (mediaPlayer != null)
            {
                mediaPlayer.Stop();
                mediaPlayer.Dispose();
            }

            if (track != null && !string.IsNullOrEmpty(track.StreamUrl))
            {
                try
                {
                    LibVLCSharp.Shared.Core.Initialize();
                    var libVLC = new LibVLCSharp.Shared.LibVLC();
                    mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVLC);
                    mediaPlayer.Play(new LibVLCSharp.Shared.Media(libVLC, track.StreamUrl, LibVLCSharp.Shared.FromType.FromLocation));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Сталася помилка під час відтворення аудіо: {ex.Message}");
                }
            }
        }



        private async Task<string> GetTrackStreamUrlAsync(string trackId)
        {
            try
            {
                // Отримання YouTube ID треку
                var youtubeId = await _spotifyClient.Tracks.GetYoutubeIdAsync(trackId);

                if (!string.IsNullOrEmpty(youtubeId))
                {
                    // Створення нового клієнта YouTube
                    var youTubeClient = new YoutubeClient();

                    // Отримання маніфеста потоків відео
                    var streamManifest = await youTubeClient.Videos.Streams.GetManifestAsync(youtubeId);

                    // Знайти потік з аудіо
                    var audioStreamInfo = streamManifest.GetAudioStreams().FirstOrDefault();

                    if (audioStreamInfo != null)
                    {
                        return audioStreamInfo.Url;
                    }
                    else
                    {
                        MessageBox.Show("Не вдалося знайти аудіо потік для цього треку на YouTube.");
                        return null;
                    }
                }
                else
                {
                    MessageBox.Show("Не вдалося отримати YouTube ID для цього треку.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
                return null;
            }
        }




        private void TrackTimer_Tick(object sender, EventArgs e)
        {
            // Оновлення даних кожну секунду

            // Оновлюємо значення поточної позиції програвання пісні і тривалості пісні
            currentPosition = mediaPlayer.Time;
            duration = mediaPlayer.Length;

            // Оновлюємо значення міток часу
            timeLabel1.Text = TimeSpan.FromMilliseconds(currentPosition).ToString(@"mm\:ss"); // Поточний час
            timeLabel2.Text = TimeSpan.FromMilliseconds(duration).ToString(@"mm\:ss"); // Загальна тривалість

            // Оновлюємо час прогресу пісні на слайдері
            if (duration != 0)
            {
                musicSlider.Value = (int)((currentPosition / (double)duration) * musicSlider.Maximum); // Прив'язуємо ползунок до поточної позиції програвання пісні
            }
            else
            {
                // Обробка випадку, коли тривалість дорівнює нулю
                musicSlider.Value = 0; // або будь-яке інше значення за вашим вибором
            }

            // Перевіряємо, чи прогрес дорівнює тривалості, тобто чи пісня досягла кінця
            if (currentPosition >= duration)
            {
                // Виконуємо дії при завершенні пісні
                mediaPlayer.Stop(); // Зупиняємо програвання пісні
                _trackTimer.Stop(); // Зупиняємо таймер
            }
        }



        private void mediaPlayer_Playing(object sender, EventArgs e)
        {
            // Початок програвання нової пісні

            // Зупиняємо таймер, якщо він вже працює
            _trackTimer.Stop();

            // Починаємо таймер
            _trackTimer.Start();
        }


        private void musicSlider_ValueChanged(object sender, EventArgs e)
        {
            // Починаємо програвання пісні, стартуємо таймер
            _trackTimer.Start();
        }

        private void mediaPlayer_EndReached(object sender, EventArgs e)
        {
            // Зупиняємо таймер при завершенні програвання пісні
            _trackTimer.Stop();
        }




        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Color borderColor = System.Drawing.Color.Black; // Задати бажаний колір
            int borderWidth = 1; // Задати бажану товщину обведення

            // Малювання обведення
            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                Rectangle rect = new Rectangle(0, 0, panel3.ClientSize.Width - 1, panel3.ClientSize.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {
            System.Drawing.Color borderColor = System.Drawing.Color.Black; // Задати бажаний колір
            int borderWidth = 1; //
                                 // Малювання обведення
            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                Rectangle rect = new Rectangle(0, 0, panel4.ClientSize.Width - 1, panel4.ClientSize.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            mediaPlayer.Play();

            btnPlay.Hide();
            btnPause.Show();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            mediaPlayer.Pause();
            btnPause.Hide();
            btnPlay.Show();
        }



        private void btnVolumemedium_Click(object sender, EventArgs e)
        {
            previousVolume = mediaPlayer.Volume;
            mediaPlayer.Volume = 0;
            btnVolumemedium.Hide();
            btnVolumemute.Show();
        }

        private void btnVolumemute_Click(object sender, EventArgs e)
        {
            mediaPlayer.Volume = previousVolume;
            
            btnVolumemute.Hide();
            btnVolumemedium.Show();
        }

        private void TrackBar_Scroll(object sender, EventArgs e)
        {
            if (mediaPlayer != null) // Перевірка на null, щоб уникнути виклику методів на null об'єкті
            {
                // Отримати значення гучності з TrackBar
                System.Windows.Forms.TrackBar trackBar = (System.Windows.Forms.TrackBar)sender; // Приведення sender до типу TrackBar
                int volumeValue = trackBar.Value;

                // Встановити гучність відтворення
                mediaPlayer.Volume = volumeValue;
            }
        }

      

        private class ListBoxItem
        {
            public string Text { get; set; }
            public TrackSearchResult Track { get; set; }
            public AlbumSearchResult Album { get; set; }
            public ArtistSearchResult Artist { get; set; }
            public PlaylistSearchResult Playlist { get; set; }
        }

        // Ваші методи для завантаження метаданих треків, альбомів, виконавців і плейлистів
        private async void GetPlaylistMetadata(string playlistUrl)
        {
            try
            {
                // Отримання метаданих списку відтворення за URL
                var playlist = await _spotifyClient.Playlists.GetAsync(playlistUrl);
                MessageBox.Show($"Назва списку відтворення: {playlist.Name}\nКількість треків: {playlist.Tracks.Count}\nКількість підписників: {playlist.Followers.Total}");

                // Отримання треків зі списку відтворення
                var tracks = await _spotifyClient.Playlists.GetTracksAsync(playlistUrl);

                // Виведення інформації про перші 5 треків
                MessageBox.Show("Перші 5 треків у списку відтворення:");
                foreach (var track in tracks.Take(5))
                {
                    MessageBox.Show($"Трек: {track.Title}\nВиконавець: {track.Artists.FirstOrDefault()?.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void GetAlbumMetadata(string albumUrl)
        {
            try
            {
                // Отримання метаданих альбому за URL
                var album = await _spotifyClient.Albums.GetAsync(albumUrl);
                MessageBox.Show($"Назва альбому: {album.Name}\nВиконавець: {string.Join(", ", album.Artists)}\nКількість треків: {album.Tracks.Count}");

                // Отримання треків з альбому
                var tracks = album.Tracks;

                // Виведення інформації про перші 5 треків
                MessageBox.Show("Перші 5 треків у цьому альбомі:");
                foreach (var track in tracks.Take(5))
                {
                    MessageBox.Show($"Трек: {track.Title}\nВиконавець: {track.Artists.FirstOrDefault()?.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void GetAlbumTracks(string albumUrl)
        {
            try
            {
                // Отримання всіх треків у альбомі за URL
                var tracks = await _spotifyClient.Albums.GetAllTracksAsync(albumUrl);
                MessageBox.Show($"Знайдено {tracks.Count} треків у цьому альбомі.");

                // Виведення інформації про перші 20 треків
                MessageBox.Show("Перші 20 треків у цьому альбомі:");
                foreach (var track in tracks.Take(20))
                {
                    MessageBox.Show($"Трек: {track.Title}\nВиконавець: {track.Artists.FirstOrDefault()?.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void GetArtistInfo(string artistUrl)
        {
            try
            {
                // Отримання метаданих виконавця за URL
                var artist = await _spotifyClient.Artists.GetAsync(artistUrl);
                MessageBox.Show($"Назва виконавця: {artist.Name}");

                // Отримання альбомів виконавця
                var albums = await _spotifyClient.Artists.GetAlbumsAsync(artistUrl);

                MessageBox.Show($"Кількість альбомів: {albums.Count}");

                // Виведення інформації про перші 5 альбомів
                MessageBox.Show("Перші 5 альбомів виконавця:");
                foreach (var album in albums.Take(5))
                {
                    MessageBox.Show($"Назва альбому: {album.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void GetArtistTracks(string artistUrl)
        {
            try
            {
                // Отримання композицій, включених до виконавця за URL
                var tracks = await _spotifyClient.Artists.GetAllAlbumsAsync(artistUrl);

                // Перевірка, чи є хоча б одна композиція у списку
                if (tracks.Any())
                {
                    // Якщо список не порожній, показати кількість інформацію про перші 5 композицій
                    MessageBox.Show($"Кількість композицій: {tracks.Count()}");

                    MessageBox.Show("Перші 5 композицій виконавця:");
                    foreach (var track in tracks.Take(5))
                    {
                        MessageBox.Show($"Назва треку: {track.Name}");
                    }
                }
                else
                {
                    MessageBox.Show("Список композицій виконавця порожній.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void DownloadTrack(string trackUrl)
        {
            try
            {
                // Отримання URL-адреси завантаження композиції за її Spotify URL
                var downloadUrl = await _spotifyClient.Tracks.GetDownloadUrlAsync(trackUrl);

                // Перевірка, чи вдалося отримати URL-адресу завантаження
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    // Завантаження можливе, використовуйте отриману URL-адресу для початку завантаження
                    // Наприклад, ви можете відкрити браузер і передати цю URL-адресу, щоб завантажити файл
                    // Або ви можете використовувати свою власну логіку завантаження
                    MessageBox.Show($"URL-адреса завантаження композиції: {downloadUrl}");
                }
                else
                {
                    MessageBox.Show("Не вдалося отримати URL-адресу завантаження композиції.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }

        private async void GetYoutubeId(string trackUrl)
        {
            try
            {
                // Отримання YouTube ID з треку за його Spotify URL
                var youtubeId = await _spotifyClient.Tracks.GetYoutubeIdAsync(trackUrl);

                // Перевірка, чи вдалося отримати YouTube ID
                if (!string.IsNullOrEmpty(youtubeId))
                {
                    // Формування URL-адреси YouTube за отриманим YouTube ID
                    var youtubeUrl = $"https://youtube.com/watch?v={youtubeId}";

                    // Виведення URL-адреси YouTube
                    MessageBox.Show($"YouTube URL: {youtubeUrl}");
                }
                else
                {
                    MessageBox.Show("Не вдалося отримати YouTube ID з треку.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}");
            }
        }




        public class Song
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public int DurationMs { get; set; }
            public string StreamUrl { get; set; }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Перемикання на наступний трек
            PlayNextTrack();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Перемикання на попередній трек
            PlayPreviousTrack();
        }



        private void PlayNextTrack()
        {
            // Отримання індексу вибраного елемента в списку listBox1
            int currentIndex = listBox1.SelectedIndex;

            // Перевірка, чи індекс не виходить за межі діапазону та чи є наступний елемент
            if (currentIndex >= 0 && currentIndex < listBox1.Items.Count - 1)
            {
                // Встановлюємо вибраний елемент в listBox1
                listBox1.SelectedIndex = currentIndex + 1;
            }
        }

        private void PlayPreviousTrack()
        {
            // Отримання індексу вибраного елемента в списку listBox1
            int currentIndex = listBox1.SelectedIndex;

            // Перевірка, чи індекс не виходить за межі діапазону та чи є попередній елемент
            if (currentIndex > 0 && currentIndex < listBox1.Items.Count)
            {
                // Встановлюємо вибраний елемент в listBox1
                listBox1.SelectedIndex = currentIndex - 1;
            }
        }

        private void pictureBoxheart_Click(object sender, EventArgs e)
        {
            if (_currentTrack != null)
            {
                // Додати обрану пісню до списку улюблених
                favoriteSongs.Add(_currentTrack);

                // Оновити UI або показати повідомлення
                MessageBox.Show($"Пісня '{_currentTrack.Title}' додана до улюблених.");

                // Створити новий Label для відображення інформації про трек
                System.Windows.Forms.Label trackLabel = new System.Windows.Forms.Label();
                trackLabel.AutoSize = true;
                trackLabel.Text = $"{_currentTrack.Title} - {_currentTrack.Artist}";

            }
            
        }

       

        private void OpenPlaylistMusicForm()
        {
            // Передаємо список favoriteSongs до конструктора PlaylistMusic
            PlaylistMusic playlistMusicForm = new PlaylistMusic(this, favoriteSongs);
            playlistMusicForm.Show();
        }

        private void label2_Click_1(object sender, EventArgs e)
        {
            // Створюємо новий екземпляр форми PlaylistMusic і передаємо посилання на поточний екземпляр MainForm та список улюблених пісень
            PlaylistMusic playlistMusicForm = new PlaylistMusic(this, this.GetFavoriteSongs());

            // Приховуємо головну форму
            this.Hide();

            // Відкриваємо нову форму
            playlistMusicForm.Show();
        }
        public List<Song> GetFavoriteSongs()
        {
            // Повертаємо список обраних пісень
            return favoriteSongs;
        }
    }
}
