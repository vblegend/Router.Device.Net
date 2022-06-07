# Router.Device.Net
Use the UPNP discovery protocol to obtain the external IP address of the network router

## examples
``` csharp
using Router.Device.Net;

Console.WriteLine("Hello, World!");
var routers = RouterDeviceNet.FindRouters("以太网").Result;
foreach (var router in routers)
{
    var ip = router.GetExternalIPAddress().Result;
    Console.WriteLine("==============================================");
    Console.WriteLine($"DeviceName：{router.friendlyName}");
    Console.WriteLine($" DeviceUrl：{router.presentationURL.Trim()}");
    Console.WriteLine($" IPAddress：{ip}");
}
Console.ReadLine();
```