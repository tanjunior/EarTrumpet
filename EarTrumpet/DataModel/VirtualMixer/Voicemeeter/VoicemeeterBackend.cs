using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

public sealed class VoicemeeterBackend : IVirtualMixerBackend
{
    private sealed record Edition(string Name, int HardwareStrips, string[] VirtualStripNames);

    // Keyed by VBVMR_GetVoicemeeterType. Hardware strips carry 2 level channels, virtual strips 8.
    private static readonly Dictionary<int, Edition> Editions = new()
    {
        [1] = new("Voicemeeter", 2, ["Voicemeeter VAIO"]),
        [2] = new("Voicemeeter Banana", 3, ["Voicemeeter VAIO", "Voicemeeter AUX"]),
        [3] = new("Voicemeeter Potato", 5, ["Voicemeeter VAIO", "Voicemeeter AUX", "Voicemeeter VAIO3"]),
    };

    private readonly VoicemeeterRemote _remote;
    private readonly bool _isLoggedIn;
    private Edition _edition;
    private VoicemeeterStrip[] _strips = [];

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

    public string DisplayName => _edition?.Name;
    public IReadOnlyList<IVirtualStrip> Strips => _strips;

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
            return;
        }

        // Always poll the dirty flag: the API only refreshes parameter values when it's read.
        if (_remote.IsParametersDirty() || isNewEdition)
        {
            foreach (var strip in _strips)
            {
                strip.RefreshParameters();
            }
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

    private VoicemeeterStrip[] CreateStrips(Edition edition)
    {
        var hardware = Enumerable.Range(0, edition.HardwareStrips)
            .Select(i => new VoicemeeterStrip(_remote, i, $"Hardware Input {i + 1}", isVirtual: false, levelChannel: i * 2));
        var virtualStrips = edition.VirtualStripNames
            .Select((name, i) => new VoicemeeterStrip(_remote, edition.HardwareStrips + i, name, isVirtual: true,
                levelChannel: edition.HardwareStrips * 2 + i * 8));
        return [.. hardware, .. virtualStrips];
    }
}
