using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VinylPod
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint MOD_CTRL = 0x0002;

        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private MediaManager _mediaManager;
        private Storyboard _spinStoryboard;
        private bool _isSpinning = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupTrayIcon();

            _spinStoryboard = (Storyboard)FindResource("SpinAnimation");
            _spinStoryboard.Begin(this, true);
            _spinStoryboard.Pause(this);

            _mediaManager = new MediaManager();
            _mediaManager.OnSongChanged += MediaManager_OnSongChanged;
            _mediaManager.OnPlaybackStateChanged += MediaManager_OnPlaybackStateChanged;
            _ = _mediaManager.StartAsync();

            this.MouseLeftButtonDown += (s, e) => { if (this.WindowState == WindowState.Normal) this.DragMove(); };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(HwndHook);

            for (uint i = 1; i <= 5; i++) RegisterHotKey(hwnd, (int)(9000 + i), MOD_CTRL, 0x30 + i);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312) // WM_HOTKEY
            {
                int id = wParam.ToInt32();
                if (id >= 9001 && id <= 9005) ChangeMode(id - 9000);
            }
            return IntPtr.Zero;
        }

        private void ChangeMode(int mode)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            this.WindowState = WindowState.Normal;
            AmbientBackground.Visibility = Visibility.Collapsed;
            
            // Explicitly calling System.Windows.Media.Color
            GlassCard.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 26, 34, 50));

            if (mode == 1) { this.Width = 360; this.Height = 130; this.Topmost = true; }
            if (mode == 2) { this.Width = 540; this.Height = 195; this.Topmost = true; }
            if (mode == 3) { this.Width = 800; this.Height = 290; this.Topmost = true; }
            
            if (mode == 4 || mode == 5) 
            {
                AmbientBackground.Visibility = Visibility.Visible;
                this.WindowState = WindowState.Maximized;
                
                // FIXED: Explicitly calling System.Windows.Media.Color
                if (AlbumArtBrush.ImageSource != null) GlassCard.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 0, 0)); 

                if (mode == 4)
                {
                    this.Topmost = false;
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, 0x0001 | 0x0002);
                }
                else if (mode == 5)
                {
                    this.Topmost = true;
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002);
                }
            }
        }

        private void MediaManager_OnSongChanged(SongDetails song)
        {
            Dispatcher.Invoke(() => 
            {
                SongTitleText.Text = string.IsNullOrEmpty(song.Title) ? "Unknown Track" : song.Title;
                SongArtistText.Text = string.IsNullOrEmpty(song.Artist) ? "Unknown Artist" : song.Artist;

                if (song.AlbumArt != null)
                {
                    AlbumArtBrush.ImageSource = song.AlbumArt;
                    AmbientArtBrush.ImageSource = song.AlbumArt;

                    if (this.WindowState == WindowState.Normal)
                    {
                        // Explicitly calling System.Windows.Media.Color
                        System.Windows.Media.Color c = song.DominantColor;
                        GlassCard.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(215, c.R, c.G, c.B)); 
                    }
                }
                else
                {
                    AlbumArtBrush.ImageSource = null;
                    AmbientArtBrush.ImageSource = null;
                }
            });
        }

        private void MediaManager_OnPlaybackStateChanged(bool isPlaying)
        {
            Dispatcher.Invoke(() => 
            {
                BtnPlayPause.Content = isPlaying ? "⏸" : "▶";
                if (isPlaying && !_isSpinning) { _spinStoryboard.Resume(this); _isSpinning = true; }
                else if (!isPlaying && _isSpinning) { _spinStoryboard.Pause(this); _isSpinning = false; }
            });
        }

        private async void BtnPrev_Click(object sender, RoutedEventArgs e) => await _mediaManager.SkipPreviousAsync();
        private async void BtnPlayPause_Click(object sender, RoutedEventArgs e) => await _mediaManager.TogglePlayPauseAsync();
        private async void BtnNext_Click(object sender, RoutedEventArgs e) => await _mediaManager.SkipNextAsync();

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            
            var iconStream = System.Windows.Application.GetResourceStream(new System.Uri("pack://application:,,,/VinylPod.ico"))?.Stream;
            if (iconStream != null)
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }
            else
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Information; 
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "VinylPod";

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s, e) => 
            {
                _notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown(); 
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnClosed(EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            for (uint i = 1; i <= 5; i++) UnregisterHotKey(hwnd, (int)(9000 + i)); 
            base.OnClosed(e);
        }
    }
}