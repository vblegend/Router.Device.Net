using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;



namespace Router.Device.Net
{
    public class RouterDeviceNet
    {
        private static readonly String DiscoverMessage = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 3\r\nST: upnp:rootdevice\r\n\r\n";
        private static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.250");
        private static readonly Int32 MulticastPort = 1900;
        private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(MulticastAddress, MulticastPort);


        public static async Task<IReadOnlyList<RouterDevice>> FindRouters(String netAdapterName)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                if (adapter.Name == netAdapterName)
                {
                    return await FindRouters(adapter);
                }
            }
            throw new Exception("network adapter not found.");
        }


        public static async Task<IReadOnlyList<RouterDevice>> FindRouters(NetworkInterface netAdapter)
        {
            var properties = netAdapter.GetIPProperties();
            foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return await FindRouters(ip.Address);
                }
            }
            throw new Exception("The network adapter is not connected to the device.");
        }

        public static async Task<IReadOnlyList<RouterDevice>> FindRouters(IPAddress localIp)
        {
            var result = new List<Task<RouterDevice>>();
            var udp = new UdpClient();
            udp.Client.Bind(new IPEndPoint(localIp, 0));
            udp.EnableBroadcast = true;
            udp.JoinMulticastGroup(MulticastAddress, localIp);
            var buffer = Encoding.Default.GetBytes(DiscoverMessage);
            await udp.SendAsync(buffer, buffer.Length, MulticastEndPoint);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {
                cancellationTokenSource.CancelAfter(1000);
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var recv =  udp.ReceiveAsync();
                    recv.Wait(cancellationTokenSource.Token);
                    string message = Encoding.Default.GetString(recv.Result.Buffer);
                    if (message.Contains("upnp:rootdevice"))
                    {
                        result.Add(parseDevice(message));
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
            }
            finally
            {
                udp.Dispose();
            }

            Task.WaitAll(result.ToArray());

            return result.Where(E => E.Result != null).Select(e => e.Result).ToList();
        }



        /// <summary>
        /// HTTP/1.1 200 OK
        /// CACHE-CONTROL: max-age=60
        /// DATE: Tue, 07 Jun 2022 12:45:07 GMT
        /// EXT:
        /// LOCATION: http://192.168.1.1:1900/igd.xml
        /// SERVER: vxWorks/5.5 UPnP/1.0 TL-XDR3010易展版/2.0
        /// ST: upnp:rootdevice
        /// USN: uuid:8c15e41f-3d83-41c1-b35d-5DC486961FE5::upnp:rootdevice
        /// </summary>
        /// <param name="infoString"></param>
        /// <returns></returns>

        private static async Task<RouterDevice> parseDevice(string infoString)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage result = null;
            Stream stream = null;
            try
            {
                Regex reg = new Regex("(?<=(LOCATION:))[.\\s\\S]*?(?=(\r\n))", RegexOptions.Multiline | RegexOptions.Singleline);
                var locationUrl = reg.Match(infoString).Value;
                if (locationUrl.Length == 0) return null;
                result = await client.GetAsync(locationUrl);
                stream = await result.Content.ReadAsStreamAsync();
                XmlSerializer xs = new XmlSerializer(typeof(DeviceDescriptionFile));
                var descriptionFile = xs.Deserialize(stream) as DeviceDescriptionFile;
                if (descriptionFile != null && descriptionFile.device.deviceType.Contains("InternetGatewayDevice"))
                {
                    var index = locationUrl.LastIndexOf("/");
                    var baseUrl = locationUrl.Substring(0, index);
                    return new RouterDevice(baseUrl, descriptionFile);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (result != null)
                {
                    result.Dispose();
                }
            }
        }







    }
}