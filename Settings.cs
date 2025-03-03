using RJCP.IO.Ports;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace TM5103.OPCUA
{
    internal static class Settings
    {
        private static string _filepath = System.IO.Path.GetDirectoryName(Environment.ProcessPath) + "\\settings.xml";

        private static int _ipport = 7718;

        public static int IpPort
        {
            get => _ipport;
            set { _ipport = value; }
        }

        private static string _port = "COM1";
        public static string Port
        {
            get => _port;
            set { _port = value; }
        }
        private static int _speed = 9600;

        public static int Speed
        {
            get => _speed;
            set { _speed = value; }
        }

        private static int _address = 1;

        public static int Address
        {
            get => _address;
            set { _address = value; }
        }



        private static bool[] _chanlist = new bool[] { true, true, true, true, true, true, true, true };

        public static bool[] Chanlist
        {
            get => _chanlist;
            set { _chanlist = value; }
        }

        /// <summary>
        /// Коллекция всех настроек
        /// Ожидается в формате (string)"Имя ком порта", (int)Скорость, (Dictionary &lt;int, Dictionary&lt;int, bool&gt;&gt;)Cловарь "адрес - словарь каналов"
        /// </summary>
        public static List<List<object>>? AllSettings = new();

        public static List<SerialPortStream> serialPortStreams = new List<SerialPortStream>();

        public static string ReadAll()
        {
            string logmsg = "";
            if (!File.Exists(_filepath))
            {
                Debug.WriteLine(_filepath + " not found, creating sample one");
                Settings.Save();
                Settings.ReadAll();
            }
            else
            {
                Debug.WriteLine(_filepath + " found, attempting read");
                logmsg = $"Settings File: {_filepath} \r\n";
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(_filepath);
                XmlElement? xRoot = xDoc.DocumentElement; // получим корневой элемент
                if (xRoot != null & xRoot.Name == "server")
                {
                    XmlNode? ipport = xRoot.Attributes.GetNamedItem("ipport");
                    _ipport = Convert.ToInt32(ipport?.Value);
                    logmsg += $"Endpoint URL: opc.tcp://localhost:{Settings.IpPort} \r\n";
                    foreach (XmlElement xnode in xRoot) // обход всех узлов в корневом элементе
                    {
                        List<object>? PortSettingsList = new();
                        Dictionary<int, Dictionary<int, bool>>? AddrChanList = new();
                        XmlNode? name = xnode.Attributes.GetNamedItem("name"); // получаем атрибуты
                        XmlNode? speed = xnode.Attributes.GetNamedItem("speed");
                        PortSettingsList?.Add(name?.Value);
                        PortSettingsList?.Add(Convert.ToInt32(speed?.Value));
                        Debug.WriteLine($"Type: {xnode.NodeType} PortName: {name?.Value} Speed: {speed?.Value}");
                        logmsg += $"Polling: {name?.Value}@{speed?.Value} \r\n";


                        foreach (XmlNode addrnode in xnode.ChildNodes) // обходим все дочерние узлы элемента port
                        {

                            Dictionary<int, bool> channellist = new Dictionary<int, bool>();
                            logmsg += $"___________Address: {addrnode.Attributes?.GetNamedItem("address")?.Value} \r\n";
                            foreach (XmlNode channode in addrnode)
                            {
                                int key = Convert.ToInt32(channode.Attributes?.GetNamedItem("num")?.Value);
                                bool value = Convert.ToBoolean(channode.InnerText);
                                channellist.Add(key, value);
                                Debug.WriteLine($"Channel {channode.Attributes?.GetNamedItem("num")?.Value}: {Convert.ToBoolean(channode.InnerText)}");
                                logmsg += $"____________________Channel {channode.Attributes?.GetNamedItem("num")?.Value}: {Convert.ToBoolean(channode.InnerText)} \r\n";
                            }
                            AddrChanList?.Add(Convert.ToInt32(addrnode.Attributes?.GetNamedItem("address")?.Value), channellist);
                            
                        }
                        PortSettingsList.Add(AddrChanList);
                        AllSettings?.Add(PortSettingsList);
                    }
                }
            }
            return logmsg;
        }

        public static void Save()
        {
            XDocument xdoc = new XDocument();
            XElement tm = new XElement("server");
            XAttribute ipport = new XAttribute("ipport", _ipport);
            tm.Add(ipport);
            XElement comport = new XElement("port");
            XAttribute name = new XAttribute("name", _port);
            XAttribute speed = new XAttribute("speed", _speed);

            comport.Add(name);
            comport.Add(speed);

            XElement address = new XElement("address");
            XAttribute addressnum = new XAttribute("address", _address);
            address.Add(addressnum);
            for (int i = 0; i < _chanlist.Length; i++)
            {
                XElement channame = new XElement("Channel");
                XAttribute channum = new XAttribute("num", (i + 1));
                channame.Value = true.ToString();
                channame.Add(channum);
                address.Add(channame);
            };
            comport.Add(address);
            tm.Add(comport);
            xdoc.Add(tm);
            xdoc.Save(_filepath);
        }
    }
}
