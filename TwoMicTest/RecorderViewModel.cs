using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace TwoMicTest;

public class RecorderViewModel: ObservableObject
{
    private static readonly WaveFormat _format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
    private string? _device;
    private MMDevice? _recorderDevice;
    private WasapiCapture? _capture;
    private MemoryStream? _memoryStream;
    private List<float>? _samples;
    private readonly List<float> _currentSamples = new();
    private WaveFileWriter? _waveWriter;
    private List<Range>? _ranges;
    private PointCollection? _waveform;
    private float _minLevel;
    private float _maxLevel;

    public RecorderViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public MainViewModel MainViewModel { get; }

    public PointCollection? Waveform
    {
        get => _waveform;
        private set => SetProperty(ref _waveform, value);
    }

    public List<float>? Samples
    {
        get => _samples;
        set => SetProperty(ref _samples, value);
    }

    public string? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public Task Warmup(MMDeviceEnumerator enumerator) => Task.Run(() =>
    {
        _recorderDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(d => d.FriendlyName == Device) ??
                          throw new Exception($"Device was disconnected: {Device}");

        _capture = new WasapiCapture(_recorderDevice) {WaveFormat = _format};
        _memoryStream = new MemoryStream();
        _waveWriter = new WaveFileWriter(_memoryStream, _format);
        Samples = null;
        Ranges = null;
        Waveform = null;
        MaxLevel = 0;
        MinLevel = 0;

        _currentSamples.Clear();

        _capture.DataAvailable += (_, a) =>
        {
            var i = 0;

            while (i <= a.BytesRecorded - 4)
            {
                var f = BitConverter.ToSingle(a.Buffer, i);
                i += 4;
                _currentSamples.Add(f);
            }

            _waveWriter.Write(a.Buffer, 0, a.BytesRecorded);
        };
    });

    public void Start()
    {
        _capture?.StartRecording();
    }

    public Task Stop() => Task.Run(async () =>
    {
        _capture?.StopRecording();

        Samples = _currentSamples;
        MaxLevel = _currentSamples?.Max() ?? 0;
        MinLevel = _currentSamples?.Min() ?? 0;

        Ranges = GetRanges(_currentSamples, 24);

        var waveRanges = GetRanges(_currentSamples, 200);
        
        var points = waveRanges?.Select((range, i) => new Point(i, range.Min + 1))
            .Concat(waveRanges.AsEnumerable().Reverse().Select((range, i) => new Point(waveRanges.Count - i - 1, range.Max + 1)));

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Waveform = points == null ? null : new PointCollection(points);
        });

    });

    private List<Range>? GetRanges(IReadOnlyCollection<float>? samples, int n) =>
        samples?.Select((f, i) => new {group = Math.Clamp((int) Math.Round((double) i / samples.Count * n - 0.5), 0, n - 1), f})
            .GroupBy(g => g.group)
            .Select(g => g.Select(i => i.f).ToArray())
            .Select(g => new Range(g))
            .ToList();

    public float MinLevel
    {
        get => _minLevel;
        set => SetProperty(ref _minLevel, value);
    }

    public float MaxLevel
    {
        get => _maxLevel;
        set => SetProperty(ref _maxLevel, value);
    }

    public List<Range>? Ranges
    {
        get => _ranges;
        set => SetProperty(ref _ranges, value);
    }
}