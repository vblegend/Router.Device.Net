using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Router.Device.Net
{

    public class SpecVersion
    {
        public Int32 major { get; set; }
        public Int32 minor { get; set; }
    }


    public class SpecService
    {
        [XmlElement("serviceType")]
        public String serviceType { get; set; }

        [XmlElement("serviceId")]
        public String serviceId { get; set; }

        [XmlElement("SCPDURL")]
        public String ScpdUrl { get; set; }

        [XmlElement("controlURL")]
        public String controlUrl { get; set; }

        [XmlElement("eventSubURL")]
        public String eventSubUrl { get; set; }
    }

    public class SpecArgument
    {
        [XmlElement("name")]
        public String name { get; set; }

        /// <summary>
        /// in out
        /// </summary>
        [XmlElement("direction")]
        public String direction { get; set; }

        [XmlElement("relatedStateVariable")]
        public String relatedStateVariable { get; set; }




    }



    public class SpecAction {
        [XmlElement("name")]
        public String name { get; set; }

        [XmlArray("argumentList")]
        [XmlArrayItem("argument")]
        public List<SpecArgument> arguments { get; set; }

    }




    public class SpecDevice
    {
        [XmlElement("deviceType")]
        public String deviceType { get; set; }

        [XmlElement("presentationURL")]
        public String presentationURL { get; set; }

        [XmlElement("friendlyName")]
        public String friendlyName { get; set; }

        [XmlElement("manufacturer")]
        public String manufacturer { get; set; }

        [XmlElement("manufacturerURL")]
        public String manufacturerURL { get; set; }

        [XmlElement("modelDescription")]
        public String modelDescription { get; set; }

        [XmlElement("modelName")]
        public String modelName { get; set; }


        [XmlElement("modelNumber")]
        public String modelNumber { get; set; }

        [XmlElement("UDN")]
        public String udn { get; set; }


        [XmlElement("UPC")]
        public String upc { get; set; }


        [XmlArray("serviceList")]
        [XmlArrayItem("service")]
        public List<SpecService> services { get; set; }


        [XmlArray("deviceList")]
        [XmlArrayItem("device")]
        public List<SpecDevice> devices { get; set; }

    }




    [XmlRoot(elementName: "root", Namespace = "urn:schemas-upnp-org:device-1-0")]
    public class DeviceDescriptionFile
    {
        [XmlElement("specVersion")]
        public SpecVersion specVersion { get; set; }

        [XmlElement("device")]
        public SpecDevice device { get; set; }
    }





    [XmlRoot(elementName: "scpd", Namespace = "urn:schemas-upnp-org:service-1-0")]
    public class ServiceDescriptionFile
    {
        [XmlElement("specVersion")]
        public SpecVersion specVersion { get; set; }

        [XmlArray("actionList")]
        [XmlArrayItem("action")]
        public List<SpecAction> actions { get; set; }
    }


}
