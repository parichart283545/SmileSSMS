using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SmileSCommunicateRESTfulService.Models
{
    [XmlRoot("DeliveryNotify")]
    public class DeliveryNotify
    {
        [XmlElement("transaction")]
        public Transactions Transaction { get; set; }
    }

    public class Transactions
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("status")]
        public string Status { get; set; }

        [XmlAttribute("detail")]
        public string Detail { get; set; }

        [XmlElement("data")]
        public List<Data> Data { get; set; }
    }

    public class Data
    {
        [XmlAttribute("phone")]
        public string Phone { get; set; }

        [XmlAttribute("credit")]
        public string Credit { get; set; }

        [XmlAttribute("processing")]
        public string Processing { get; set; }

        [XmlAttribute("success")]
        public string Success { get; set; }

        [XmlAttribute("fail")]
        public string Fail { get; set; }

        [XmlAttribute("block")]
        public string Block { get; set; }

        [XmlAttribute("expire")]
        public string Expire { get; set; }

        [XmlAttribute("unknown")]
        public string Unknown { get; set; }

        [XmlAttribute("return")]
        public string Return { get; set; }
    }
}