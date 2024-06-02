using System;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SpotifyExplode;
using YoutubeExplode;
using YoutubeExplode.Common;
using static Kursova.MainForm;
using System.Net.Http;

namespace Kursova
{
    public class SongStreamingService
    {
        private readonly YoutubeClient _youTubeClient;
        private readonly SpotifyClient _spotifyClient;
        private readonly MainForm _mainForm;

        public SongStreamingService(MainForm mainForm)
        {
            _mainForm = mainForm;
            var httpClient = new HttpClient();
            _youTubeClient = new YoutubeClient(httpClient);

            // Перевірка, чи метод SetApiKey доступний
            if (_youTubeClient.GetType().GetMethod("SetApiKey") != null)
            {
                // Виклик методу SetApiKey
                _youTubeClient.GetType().GetMethod("SetApiKey").Invoke(_youTubeClient, new object[] { "AIzaSyDQ5UBztTg-WpESpeJfsFThx80xFG5xVU4" });
            }
            else
            {
                Console.WriteLine("Метод SetApiKey не підтримується в даній версії YoutubeClient.");
            }

            _spotifyClient = new SpotifyClient();
        }



        public async Task<string> GetYouTubeVideoIdAsync(string trackTitle)
        {
            try
            {
                var searchResults = await _youTubeClient.Search.GetVideosAsync(trackTitle);
                var firstVideo = searchResults.FirstOrDefault();
                return firstVideo?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час отримання ID відео на YouTube: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetYouTubeAudioStreamUrlAsync(string trackId)
        {
            try
            {
                var track = await _spotifyClient.Tracks.GetAsync(trackId);
                var searchResults = await _youTubeClient.Search.GetVideosAsync($"{track.Artists.FirstOrDefault()?.Name} - {track.Title}");
                var firstVideo = searchResults.FirstOrDefault();
                if (firstVideo != null)
                {
                    var streamInfoSet = await _youTubeClient.Videos.Streams.GetManifestAsync(firstVideo.Id);
                    var audioStreamInfo = streamInfoSet.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
                    return audioStreamInfo?.Url;
                }
                else
                {
                    Console.WriteLine("Відео на YouTube для даного треку не знайдено.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час отримання аудіопотоку з YouTube: {ex.Message}");
                return null;
            }
        }

        public async Task<Song> GetSongInfoAsync(string trackId)
        {
            try
            {
                var track = await _spotifyClient.Tracks.GetAsync(trackId);
                var youtubeVideoId = await GetYouTubeVideoIdAsync($"{track.Artists.FirstOrDefault()?.Name} - {track.Title}");
                if (youtubeVideoId != null)
                {
                    var streamUrl = await GetYouTubeAudioStreamUrlAsync(youtubeVideoId);
                    if (streamUrl != null)
                    {
                        return new Song
                        {
                            Id = track.Id,
                            Title = track.Title,
                            Artist = track.Artists.FirstOrDefault()?.Name,
                            Album = track.Album.Name,
                            DurationMs = (int)track.DurationMs,
                            StreamUrl = streamUrl
                        };
                    }
                }

                Console.WriteLine("Потік звуку для даного треку не знайдено.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час отримання інформації про пісню: {ex.Message}");
                return null;
            }
        }

        public async Task PlaySongAsync(Song song)
        {
            try
            {
                Core.Initialize();
                using (var libVLC = new LibVLC())
                {
                    using (var media = new Media(libVLC, song.StreamUrl, FromType.FromLocation))
                    {
                        using (var mediaPlayer = new MediaPlayer(media))
                        {
                            mediaPlayer.Play();
                            await Task.Delay(song.DurationMs);
                            mediaPlayer.Stop();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час відтворення пісні: {ex.Message}");
            }
        }

    }
}
