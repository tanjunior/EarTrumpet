using EarTrumpet.Extensions;
using System;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

internal sealed class VoicemeeterStrip : BindableBase, IVirtualStrip
{
    // Voicemeeter faders run from -60 dB to +12 dB. EarTrumpet sliders top out at 0 dB, so boost above
    // unity set in Voicemeeter is shown as full scale and only replaced once the slider is moved.
    private const float MinGainDb = -60f;
    private const float MaxGainDb = 0f;

    // The Remote API applies writes asynchronously, so a read right after a write can return the old
    // value and make the slider jump back. Ignore remote values briefly after a local change.
    private static readonly TimeSpan LocalChangeHold = TimeSpan.FromMilliseconds(300);

    private readonly VoicemeeterRemote _remote;
    private readonly string _param;
    private readonly string _defaultName;
    private readonly int _levelChannel;
    private string _displayName;
    private float _gainDb;
    private bool _isMuted;
    private DateTime _holdRemoteUntil;

    public VoicemeeterStrip(VoicemeeterRemote remote, int index, string defaultName, bool isVirtual, int levelChannel)
    {
        _remote = remote;
        _param = $"Strip[{index}]";
        _defaultName = defaultName;
        _displayName = defaultName;
        _levelChannel = levelChannel;
        IsVirtual = isVirtual;
    }

    public string Id => $"Voicemeeter.{_param}";
    public bool IsVirtual { get; }
    public float PeakValue1 { get; private set; }
    public float PeakValue2 { get; private set; }

    public string DisplayName
    {
        get => _displayName;
        private set
        {
            if (_displayName != value)
            {
                _displayName = value;
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                _remote.SetFloat($"{_param}.Mute", value ? 1f : 0f);
                _holdRemoteUntil = DateTime.UtcNow + LocalChangeHold;
                RaisePropertyChanged(nameof(IsMuted));
            }
        }
    }

    // Scalar or dB depending on App.Settings.UseLogarithmicVolume, like Windows audio streams.
    public float Volume
    {
        get => App.Settings.UseLogarithmicVolume ? GetVolumeLogarithmic() : GetVolumeScalar();
        set
        {
            if (App.Settings.UseLogarithmicVolume)
            {
                SetVolumeLogarithmic(value);
            }
            else
            {
                SetVolumeScalar(value);
            }
        }
    }

    // Linear in dB across the fader range, matching how Voicemeeter's own faders move.
    public float GetVolumeScalar() => ((_gainDb - MinGainDb) / (MaxGainDb - MinGainDb)).Bound(0, 1f);

    public float GetVolumeLogarithmic() => _gainDb.Bound(App.Settings.LogarithmicVolumeMinDb, MaxGainDb);

    public void SetVolumeScalar(float value) => SetGain(MinGainDb + value.Bound(0, 1f) * (MaxGainDb - MinGainDb));

    // The bottom of EarTrumpet's dB slider means "off", so map it to Voicemeeter's minimum.
    public void SetVolumeLogarithmic(float value) => SetGain(value <= App.Settings.LogarithmicVolumeMinDb ? MinGainDb : value);

    public void RefreshParameters()
    {
        var label = _remote.GetString($"{_param}.Label");
        DisplayName = string.IsNullOrWhiteSpace(label) ? _defaultName : label;

        if (DateTime.UtcNow < _holdRemoteUntil)
        {
            return;
        }

        var gain = _remote.GetFloat($"{_param}.Gain");
        if (gain.HasValue && gain.Value != _gainDb)
        {
            _gainDb = gain.Value;
            RaisePropertyChanged(nameof(Volume));
        }

        var mute = _remote.GetFloat($"{_param}.Mute");
        if (mute.HasValue && (mute.Value >= 0.5f) != _isMuted)
        {
            _isMuted = mute.Value >= 0.5f;
            RaisePropertyChanged(nameof(IsMuted));
        }
    }

    // Pre-fader, like Windows audio peaks: VolumeSlider scales the meter by the fader position itself.
    public void UpdateLevels()
    {
        var left = _remote.GetLevel(0, _levelChannel).Bound(0, 1f);
        var right = _remote.GetLevel(0, _levelChannel + 1).Bound(0, 1f);
        if (App.Settings.UseLogarithmicVolume)
        {
            left = left.LinearToLogNormalized();
            right = right.LinearToLogNormalized();
        }
        PeakValue1 = left;
        PeakValue2 = right;
    }

    private void SetGain(float gainDb)
    {
        gainDb = gainDb.Bound(MinGainDb, MaxGainDb);
        _holdRemoteUntil = DateTime.UtcNow + LocalChangeHold;
        if (gainDb != _gainDb)
        {
            _gainDb = gainDb;
            _remote.SetFloat($"{_param}.Gain", gainDb);
            RaisePropertyChanged(nameof(Volume));
        }
    }
}
