using RJCP.IO.Ports;
using System.Diagnostics;

namespace TM5103.OPCUA
{
    internal class Port(string portname, int portspeed)
    {
        SerialPortStream port = new SerialPortStream();
        public string PortName = portname;

        public bool IsConnected { get; private set; }

        public bool Connect()
        {
            try
            {
                port.PortName = portname;
                port.BaudRate = portspeed;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.DtrEnable = true;
                port.RtsEnable = false;
                port.ReadTimeout = 1000;
                port.WriteTimeout = 1000;
                port.NewLine = "\r";
                port.Open();
                IsConnected = true;
                return true;
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc);
                return false;

            }
        }

        public void Disconnect()
        {
            port.Flush();
            port.Close();
        }



        static string KSUM(string S)
        {
            int KS = 65535;
            for (int i = 1; i < S.Length; i++) // Start from index 1 to match Pascal's 1-based index
            {
                KS ^= (byte)S[i]; // XOR with the ASCII value of the character
                for (int l = 0; l < 8; l++)
                {
                    if ((KS & 1) == 1) // Check if the least significant bit is 1
                    {
                        KS = (KS >> 1) ^ 40961; // Right shift and XOR with 40961
                    }
                    else
                    {
                        KS >>= 1; // Just right shift
                    }
                }
            }
            return KS.ToString(); // Convert KS to string
        }

        public string ReadPV(int address, int chan)
        {
            string askstring = ":" + address + ";1;" + chan + ";";

            string answstring;
            string chkstring = "";
            string answks;
            try
            {
                port.Write(askstring + KSUM(askstring) + "\r");
                answstring = port.ReadLine();
            }
            catch (System.IO.IOException e) { Debug.WriteLine(e.Message + " Closing port"); port.Close(); IsConnected = false; return "-9999"; }
            catch (System.TimeoutException e) { Debug.WriteLine(e.Message + " Time is out"); return "$timeout";  }
            catch (Exception e) { Debug.WriteLine(e.Message); return "-9999"; }
            answstring = answstring.Substring(answstring.IndexOf("!"));
            string[] parts = answstring.Split(';');
            Console.WriteLine(answstring);
            for (int i = 0; i < answstring.Length; i++) Console.Write((int)answstring[i] + " ");
            Console.WriteLine();
            answks = parts[parts.Length - 1];

            for (int i = 0; i < parts.Length - 1; i++)
            {
                chkstring = chkstring + parts[i] + ";";
            }

            if (KSUM(chkstring) == answks)
            {
                Console.WriteLine("Answer: " + parts[1]);
                return parts[1];
            }
            else
                return "-999";
        }


    }
}
