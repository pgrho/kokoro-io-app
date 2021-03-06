using System.Linq;
using KokoroIO.XamarinForms.Services;
using KokoroIO.XamarinForms.UWP.Services;
using Windows.Graphics.Display;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Xamarin.Forms;

[assembly: Dependency(typeof(DeviceService))]

namespace KokoroIO.XamarinForms.UWP.Services
{
    public sealed class DeviceService : IDeviceService
    {
        public string MachineName
            => NetworkInformation
                .GetHostNames()
                .FirstOrDefault(n => n.Type == HostNameType.DomainName)?.DisplayName ?? "Universal Windows Platform";

        public DeviceKind Kind => DeviceKind.Uwp;
         
        public float GetDisplayScale()
            => (float)DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
    }
}