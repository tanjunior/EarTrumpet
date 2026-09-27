using System;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

internal sealed class VoicemeeterRoute : BindableBase, IVirtualRoute
{
    private readonly VoicemeeterRemote _remote;
    private readonly string _param;
    private bool _isEnabled;
    private DateTime _holdRemoteUntil;

    public VoicemeeterRoute(VoicemeeterRemote remote, string stripParam, string busName)
    {
        _remote = remote;
        _param = $"{stripParam}.{busName}";
        Name = busName;
    }

    public string Name { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                _remote.SetFloat(_param, value ? 1f : 0f);
                _holdRemoteUntil = DateTime.UtcNow + VoicemeeterStrip.LocalChangeHold;
                RaisePropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public void RefreshParameters()
    {
        if (DateTime.UtcNow < _holdRemoteUntil)
        {
            return;
        }

        var value = _remote.GetFloat(_param);
        if (value.HasValue && (value.Value >= 0.5f) != _isEnabled)
        {
            _isEnabled = value.Value >= 0.5f;
            RaisePropertyChanged(nameof(IsEnabled));
        }
    }
}
