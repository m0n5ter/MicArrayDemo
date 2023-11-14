using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TwoMicTest;

public class RecorderViewModel: ObservableObject
{
    private static readonly WaveFormat _format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
    private string? _device;
    private MMDevice? _recorderDevice;
    private WasapiCapture? _capture;
    private MemoryStream? _memoryStream;
    private List<float> _samples = new();
    private WaveFileWriter? _waveWriter;
    private List<Range> _ranges;
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

    public string? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public void Warmup(MMDeviceEnumerator enumerator)
    {
        _recorderDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(d => d.FriendlyName == Device) ??
                          throw new Exception($"Device was disconnected: {Device}");

        _capture = new WasapiCapture(_recorderDevice){WaveFormat = _format};
        _memoryStream = new MemoryStream();
        _waveWriter = new WaveFileWriter(_memoryStream, _format);
        _samples.Clear();
        Waveform = null;

        _capture.DataAvailable += (s, a) =>
        {
            var i = 0;

            while (i <= a.BytesRecorded - 4)
            {
                var f = BitConverter.ToSingle(a.Buffer, i);
                i += 4;
                _samples.Add(f);
            }

            _waveWriter.Write(a.Buffer, 0, a.BytesRecorded);
        };
    }

    public void Start()
    {
        _capture?.StartRecording();
    }

    public void Stop()
    {
        _capture?.StopRecording();

        Ranges = _samples.Select((f, i) => new {group = Math.Clamp((int) Math.Round((double) i / _samples.Count * 100), 0, 99), f})
            .GroupBy(g => g.group)
            .Select(g => g.Select(_ => _.f).ToArray())
            .Select(g => new Range(g.Min(), g.Max()))
            .ToList();

        Application.Current.Dispatcher.Invoke(() =>
        {
            Waveform = new PointCollection(Ranges.Select((range, i) => new Point(i, range.Min + 1)).Concat(Ranges.AsEnumerable().Reverse().Select((range, i) => new Point(Ranges.Count - i - 1, range.Max + 1))));
        });

        MaxLevel = _samples.Max();
        MinLevel = _samples.Min();
    }

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

    public List<Range> Ranges
    {
        get => _ranges;
        set => SetProperty(ref _ranges, value);
    }
}

public struct Range
{
    public float Min { get; }
    
    public float Max { get; }

    public Range(float min, float max)
    {
        Min = min;
        Max = max;
    }
}