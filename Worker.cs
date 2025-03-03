using LibUA.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using static TM5103.OPCUA.UAServer;

namespace TM5103.OPCUA
{
    public class Worker : BackgroundService
    {


        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
        public delegate void DataHandler(ushort ns, int addr, int chan, float val, StatusCode status);
        public event DataHandler? GotData;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                #region Settingsreadall
                var sw = new Stopwatch();
                sw.Start();
                string logmsg = Settings.ReadAll();
                var app = new DemoApplication();

                var server = new LibUA.Server.Master(app, Settings.IpPort, 10, 30, 100, _logger);
                server.Start();
                sw.Stop();
                _logger.LogWarning($"Server started in {sw.ElapsedMilliseconds:N3} ms\r\n" + logmsg);
                #endregion
                Debug.WriteLine("_______________________________________");
                GotData += new DataHandler(app.DataUpdate);
                ushort ns = 1;
                foreach (var port in Settings.AllSettings)
                {

                    Task task = new Task(async () =>
                    {
                        Port comport = new Port((string)port[0], (int)port[1]);
                        Debug.WriteLine("Port " + port[0] + " @ " + port[1]);
                       
                        comport.Connect();
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            if (comport.IsConnected)
                            {
                                if (_logger.IsEnabled(LogLevel.Information))
                                {
                                    foreach (var addr in (Dictionary<int, Dictionary<int, bool>>)port[2])
                                    {
                                        Debug.WriteLine("Address " + addr.Key);
                                        foreach (var chan in addr.Value)
                                        {
                                            Debug.WriteLine("Channel #" + chan.Key + " is " + chan.Value);
                                            if (chan.Value)
                                            {
                                                string val = comport.ReadPV(addr.Key, chan.Key - 1);

                                                if (val[0] != 0x24)
                                                {
                                                    GotData?.Invoke(ns, addr.Key, chan.Key, Convert.ToSingle(val, CultureInfo.InvariantCulture), StatusCode.Good);
                                                    Debug.WriteLine($"What I got {ns}, {addr.Key}, {chan.Key}, {Convert.ToSingle(val, CultureInfo.InvariantCulture)}");
                                                }

                                                else
                                                {
                                                    switch (val)
                                                    {
                                                        case "$timeout":
                                                            GotData?.Invoke(ns, addr.Key, chan.Key, -9999f, StatusCode.BadTimeout);
                                                            Debug.WriteLine($"Failed timeout: {ns}, {addr.Key}, {chan.Key}, {-9999f}");
                                                            break;
                                                        default:
                                                            GotData?.Invoke(ns, addr.Key, chan.Key, -9999f, StatusCode.BadOutOfRange);
                                                            Debug.WriteLine($"Failed: {ns}, {addr.Key}, {chan.Key}, {-9999f}");
                                                            break;
                                                    }

                                                }
                                            }
                                        }
                                    }

                                }
                                await Task.Delay(1000, stoppingToken);
                            }
                            else
                            {
                                foreach (var addr in (Dictionary<int, Dictionary<int, bool>>)port[2])
                                {
                                    foreach (var chan in addr.Value)
                                    {
                                        GotData?.Invoke(ns, addr.Key, chan.Key, -9999f, StatusCode.BadNoCommunication);
                                        Debug.WriteLine($"Failed no communication: {ns}, {addr.Key}, {chan.Key}, {-9999f}");
                                    }
                                }
                                
                                await Task.Delay(60000, stoppingToken);
                                string ReconnectStatus;
                                if (comport.Connect()) ReconnectStatus = "Success"; else ReconnectStatus = "Fail";
                                _logger.LogWarning($"Reconnect to {comport.PortName}: {ReconnectStatus}");
                                
                                
                            }
                        }


                    });
                    task.Start();
                    do { await Task.Delay(100); } while (task.Status != TaskStatus.Running);
                    ns++;
                }



            }
            catch (OperationCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
        }


    }
}
