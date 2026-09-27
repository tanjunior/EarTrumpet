using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EarTrumpet.DataModel.VirtualMixer.Voicemeeter;

// Wrapper over VoicemeeterRemote(64).dll, which ships with every Voicemeeter install.
// The DLL is loaded from the install folder at runtime so EarTrumpet still runs without Voicemeeter.
internal sealed class VoicemeeterRemote
{
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VB:Voicemeeter {17359A74-1236-5467}";
    private const string DefaultInstallDir = @"C:\Program Files (x86)\VB\Voicemeeter";

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NoArgsFn();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int OutIntFn(out int value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetFloatFn([MarshalAs(UnmanagedType.LPStr)] string name, out float value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetFloatFn([MarshalAs(UnmanagedType.LPStr)] string name, float value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate int GetStringWFn([MarshalAs(UnmanagedType.LPStr)] string name, StringBuilder value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetStringWFn([MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetLevelFn(int type, int channel, out float value);

    private readonly NoArgsFn _login;
    private readonly NoArgsFn _logout;
    private readonly OutIntFn _getVoicemeeterType;
    private readonly NoArgsFn _isParametersDirty;
    private readonly GetFloatFn _getParameterFloat;
    private readonly SetFloatFn _setParameterFloat;
    private readonly GetStringWFn _getParameterStringW;
    private readonly SetStringWFn _setParameterStringW;
    private readonly GetLevelFn _getLevel;

    private VoicemeeterRemote(IntPtr module)
    {
        _login = Export<NoArgsFn>(module, "VBVMR_Login");
        _logout = Export<NoArgsFn>(module, "VBVMR_Logout");
        _getVoicemeeterType = Export<OutIntFn>(module, "VBVMR_GetVoicemeeterType");
        _isParametersDirty = Export<NoArgsFn>(module, "VBVMR_IsParametersDirty");
        _getParameterFloat = Export<GetFloatFn>(module, "VBVMR_GetParameterFloat");
        _setParameterFloat = Export<SetFloatFn>(module, "VBVMR_SetParameterFloat");
        _getParameterStringW = Export<GetStringWFn>(module, "VBVMR_GetParameterStringW");
        _setParameterStringW = Export<SetStringWFn>(module, "VBVMR_SetParameterStringW");
        _getLevel = Export<GetLevelFn>(module, "VBVMR_GetLevel");
    }

    // Returns null if Voicemeeter isn't installed or has no Remote API build for this architecture (e.g. ARM64).
    public static VoicemeeterRemote TryLoad()
    {
        var dllName = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "VoicemeeterRemote64.dll",
            Architecture.X86 => "VoicemeeterRemote.dll",
            _ => null,
        };
        if (dllName == null)
        {
            return null;
        }

        var path = Path.Combine(FindInstallDir(), dllName);
        if (!File.Exists(path) || !NativeLibrary.TryLoad(path, out var module))
        {
            return null;
        }

        try
        {
            return new VoicemeeterRemote(module);
        }
        catch (EntryPointNotFoundException)
        {
            NativeLibrary.Free(module);
            return null;
        }
    }

    // 0: OK, 1: OK but Voicemeeter isn't running, negative: error.
    public int Login() => _login();

    public void Logout() => _logout();

    // 1: Standard, 2: Banana, 3: Potato. Null when Voicemeeter isn't running.
    public int? GetVoicemeeterType() => _getVoicemeeterType(out var type) == 0 ? type : null;

    // True when any parameter changed since the last call. Must be polled before reading parameters.
    public bool IsParametersDirty() => _isParametersDirty() > 0;

    public float? GetFloat(string name) => _getParameterFloat(name, out var value) == 0 ? value : null;

    public void SetFloat(string name, float value) => _setParameterFloat(name, value);

    public string GetString(string name)
    {
        // The API writes up to 512 wide chars.
        var value = new StringBuilder(512);
        return _getParameterStringW(name, value) == 0 ? value.ToString() : null;
    }

    public void SetString(string name, string value) => _setParameterStringW(name, value);

    // type: 0 = pre-fader input, 1 = post-fader input, 2 = post-mute input, 3 = output. Linear amplitude.
    public float GetLevel(int type, int channel) => _getLevel(type, channel, out var value) == 0 ? value : 0f;

    private static T Export<T>(IntPtr module, string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(module, name));

    private static string FindInstallDir()
    {
        // Voicemeeter's installer is 32-bit, so its uninstall entry lives in the 32-bit registry view.
        using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(UninstallKey);
        var uninstaller = key?.GetValue("UninstallString") as string;
        return string.IsNullOrEmpty(uninstaller) ? DefaultInstallDir : Path.GetDirectoryName(uninstaller.Trim('"'));
    }
}
