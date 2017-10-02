using System.Linq;
using System.Threading.Tasks;
using KokoroIO.XamarinForms.UWP;
using Windows.Graphics.Display;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceService))]

namespace KokoroIO.XamarinForms.UWP
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => NetworkInformation
                .GetHostNames()
                .FirstOrDefault(n => n.Type == HostNameType.DomainName)?.DisplayName ?? "Universal Windows Platform";

        public DeviceKind Kind => DeviceKind.Uwp;

        public byte[] GetIdentifier()
        {
            var token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
            var dr = Windows.Storage.Streams.DataReader.FromBuffer(token.Id);

            var buf = new byte[token.Id.Length];
            dr.ReadBytes(buf);

            return buf;
        }

        public float GetDisplayScale()
            => (float)DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

        public Task<string> GetPlatformNotificationServiceHandleAsync()
            => Task.FromResult<string>(null);

        public void UnregisterPlatformNotificationService() { }
    }
}