// See https://aka.ms/new-console-template for more information


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