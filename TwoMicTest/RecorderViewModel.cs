using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TwoMicTest;

public class RecorderViewModel: ObservableObject
{
    private string? _device;
    private MMDevice _recorderDevice;
    private WasapiCapture _capture;
    private MemoryStream _memoryStream;
    private int[] _levels;

    public RecorderViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public string? Device
    {
        get => _device;
        set => SetProperty(ref _device, value);
    }

    public MainViewModel MainViewModel { get; }

    public void Warmup(MMDeviceEnumerator enumerator)
    {
        _recorderDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).FirstOrDefault(d => d.FriendlyName == Device) 
                          ?? throw new Exception("Device was disconnected: " + Device);
        _capture = new WasapiCapture(_recorderDevice) {WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)};
        _memoryStream = new MemoryStream();
        _capture.DataAvailable += (s, a) => _memoryStream.Write(a.Buffer, 0, a.BytesRecorded);
    }

    public void Start()
    {
        _capture.StartRecording();
    }

    public void Stop()
    {
        _capture.StopRecording();
        var Levels =_memoryStream.ToArray();
    }

    public int[] Levels
    {
        get => _levels;
        set => SetProperty(ref _levels, value);
    }
}