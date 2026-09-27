using EarTrumpet.DataModel.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarTrumpet.UI.ViewModels;

public class EarTrumpetDevicesSettingsPageViewModel : SettingsPageViewModel
{
    public class DeviceItem : BindableBase
    {
        private readonly EarTrumpetDevicesSettingsPageViewModel _page;
        private readonly IAudioDevice _device;

        public DeviceItem(EarTrumpetDevicesSettingsPageViewModel page, IAudioDevice device)
        {
            _page = page;
            _device = device;
        }

        public string DisplayName => _device.DisplayName;
        public bool IsAutoHidden => _page.HideUnusedVirtualMixerDevices && _page._isUnusedVirtualMixerDevice(_device);

        public bool IsShown
        {
            get => !_page._settings.HiddenDeviceIds.Contains(_device.Id);
            set
            {
                var hidden = _page._settings.HiddenDeviceIds.Where(id => id != _device.Id);
                _page._settings.HiddenDeviceIds = (value ? hidden : hidden.Append(_device.Id)).ToArray();
                RaisePropertyChanged(nameof(IsShown));
            }
        }

        public void RaiseIsAutoHiddenChanged() => RaisePropertyChanged(nameof(IsAutoHidden));
    }

    private readonly AppSettings _settings;
    private readonly Func<IAudioDevice, bool> _isUnusedVirtualMixerDevice;

    public EarTrumpetDevicesSettingsPageViewModel(AppSettings settings, IAudioDeviceManager deviceManager, Func<IAudioDevice, bool> isUnusedVirtualMixerDevice) : base(null)
    {
        _settings = settings;
        _isUnusedVirtualMixerDevice = isUnusedVirtualMixerDevice;
        Title = Properties.Resources.DevicesSettingsPageText;
        Glyph = "\xE7F5";
        Devices = deviceManager.Devices
            .OrderBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Select(d => new DeviceItem(this, d))
            .ToList();
    }

    public IReadOnlyList<DeviceItem> Devices { get; }

    public bool HideUnusedVirtualMixerDevices
    {
        get => _settings.HideUnusedVirtualMixerDevices;
        set
        {
            _settings.HideUnusedVirtualMixerDevices = value;
            foreach (var device in Devices)
            {
                device.RaiseIsAutoHiddenChanged();
            }
        }
    }
}
