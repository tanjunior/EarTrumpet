using System;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

internal sealed class VoicemeeterBus : BindableBase, IVirtualBus
{
    private readonly VoicemeeterRemote _remote;
    private readonly string _param;
    private string _label = "";
    private string _deviceName = "";
    private DateTime _holdRemoteUntil;

    public VoicemeeterBus(VoicemeeterRemote remote, int index, string name)
    {
        _remote = remote;
        _param = $"Bus[{index}]";
        Name = name;
    }

    public string Name { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(_label) ? Name : _label;

    // A buses output to a device picked in Voicemeeter; B buses appear in Windows as "Voicemeeter Out Bn".
    public string Description => Name.StartsWith('B')
        ? $"{Name}: Voicemeeter Out {Name}"
        : $"{Name}: {(string.IsNullOrEmpty(_deviceName) ? "no device" : _deviceName)}";

    public string Label
    {
        get => _label;
        set
        {
            value ??= "";
            if (_label != value)
            {
                _label = value;
                _remote.SetString($"{_param}.Label", value);
                _holdRemoteUntil = DateTime.UtcNow + VoicemeeterStrip.LocalChangeHold;
                RaisePropertyChanged(nameof(Label));
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public void RefreshParameters()
    {
        var deviceName = _remote.GetString($"{_param}.device.name") ?? "";
        if (deviceName != _deviceName)
        {
            _deviceName = deviceName;
            RaisePropertyChanged(nameof(Description));
        }

        if (DateTime.UtcNow < _holdRemoteUntil)
        {
            return;
        }

        var label = _remote.GetString($"{_param}.Label") ?? "";
        if (label != _label)
        {
            _label = label;
            RaisePropertyChanged(nameof(Label));
            RaisePropertyChanged(nameof(DisplayName));
        }
    }
}
