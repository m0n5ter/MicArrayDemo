using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TwoMicTest;

public class MainViewModel : ObservableObject
{
    private MMDeviceEnumerator _enumerator;
    private string? _playbackDevice;
    private RecorderViewModel? _recorder2;
    private RecorderViewModel? _recorder1;

    public ObservableCollection<string> PlaybackDevices { get; } = new();
    
    public ObservableCollection<string> RecordDevices { get; } = new();

    public MainViewModel()
    {
        TestCommand = new AsyncRelayCommand(Test, () => Error == null);
    }

    public string? Error => PlaybackDevice == null ? "Playback Device is required"
        : Recorder1?.Device == null || Recorder2?.Device == null ? "Two recorder devices must be selected"
        : Recorder1?.Device == Recorder2?.Device ? "Select two different recorder devices" : null;

    public RecorderViewModel? Recorder1
    {
        get => _recorder1;
        private set
        {
            var oldValue = _recorder1;

            if (SetProperty(ref _recorder1, value))
            {
                if (oldValue != null) oldValue.PropertyChanged -= OnRecorderPropertyChanged;
                if (value != null) value.PropertyChanged += OnRecorderPropertyChanged;
                InvalidateTestCommand();
            }
        }
    }

    private void OnRecorderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvalidateTestCommand();
    }

    private void InvalidateTestCommand()
    {
        OnPropertyChanged(nameof(Error));
        TestCommand.NotifyCanExecuteChanged();
    }

    public RecorderViewModel? Recorder2
    {
        get => _recorder2;
        private set
        {
            var oldValue = _recorder2;

            if (SetProperty(ref _recorder2, value))
            {
                if (oldValue != null) oldValue.PropertyChanged -= OnRecorderPropertyChanged;
                if (value != null) value.PropertyChanged += OnRecorderPropertyChanged;
                InvalidateTestCommand();
            }
        }
    }

    public async Task Initialize()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _enumerator = new MMDeviceEnumerator();
            
            PlaybackDevices.Clear();
            RecordDevices.Clear();

            _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).OrderBy(_ => _.FriendlyName).ToList().ForEach(d => PlaybackDevices.Add(d.FriendlyName));
            _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).OrderBy(_ => _.FriendlyName).ToList().ForEach(d => RecordDevices.Add(d.FriendlyName));

            PlaybackDevice = PlaybackDevices.FirstOrDefault();

            Recorder1 = new RecorderViewModel(this){Device = RecordDevices.FirstOrDefault()};
            Recorder2 = new RecorderViewModel(this) {Device = RecordDevices.Skip(1).FirstOrDefault()};
        });
    }

    public string? PlaybackDevice
    {
        get => _playbackDevice;
        set => SetProperty(ref _playbackDevice, value);
    }

    public async Task Test()
    {
        await Task.Run(async () =>
        {
            var playbackEnded = new ManualResetEventSlim(false);

            var playbackDevice = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => d.FriendlyName == PlaybackDevice);
            Recorder1?.Warmup(_enumerator);
            Recorder2?.Warmup(_enumerator);

            Recorder1?.Start();
            Recorder2?.Start();

            var playMs = new MemoryStream(File.ReadAllBytes("test.wav"));
            WaveStream waveStream = new WaveFileReader(playMs);
            var totalTime = waveStream.TotalTime;
            var player = new WasapiOut(playbackDevice, AudioClientShareMode.Shared, true, 0);
            player.Init(waveStream);
            player.PlaybackStopped += (_, _) => playbackEnded.Set();

            await Task.Delay(100);
            player.Play();
            playbackEnded.WaitHandle.WaitOne();
            await Task.Delay(100);

            Recorder1?.Stop();
            Recorder2?.Stop();
        });

    }

    public AsyncRelayCommand TestCommand { get; }

}