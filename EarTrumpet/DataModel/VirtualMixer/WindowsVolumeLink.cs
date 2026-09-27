using EarTrumpet.DataModel.Audio;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace EarTrumpet.DataModel.VirtualMixer;

// Voicemeeter ignores the Windows volume of its virtual playback devices, so the taskbar slider,
// volume keys and EarTrumpet's device slider do nothing for them. This forwards Windows volume and
// mute changes on those devices to the matching virtual strip.
public sealed class WindowsVolumeLink
{
    private readonly IVirtualMixerBackend _backend;

    public WindowsVolumeLink(IAudioDeviceManager deviceManager, IVirtualMixerBackend backend)
    {
        _backend = backend;
        deviceManager.Devices.CollectionChanged += OnDevicesChanged;
        Watch(deviceManager.Devices);
    }

    private void OnDevicesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (IAudioDevice device in e.OldItems)
            {
                device.PropertyChanged -= OnDevicePropertyChanged;
            }
        }
        if (e.NewItems != null)
        {
            Watch(e.NewItems.Cast<IAudioDevice>());
        }
    }

    private void Watch(IEnumerable<IAudioDevice> devices)
    {
        foreach (var device in devices)
        {
            device.PropertyChanged += OnDevicePropertyChanged;
        }
    }

    // Only reacts to changes (not startup state) so a gain set in Voicemeeter isn't overwritten on launch.
    private void OnDevicePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Volume and IsMuted are raised together on every endpoint notification; handle it once.
        if (e.PropertyName != nameof(IAudioDevice.Volume))
        {
            return;
        }

        var device = (IAudioDevice)sender;

        // Strips are only kept current while the flyout is open, so refresh before looking them up.
        _backend.Update(includeLevels: false);
        var strip = _backend.Strips.FirstOrDefault(s => IsLinked(s, device));
        if (strip == null)
        {
            return;
        }

        // The device's dB value follows Windows' volume curve, so 50% on the Windows slider is roughly -10 dB.
        strip.SetGain(device.GetVolumeLogarithmic());
        strip.IsMuted = device.IsMuted;
    }

    // Device display names look like "Voicemeeter Input (VB-Audio Voicemeeter VAIO)".
    private static bool IsLinked(IVirtualStrip strip, IAudioDevice device) =>
        strip.WindowsDeviceName != null &&
        (device.DisplayName == strip.WindowsDeviceName ||
         device.DisplayName.StartsWith(strip.WindowsDeviceName + " (", StringComparison.Ordinal));
}
