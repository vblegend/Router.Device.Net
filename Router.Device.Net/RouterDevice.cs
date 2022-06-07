using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Router.Device.Net
{
    public class RouterDevice
    {
        private DeviceDescriptionFile descriptionFile;
        private String specBaseUrl;
        private ConcurrentDictionary<String, ServiceDescriptionFile> serviceDesc { get; set; }
        public String deviceType => this.descriptionFile.device.deviceType;
        public String presentationURL => this.descriptionFile.device.presentationURL;
        public String friendlyName => this.descriptionFile.device.friendlyName;
        public String manufacturer => this.descriptionFile.device.manufacturer;
        public String manufacturerURL => this.descriptionFile.device.manufacturerURL;
        public String modelDescription => this.descriptionFile.device.modelDescription;
        public String modelName => this.descriptionFile.device.modelName;
        public String modelNumber => this.descriptionFile.device.modelNumber;
        public String udn => this.descriptionFile.device.udn;
        public String upc => this.descriptionFile.device.upc;


        internal RouterDevice(String specBaseUrl, DeviceDescriptionFile descriptionFile)
        {
            this.specBaseUrl = specBaseUrl;
            this.descriptionFile = descriptionFile;
            this.serviceDesc = new ConcurrentDictionary<String, ServiceDescriptionFile>();
        }
        
        public async Task<String> GetExternalIPAddress()
        {
            var device = FindDeviceByTypeStartWith(this.descriptionFile.device, "urn:schemas-upnp-org:device:WANConnectionDevice:");
            if (device == null) return null;
            var service = FindServiceByTypeStartWith(device, "urn:schemas-upnp-org:service:WANIPConnection:");
            if (service == null) return null;
            var url = this.specBaseUrl + service.ScpdUrl;
            var serviceDescriptionFile = await GetServiceDescriptionFile(url);
            if (serviceDescriptionFile == null) return null;
            var action = FindAction(serviceDescriptionFile, "GetExternalIPAddress");
            if (action == null) return null;
            return await CallAction(service, action);
        }


        private SpecAction FindAction(ServiceDescriptionFile serviceDescriptionFile, String actionName)
        {
            foreach (var action in serviceDescriptionFile.actions)
            {
                if (action.name == actionName) return action;
            }
            return null;
        }

        private async Task<String> CallAction(SpecService service, SpecAction action)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("SoapAction", service.serviceType + "#" + action.name);
                var xml = BuildReuquestXml(service, action);
                HttpContent content = new StringContent(xml.InnerXml);
                content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                content.Headers.ContentType.CharSet = "utf-8";
                var url = descriptionFile.device.presentationURL.Trim() + service.controlUrl;
                using (var result = await client.PostAsync(url, content))
                {
                    var xmlText = await result.Content.ReadAsStringAsync();
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xmlText);
                    var argument = action.arguments.Where(e => e.direction == "out").FirstOrDefault();
                    if (argument == null) return null;
                    var node = xmlDocument.SelectSingleNode(".//" + argument.name);
                    if (node == null) return null;
                    return node.InnerText;
                };
            }
        }

        private XmlDocument BuildReuquestXml(SpecService service, SpecAction action)
        {
            var xml = new XmlDocument();
            var declaration = xml.CreateXmlDeclaration("1.0", "utf-8", null);
            var envelope = xml.CreateNode(XmlNodeType.Element, "Envelope", "http://www.w3.org/2001/12/soap-envelope");
            var body = xml.CreateNode(XmlNodeType.Element, "Body", "http://www.w3.org/2001/12/soap-envelope");
            var getExternalIPAddress = xml.CreateNode(XmlNodeType.Element, action.name, service.serviceType);
            body.AppendChild(getExternalIPAddress);
            envelope.AppendChild(body);
            xml.AppendChild(declaration);
            xml.AppendChild(envelope);
            return xml;
        }

        private SpecDevice FindDeviceByTypeStartWith(SpecDevice rootDevice, String deviceType)
        {
            if (rootDevice.deviceType.StartsWith(deviceType)) return rootDevice;

            foreach (var device in rootDevice.devices)
            {
                var dev = FindDeviceByTypeStartWith(device, deviceType);
                if (dev != null) return dev;
            }
            return null;
        }


        private SpecService FindServiceByTypeStartWith(SpecDevice device, String serviceType)
        {
            foreach (var service in device.services)
            {
                if (service.serviceType.StartsWith(serviceType))
                {
                    return service;
                }
            }
            return null;
        }

        private async Task<ServiceDescriptionFile> GetServiceDescriptionFile(string locationUrl)
        {
            if (this.serviceDesc.TryGetValue(locationUrl, out var serviceDescriptionFile))
            {

                return serviceDescriptionFile;
            }
            HttpClient client = new HttpClient();
            HttpResponseMessage result = null;
            Stream stream = null;
            try
            {
                result = await client.GetAsync(locationUrl);
                stream = await result.Content.ReadAsStreamAsync();
                XmlSerializer xs = new XmlSerializer(typeof(ServiceDescriptionFile));
                serviceDescriptionFile = xs.Deserialize(stream) as ServiceDescriptionFile;
                this.serviceDesc.TryAdd(locationUrl, serviceDescriptionFile);
                return serviceDescriptionFile;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
                if (result != null)
                {
                    result.Dispose();
                }
            }
        }
    }
}
