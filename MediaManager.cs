using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace VinylPod
{
    public class SongDetails
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public BitmapImage? AlbumArt { get; set; }
        public System.Windows.Media.Color DominantColor { get; set; } 
    }

    public class MediaManager
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private GlobalSystemMediaTransportControlsSession? _currentSession;

        public event Action<SongDetails>? OnSongChanged;
        public event Action<bool>? OnPlaybackStateChanged;

        public async Task StartAsync()
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                TryBindToCurrentSession();
            }
        }

        public async Task TogglePlayPauseAsync()
        {
            if (_currentSession != null) 
            {
                try 
                {
                    await _currentSession.TryTogglePlayPauseAsync();
                    await Task.Delay(150); 
                    UpdatePlaybackState(); 
                } catch { }
            }
        }

        public async Task SkipNextAsync()
        {
            if (_currentSession != null) try { await _currentSession.TrySkipNextAsync(); } catch { }
        }

        public async Task SkipPreviousAsync()
        {
            if (_currentSession != null) try { await _currentSession.TrySkipPreviousAsync(); } catch { }
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            TryBindToCurrentSession();
        }

        private void TryBindToCurrentSession()
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
            }

            _currentSession = _sessionManager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                
                _ = UpdateSongDetailsAsync();
                UpdatePlaybackState();
            }
        }

        private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => UpdatePlaybackState();

        private void UpdatePlaybackState()
        {
            if (_currentSession == null) return;
            try
            {
                var playbackInfo = _currentSession.GetPlaybackInfo();
                bool isPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                OnPlaybackStateChanged?.Invoke(isPlaying);
            }
            catch { }
        }

        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => _ = UpdateSongDetailsAsync();

        private async Task UpdateSongDetailsAsync()
        {
            if (_currentSession == null) return;
            try
            {
                var properties = await _currentSession.TryGetMediaPropertiesAsync();
                if (properties != null)
                {
                    BitmapImage? albumArt = null;
                    System.Windows.Media.Color tintColor = System.Windows.Media.Color.FromRgb(26, 34, 50); 

                    if (properties.Thumbnail != null)
                    {
                        albumArt = await GetThumbnailAsync(properties.Thumbnail);
                        if (albumArt != null) tintColor = GetAverageColor(albumArt);
                    }

                    OnSongChanged?.Invoke(new SongDetails 
                    { 
                        Title = properties.Title, 
                        Artist = properties.Artist, 
                        AlbumArt = albumArt,
                        DominantColor = tintColor
                    });
                }
            }
            catch { }
        }

        private async Task<BitmapImage?> GetThumbnailAsync(IRandomAccessStreamReference thumbnail)
        {
            if (thumbnail == null) return null;
            using var stream = await thumbnail.OpenReadAsync();
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream.AsStream();
            bitmap.EndInit();
            bitmap.Freeze(); 
            return bitmap;
        }

        private System.Windows.Media.Color GetAverageColor(BitmapSource bitmap)
        {
            try
            {
                var formatConverted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int width = formatConverted.PixelWidth;
                int height = formatConverted.PixelHeight;
                byte[] pixels = new byte[height * width * 4];
                formatConverted.CopyPixels(pixels, width * 4, 0);

                long r = 0, g = 0, b = 0;
                int step = 4 * 10; 
                int count = 0;
                
                for (int i = 0; i < pixels.Length; i += step)
                {
                    b += pixels[i]; g += pixels[i + 1]; r += pixels[i + 2];
                    count++;
                }

                if (count == 0) return System.Windows.Media.Colors.Black;
                return System.Windows.Media.Color.FromRgb((byte)(r / count), (byte)(g / count), (byte)(b / count));
            }
            catch { return System.Windows.Media.Color.FromRgb(26, 34, 50); }
        }
    }
}