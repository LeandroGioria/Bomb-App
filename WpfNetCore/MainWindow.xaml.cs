using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.ComponentModel;
using System.Timers;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace WpfNetCore
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private static PausableTimer aTimerProcessBar;
        private static PausableTimer aTimerBomba;
        private int totalSeconds;
        private bool bPaused = false;
        public double RemainingAfterPauseGlobal = 0;

        private int _workerState;
        public int WorkerState
        {
            get { return _workerState; }
            set { 
                _workerState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WorkerState"));
            }
        }
                       
        private void BtStart_Click(object sender, RoutedEventArgs e)
        {
            if (ThereIsInvalidField())
                return;

            ResetPause();

            totalSeconds = GetSecondsTotal();
            progressBarBomba.Maximum = totalSeconds;
            RemainingAfterPauseGlobal = totalSeconds;

            WorkerState = 0;
            aTimerProcessBar = new PausableTimer(1000);
            aTimerProcessBar._isProcessBar = true;
            aTimerProcessBar.Elapsed += OnTimedEvent;
            aTimerProcessBar.AutoReset = true;
            aTimerProcessBar.Start();

            aTimerBomba = new PausableTimer(totalSeconds * 1000);
            aTimerBomba.Elapsed += OnTimedEvent2;
            aTimerBomba.AutoReset = false;
            aTimerBomba.Start();
        }
               
        private void BtReset_Click(object sender, RoutedEventArgs e)
        {
            if(aTimerProcessBar != null)
            {
                aTimerProcessBar.Stop();
                aTimerBomba.Stop();
                aTimerProcessBar = null;
                aTimerBomba = null;
                WorkerState = 0;
                bPaused = false;
                UpdateTimeView();
                ResetPause();
            }
        }

        private void BtPause_Click(object sender, RoutedEventArgs e)
        {
            if(aTimerProcessBar != null)
            {
                if (!bPaused)
                {
                    bPaused = true;
                    aTimerProcessBar.Pause();
                    aTimerBomba.Pause();
                    RemainingAfterPauseGlobal = aTimerBomba.RemainingAfterPause / 1000;
                    Dispatcher.BeginInvoke(new ThreadStart(() => {
                        btPause.Content = "Continuar";
                    }));
                }
                else
                {
                    aTimerProcessBar.Resume();
                    aTimerBomba.Resume();
                    ResetPause();
                }
            }            
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            WorkerState++;
            UpdateTimeView();
        }

        private void OnTimedEvent2(Object source, ElapsedEventArgs e)
        {
            aTimerProcessBar.Stop();
            aTimerBomba.Stop();
            ExecuteCommand();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (checkDesligar == null || checkHibernar == null)
                return;

            if (checkHibernar.IsChecked == true)
                checkDesligar.IsChecked = false;
            else
                checkDesligar.IsChecked = true;
        }

        private void CheckDesligar_Checked(object sender, RoutedEventArgs e)
        {
            if (checkDesligar == null || checkHibernar == null)
                return;

            if (checkDesligar.IsChecked == true)
                checkHibernar.IsChecked = false;
            else
                checkHibernar.IsChecked = true;
        }

        private void ExecuteCommand()
        {
            //MessageBox.Show("bum");
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.StandardInput.AutoFlush = true;
            process.StandardInput.WriteLine();

            string cmd = string.Empty;
            bool isChecked = false;
            Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                isChecked = checkDesligar.IsChecked == true;
            })).Wait();
            if (isChecked == true)
                cmd = "shutdown /s /t 0";
            else
                cmd = "shutdown /h";

            process.StandardInput.WriteLine(cmd);
        }

        private bool ThereIsInvalidField()
        {
            if (string.IsNullOrEmpty(TextHours.Text) || string.IsNullOrEmpty(TextMinutes.Text) || string.IsNullOrEmpty(TextMinutes.Text))
            {
                MessageBox.Show(("Por favor, preencha todos campos."), "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return true;
            }

            if ((float.Parse(TextHours.Text, CultureInfo.InvariantCulture.NumberFormat) == 0 &&
                float.Parse(TextMinutes.Text, CultureInfo.InvariantCulture.NumberFormat) == 0 &&
                float.Parse(TextSeconds.Text, CultureInfo.InvariantCulture.NumberFormat) == 0))
            {
                MessageBox.Show(("Por favor, insira o tempo."), "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }

            return false;
        }

        private int GetSecondsTotal()
        {
            int totalSeconds;

            totalSeconds = string.IsNullOrEmpty(TextHours.Text) ? 0 : Int32.Parse(TextHours.Text, CultureInfo.InstalledUICulture.NumberFormat) * 60 * 60;
            totalSeconds += string.IsNullOrEmpty(TextMinutes.Text) ? 0 : Int32.Parse(TextMinutes.Text, CultureInfo.InstalledUICulture.NumberFormat) * 60;
            totalSeconds += string.IsNullOrEmpty(TextSeconds.Text) ? 0 : Int32.Parse(TextSeconds.Text, CultureInfo.InstalledUICulture.NumberFormat);
           
            return totalSeconds;
        }

        private void btExplode_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Você tem certeza? Vai morrer geral!!", "Fudeu", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                return;

            ExecuteCommand();
        }

        private void TextHours_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateTimeView();
        }

        private void TextMinutes_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {         
            UpdateTimeView();
        }

        private void TextSeconds_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateTimeView();
        }

        private void UpdateTimeView()
        {
            Dispatcher.BeginInvoke(new ThreadStart(() => {
                progressBarBomba.Value = WorkerState;
                TimeSpan time = TimeSpan.FromSeconds(GetSecondsTotal() - WorkerState);
                lblTime.Content = string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
            }));
        }

        private void ResetPause()
        {
            bPaused = false;
            Dispatcher.BeginInvoke(new ThreadStart(() => {
                btPause.Content = "Pausar";
            }));
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}

    public class PausableTimer : System.Timers.Timer
    {
        public double RemainingAfterPause { get; private set; }

        private readonly Stopwatch _stopwatch;
        private readonly double _initialInterval;
        private bool _resumed;
        public bool _isProcessBar;

        public PausableTimer(double interval) : base(interval)
        {
            _initialInterval = interval;
            Elapsed += OnElapsed;
            _stopwatch = new Stopwatch();
            _isProcessBar = false;
        }

        public new void Start()
        {
            ResetStopwatch();
            base.Start();
        }

        public new void Stop()
        {
            ResetStopwatch();
            base.Stop();
        }

        private void ResetStopwatch()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        private void OnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            
            if (_resumed)
            {
                _resumed = false;
                Stop();
                Interval = _initialInterval;
                Start();
            }

            ResetStopwatch();
        }

        public void Pause()
        {
            double sec = _stopwatch.Elapsed.TotalSeconds;

            RemainingAfterPause = Interval - _stopwatch.Elapsed.TotalMilliseconds;
            Stop();
            _stopwatch.Stop();

        }

        public void Resume()
        {
            _resumed = true;
            if(RemainingAfterPause != 0)
             Interval = RemainingAfterPause;
            RemainingAfterPause = 0;

            Start();
        }
            
    }
}
