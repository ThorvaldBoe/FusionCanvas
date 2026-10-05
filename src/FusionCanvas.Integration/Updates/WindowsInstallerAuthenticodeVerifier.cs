using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FusionCanvas.Application.Updates;

namespace FusionCanvas.Integration.Updates;

public sealed class WindowsInstallerAuthenticodeVerifier : IInstallerAuthenticityVerifier
{
    private static readonly Guid GenericVerifyAction = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    public Task VerifyAsync(
        string installerPath,
        UpdateManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Authenticode verification is only available on Windows.");
        }

        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException("The downloaded update installer could not be found.", installerPath);
        }

        VerifyWindowsSignature(installerPath);

#pragma warning disable SYSLIB0057 // Authenticode exposes the signer through the signed PE file.
        using var signerCertificate = X509Certificate2.CreateFromSignedFile(installerPath);
#pragma warning restore SYSLIB0057
        var actualFingerprint = signerCertificate.GetCertHash(HashAlgorithmName.SHA256);
        var expectedFingerprint = Convert.FromHexString(manifest.PublisherCertificateSha256);
        if (!CryptographicOperations.FixedTimeEquals(actualFingerprint, expectedFingerprint))
        {
            throw new InvalidOperationException("The update installer publisher does not match the release manifest.");
        }

        return Task.CompletedTask;
    }

    private static void VerifyWindowsSignature(string installerPath)
    {
        var fileInfo = new WinTrustFileInfo(installerPath);
        var data = new WinTrustData
        {
            UiChoice = 2,
            RevocationChecks = 0,
            UnionChoice = 1,
            StateAction = 1,
            ProviderFlags = 0x00000010
        };
        var fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());

        try
        {
            var actionIdentifier = GenericVerifyAction;
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, fDeleteOld: false);
            data.FilePointer = fileInfoPointer;
            var result = WinVerifyTrust(IntPtr.Zero, ref actionIdentifier, ref data);
            data.StateAction = 2;
            _ = WinVerifyTrust(IntPtr.Zero, ref actionIdentifier, ref data);

            if (result != 0)
            {
                throw new InvalidOperationException("Windows could not verify the update installer signature.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoPointer);
        }
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint WinVerifyTrust(
        IntPtr windowHandle,
        ref Guid actionIdentifier,
        ref WinTrustData data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class WinTrustFileInfo
    {
        public uint StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>();

        [MarshalAs(UnmanagedType.LPWStr)]
        public string FilePath;

        public IntPtr FileHandle;
        public IntPtr KnownSubject;

        public WinTrustFileInfo(string filePath)
        {
            FilePath = filePath;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WinTrustData
    {
        public uint StructSize;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FilePointer;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;

        public WinTrustData()
        {
            StructSize = (uint)Marshal.SizeOf<WinTrustData>();
            PolicyCallbackData = IntPtr.Zero;
            SipClientData = IntPtr.Zero;
            UiChoice = 0;
            RevocationChecks = 0;
            UnionChoice = 0;
            FilePointer = IntPtr.Zero;
            StateAction = 0;
            StateData = IntPtr.Zero;
            UrlReference = IntPtr.Zero;
            ProviderFlags = 0;
            UiContext = 0;
            SignatureSettings = IntPtr.Zero;
        }
    }
}
