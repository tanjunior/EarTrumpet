using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

public sealed class VoicemeeterBackend : IVirtualMixerBackend
{
    private sealed record VirtualStrip(string Name, string WindowsDeviceName);
    private sealed record Edition(string Name, int HardwareStrips, VirtualStrip[] VirtualStrips);

    private static readonly VirtualStrip Vaio = new("Voicemeeter VAIO", "Voicemeeter Input");
    private static readonly VirtualStrip Aux = new("Voicemeeter AUX", "Voicemeeter AUX Input");
    private static readonly VirtualStrip Vaio3 = new("Voicemeeter VAIO3", "Voicemeeter VAIO3 Input");

    // Keyed by VBVMR_GetVoicemeeterType. Hardware strips carry 2 level channels, virtual strips 8.
    private static readonly Dictionary<int, Edition> Editions = new()
    {
        [1] = new("Voicemeeter", 2, [Vaio]),
        [2] = new("Voicemeeter Banana", 3, [Vaio, Aux]),
        [3] = new("Voicemeeter Potato", 5, [Vaio, Aux, Vaio3]),
    };

    // Windows names every Voicemeeter virtual playback device "<name> (VB-Audio Voicemeeter VAIO)".
    private const string DeviceNameSuffix = " (VB-Audio Voicemeeter VAIO)";

    private readonly VoicemeeterRemote _remote;
    private readonly bool _isLoggedIn;
    private Edition _edition;
    private VoicemeeterStrip[] _strips = [];
    private HashSet<string> _usedDevices = [];

    public VoicemeeterBackend()
    {
        _remote = VoicemeeterRemote.TryLoad();
        if (_remote == null)
        {
            Trace.WriteLine("VoicemeeterBackend: Voicemeeter Remote API not available");
            return;
        }

        var result = _remote.Login();
        _isLoggedIn = result >= 0;
        Trace.WriteLine($"VoicemeeterBackend: Login result {result}");
    }

    public event EventHandler StripsChanged;
    public event EventHandler UsedDevicesChanged;

    public string DisplayName => _edition?.Name;
    public IReadOnlyList<IVirtualStrip> Strips => _strips;

    public bool IsUnusedDevice(string deviceDisplayName) =>
        _edition != null &&
        deviceDisplayName.EndsWith(DeviceNameSuffix, StringComparison.Ordinal) &&
        !_usedDevices.Contains(TrimDeviceNameSuffix(deviceDisplayName));

    public void Update(bool includeLevels)
    {
        if (!_isLoggedIn)
        {
            return;
        }

        var type = _remote.GetVoicemeeterType();
        var edition = type.HasValue ? Editions.GetValueOrDefault(type.Value) : null;
        var isNewEdition = edition != _edition;
        if (isNewEdition)
        {
            _edition = edition;
            _strips = edition == null ? [] : CreateStrips(edition);
            StripsChanged?.Invoke(this, EventArgs.Empty);
        }

        if (_edition == null)
        {
            if (isNewEdition)
            {
                SetUsedDevices([]);
            }
            return;
        }

        // Always poll the dirty flag: the API only refreshes parameter values when it's read.
        if (_remote.IsParametersDirty() || isNewEdition)
        {
            foreach (var strip in _strips)
            {
                strip.RefreshParameters();
            }
            SetUsedDevices(ReadUsedDevices());
        }

        if (includeLevels)
        {
            foreach (var strip in _strips)
            {
                strip.UpdateLevels();
            }
        }
    }

    public void Dispose()
    {
        if (_isLoggedIn)
        {
            _remote.Logout();
        }
    }

    // Virtual devices feeding a virtual strip, plus any virtual device picked as a hardware strip's input
    // (the paid VAIO Extension devices "Voicemeeter In 1-5" only work that way).
    private HashSet<string> ReadUsedDevices()
    {
        var used = _strips.Where(s => s.WindowsDeviceName != null).Select(s => s.WindowsDeviceName).ToHashSet();
        for (var i = 0; i < _edition.HardwareStrips; i++)
        {
            var device = _remote.GetString($"Strip[{i}].device.name");
            if (!string.IsNullOrEmpty(device))
            {
                used.Add(TrimDeviceNameSuffix(device));
            }
        }
        return used;
    }

    private void SetUsedDevices(HashSet<string> used)
    {
        if (!used.SetEquals(_usedDevices))
        {
            _usedDevices = used;
            UsedDevicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string TrimDeviceNameSuffix(string name) =>
        name.EndsWith(DeviceNameSuffix, StringComparison.Ordinal) ? name[..^DeviceNameSuffix.Length] : name;

    private VoicemeeterStrip[] CreateStrips(Edition edition)
    {
        var hardware = Enumerable.Range(0, edition.HardwareStrips)
            .Select(i => new VoicemeeterStrip(_remote, i, $"Hardware Input {i + 1}", windowsDeviceName: null, levelChannel: i * 2));
        var virtualStrips = edition.VirtualStrips
            .Select((strip, i) => new VoicemeeterStrip(_remote, edition.HardwareStrips + i, strip.Name, strip.WindowsDeviceName,
                levelChannel: edition.HardwareStrips * 2 + i * 8));
        return [.. hardware, .. virtualStrips];
    }
}
