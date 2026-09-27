using System;
using System.Collections.Generic;

namespace EarTrumpet.DataModel.VirtualMixer;

// A source of virtual audio inputs (e.g. Voicemeeter). The UI only talks to this interface so the
// implementation can be swapped (e.g. for a built-in mixer on top of VB-Cable) without UI changes.
public interface IVirtualMixerBackend : IDisposable
{
    // e.g. "Voicemeeter Banana". Null when not connected.
    string DisplayName { get; }

    IReadOnlyList<IVirtualStrip> Strips { get; }

    // Raised when Strips is replaced (connect, disconnect, or a different mixer edition started).
    event EventHandler StripsChanged;

    // Raised when the result of IsUnusedDevice may have changed.
    event EventHandler UsedDevicesChanged;

    // True for a Windows playback device the mixer installs but isn't using (e.g. an unlicensed
    // extension input). Always false while the mixer isn't running.
    bool IsUnusedDevice(string deviceDisplayName);

    // Pulls state from the mixer. Must be called on the UI thread; cheap enough to call at meter rate.
    void Update(bool includeLevels);
}
