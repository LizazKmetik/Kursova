using LibVLCSharp.Shared;
using SpotifyExplode;
using SpotifyExplode.Tracks;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;



namespace Kursova
{
    public class MusicPlayerService 
    {
        public LibVLC LibVLC { get; private set; } = new LibVLC();
        public MediaPlayer MediaPlayer { get; private set; }
        public YoutubeClient YoutubeClient { get; private set; } = new YoutubeClient();
        public SpotifyClient SpotifyClient { get; private set; } = new SpotifyClient();
        public CancellationTokenSource CancellationTokenSourcePlay { get; private set; } = new CancellationTokenSource();
        public bool IsPaused { get; private set; }
        public bool IsRepeating { get; private set; }

        public event EventHandler ExceptionThrown;

        public MusicPlayerService()
        {
            MediaPlayer = new MediaPlayer(LibVLC);
        }

        public async Task OnPlaySong(string artist, string title, string songId)
        {
            try
            {
                CreateMusicSaveFolderIfNeeded();

                CancellationTokenSourcePlay?.Cancel();
                CancellationTokenSourcePlay = new CancellationTokenSource();

                CancelPlayingMusic();

                bool fileExists = File.Exists($"SavedMusic\\{artist} - {title}.mp3");

                CancelPlayingMusic();

                Media media;
                if (fileExists)
                {
                    media = new Media(LibVLC, $"SavedMusic\\{artist} - {title}.mp3", FromType.FromPath);
                }
                else
                {
                    string youtubeID = await SpotifyClient.Tracks.GetYoutubeIdAsync(TrackId.Parse(songId), CancellationTokenSourcePlay.Token);
                    StreamManifest streamInfo = await YoutubeClient.Videos.Streams.GetManifestAsync($"https://youtube.com/watch?v={youtubeID}", CancellationTokenSourcePlay.Token);
                    CancelPlayingMusic();
                    IStreamInfo content = streamInfo.GetAudioOnlyStreams().GetWithHighestBitrate();
                    media = new Media(LibVLC, content.Url, FromType.FromLocation);
                }
                media.AddOption(":no-video");

                CancelPlayingMusic();

                MediaPlayer.Media = media;
                MediaPlayer.Play();
                if (IsPaused) MediaPlayer.SetPause(true);

                CancelPlayingMusic();
            }
            catch (TaskCanceledException ex)
            {
                Trace.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                ExceptionThrown?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CancelPlayingMusic()
        {
            if (CancellationTokenSourcePlay.IsCancellationRequested)
            {
                MediaPlayer.Stop();
                MediaPlayer.Media?.Dispose();
            }
        }

        public void OnPauseSong()
        {
            IsPaused = !IsPaused;
            MediaPlayer.SetPause(IsPaused);
        }

        public void OnRepeatingSong() => IsRepeating = !IsRepeating;

        private static void CreateMusicSaveFolderIfNeeded()
        {
            if (!Directory.Exists("SavedMusic"))
            {
                Directory.CreateDirectory("SavedMusic");
            }
        }
    }
}
