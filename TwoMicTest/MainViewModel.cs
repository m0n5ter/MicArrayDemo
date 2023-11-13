using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TwoMicTest;

public class MainViewModel : ObservableObject
{
    private WasapiCapture _capture1;
    private WasapiCapture _capture2;
    private MemoryStream _ms1;
    private MemoryStream _ms2;
    private MMDeviceEnumerator _enumerator;
    private string? _playbackDevice;

    public ObservableCollection<string> PlaybackDevices { get; } = new();
    
    public ObservableCollection<string> RecordDevices { get; } = new();

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
        });
    }

    public string? PlaybackDevice
    {
        get => _playbackDevice;
        set => SetProperty(ref _playbackDevice, value);
    }

    public async Task Test()
    {
        var sss = WaveIn.DeviceCount;
        var ddd = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
        _capture1 = new WasapiCapture(ddd[0]);
        _capture2 = new WasapiCapture(ddd[2]);

        _ms1 = new MemoryStream();
        _ms2 = new MemoryStream();


        _capture1.DataAvailable += (s, a) =>
        {
            _ms1.Write(a.Buffer, 0, a.BytesRecorded);
            if (_ms1.Position >= 1000000)
            {
                _capture1.StopRecording();
                var ttt = _ms1.GetBuffer();
            }
        };

        _capture2.DataAvailable += (s, a) =>
        {
            _ms2.Write(a.Buffer, 0, a.BytesRecorded);
            if (_ms2.Position >= 1000000)
            {
                _capture2.StopRecording();
                var ttt = _ms1.GetBuffer();
            }
        };

        _capture1.StartRecording();
        _capture2.StartRecording();
    }

}