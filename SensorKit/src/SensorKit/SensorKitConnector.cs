using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCLStorage;
using System.IO;
using System.Numerics;
using Plugin.BLE.Abstractions.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SensorKitSDK
{
    public class SensorKitConnector : ViewModel, IConnector
    {
        SensorModel Data;
        IDevice _device;
        bool isUpdating = false;
        DateTime _connectedTime = DateTime.MinValue;
        string currentPacket = null;
        string currentData = "";
        int currentLength = 0;

        public static double GRAVITY = 9.80665;

        bool isLogging = false;
        bool isAutoUpdates = false;

        // Nordic UART
        public static Guid NORDIC_UART_SERVICE = new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
        public static Guid NORDIC_UART_TX = new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
        public static Guid NORDIC_UART_RX = new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");

        private IService uart;
        private ICharacteristic _uartTX;
        private ICharacteristic _uartRX;

        const string PACKET_START = "RX-";
        const string PACKET_END = "TX-";
        const string PACKET_LENGTH = "LX-";
        const string SYNC_TIME = "SX-";
        const string SYNC_COMMAND = @"=";
        const string PUSH_COMMAND = @"s";
        const string LOG_COMMAND = @"d";
        const string SYNC_COMMAND_HISTORY = @"/";


        bool _isConnected = false;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                SetValue(ref _isConnected, value, "IsConnected");
            }
        }

        public SensorKitConnector(SensorModel data)
        {
            Data = data;
        }

        #region IConnector

        public async Task Subscribe()
        {
           
            try
            {
                Debug.WriteLine($"***SUBSCRIBING {Data.Name}...");

                if (Data.Id != null)
                {
                    if(_device == null)
                        _device = await SensorKit.Instance.Adapter.ConnectToKnownDeviceAsync(Data.Id);

                    if (_device != null && (uart == null || _uartTX == null || _uartRX == null ))
                    {
                        _connectedTime = DateTime.Now;

                        uart = await _device.GetServiceAsync(NORDIC_UART_SERVICE);
                        if (uart != null)
                        {
                            _uartRX = await uart.GetCharacteristicAsync(NORDIC_UART_RX);
                            _uartTX = await uart.GetCharacteristicAsync(NORDIC_UART_TX);

                            _uartRX.ValueUpdated += (s, e) =>
                            {
                                var received = Encoding.UTF8.GetString(e.Characteristic.Value, 0, e.Characteristic.Value.Length);
                                ParsePacket(received);
                            };

                            IsConnected = true;

                            await _uartRX.StartUpdatesAsync();
                        }
                    }

                    if(_uartTX != null)
                        await _uartTX.WriteAsync(Encoding.UTF8.GetBytes(SYNC_COMMAND));

                    InvokeHelper.Invoke(() =>
                    {
                        Data.NotifyPropertyChanged("IsSubscribed");
                    });

                }
            }catch(Exception x)
            {
                Debug.WriteLine(x);
            }
        }

        public async Task SetLogging(bool isLogging)
        {
            try
            {

                this.isLogging = isLogging;

                if (_uartTX != null)
                {
                    if (isLogging)
                    {
                        await _uartTX.WriteAsync(Encoding.UTF8.GetBytes(LOG_COMMAND));
                    }
                    else
                    {
                        await _uartTX.WriteAsync(Encoding.UTF8.GetBytes(SYNC_COMMAND));
                    }
                }

            }catch(Exception x)
            {
                Debug.WriteLine(x);
            }
                
        }

        public async Task SetAutoUpdates(bool isAutoUpdates)
        {
            try
            {

                this.isAutoUpdates = isAutoUpdates;

                if (_uartTX != null)
                {
                    if (isAutoUpdates)
                    {
                        await _uartTX.WriteAsync(Encoding.UTF8.GetBytes(PUSH_COMMAND));
                    }
                    else
                    {
                        await _uartTX.WriteAsync(Encoding.UTF8.GetBytes(SYNC_COMMAND));
                    }
                }

            }
            catch (Exception x)
            {
                Debug.WriteLine(x);
            }

        }

        public async Task Unsubscribe()
        {
            try
            {
                if (_device != null)
                {
                    await SensorKit.Instance.Adapter?.DisconnectDeviceAsync(_device);
                }
            }catch(Exception x)
            {
                Debug.WriteLine(x);
            }
            _connectedTime = DateTime.MinValue;
            IsConnected = false;
        }

        #endregion

        #region Data parsing

        string log_filename = null;

        private void ParsePacket(string received)
        {
            //if (this.isLogging)
            //{
            //    if (log_filename == null)
            //    {
            //        var fileName = $"log_{Data.Id}_{SensorKit.MakeSafeFilename(Data.Name)}.txt";
            //        log_filename = PCLStorage.PortablePath.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, "sensors", $"{fileName}");
            //    }
            //    File.AppendAllText(log_filename, received);
            //    return;
            //}
            

            Debug.WriteLine($"CHUNK: {received}");
            if (!String.IsNullOrEmpty(received))
            {
                if (currentPacket == null)
                {
                    string packetStart = ParsePacketStart(received);
                    if (!String.IsNullOrEmpty(packetStart))
                    {
                        Debug.WriteLine($"PACKET START: {packetStart}");
                        currentPacket = packetStart;
                        currentData = "";
                        currentLength = 0;
                    }
                    else
                    {
                        var syncMs = ParseEndSync(received);
                        if (syncMs.HasValue)
                        {
                            Data?.SynchronizeTime(_connectedTime, syncMs.Value);
                            Data?.Save();
                        }
                    }
                }
                else
                {
                    if(currentLength == 0)
                    {
                        var len = ParsePacketLength(received);
                        if(len.HasValue && len.Value > 0)
                        {
                            Debug.WriteLine($"PACKET LENGTH: {len}");
                            currentLength = len.Value;
                            return;
                        }
                    }
                    string packetEnd = ParsePacketEnd(received);
                    if (!String.IsNullOrEmpty(packetEnd))
                    {
                        Debug.WriteLine($"PACKET END: {packetEnd}");
                        if (packetEnd == currentPacket)
                        { // packet complete
                            PacketToSensorData(currentData);
                            Debug.WriteLine($"DATA: {currentData}");
                        }

                        currentPacket = null;
                        currentData = "";
                        currentLength = 0;
                    }
                    else
                    {
                        // keep appending to the buffer
                        currentData += received;
                    }
                }
            }
        }

        private long? ParseEndSync(string s)
        {
            if (!String.IsNullOrEmpty(s) && s.StartsWith(SYNC_TIME))
            {
                var t = s.Substring(SYNC_TIME.Length);
                long result;
                if (long.TryParse(t, out result))
                {
                    return result;
                }
            }
            return null;
        }

        private string ParsePacketStart(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                int start = s.LastIndexOf(PACKET_START); // any RX in the string?
                if (start != -1)
                {
                    return s.Substring(start + PACKET_START.Length);
                }
            }
            return null;
        }

        private string ParsePacketEnd(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                int start = s.LastIndexOf(PACKET_END); // any TX in the string?
                if (start != -1)
                {
                    return s.Substring(start + PACKET_END.Length);
                }
            }
            return null;
        }

        private int? ParsePacketLength(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                int start = s.LastIndexOf(PACKET_LENGTH); // any LX in the string?
                if (start != -1)
                {
                    var len_str = s.Substring(start + PACKET_LENGTH.Length);
                    int len;
                    if(int.TryParse(len_str, out len))
                    {
                        return len;
                    }
                }
            }
            return null;
        }

        private SensorItem PacketToSensorData(string json)
        {
            try
            {
                
                var item = JsonConvert.DeserializeObject<SensorData>(json);
                if (Data != null)
                {
                    if (item?.airdata != null)
                    {
                        if (item.airdata.g != 0) // filter
                        {
                            var reading = new SensorItem
                            {
                                offsetMs = item.airdata.t,
                                itemType = SensorItemTypes.Airtime,
                                duration = item.airdata.dt,
                                altitude = item.airdata.alt,
                                force = item.airdata.g
                            };
                            Data.Append(reading);
                        }
                    }

                    if (item?.turndata != null)
                    {
                        if (item.turndata.g != 0)
                        {
                            var reading = new SensorItem
                            {
                                offsetMs = item.turndata.t,
                                itemType = SensorItemTypes.Turns,
                                duration = item.turndata.dt,
                                radius = item.turndata.r,
                                force = item.turndata.g,
                                forceAvg = item.turndata.ga
                            };
                            Data.Append(reading);
                        }
                    }

                    
                    if (item?.rawdata != null)
                    {
                        var reading = new SensorItem
                        {
                            offsetMs = item.rawdata.t,
                            itemType = SensorItemTypes.Raw,
                            wx = item.rawdata.wx * 0.001f * Math.PI / 180.0f,
                            wy = item.rawdata.wy * 0.001f * Math.PI / 180.0f,
                            wz = item.rawdata.wz * 0.001f * Math.PI / 180.0f,
                            ax = item.rawdata.ax * 0.001f * GRAVITY,
                            ay = item.rawdata.ay * 0.001f * GRAVITY,
                            az = item.rawdata.az * 0.001f * GRAVITY,
                            orientation = item.rawdata.o
                        };

                        Data.Append(reading);
                    }
                    

                    if (item?.summary != null)
                    {
                        var reading = new SensorItem
                        {
                            offsetMs = item.summary.t,
                            itemType = SensorItemTypes.Summary,
                            steps = item.summary.steps,
                            aircount = item.summary.air,
                            airgmax = item.summary.airgmax,
                            airaltmax = item.summary.airaltmax,
                            turns = item.summary.turns,
                            tgmax = item.summary.tgmax,
                            tgavg = item.summary.tgavg,
                            airgavg = item.summary.airgavg,
                            airt = item.summary.airt,
                            turnt = item.summary.turnt
                        };
                        Data.Append(reading);
                    }
                }

            }
            catch(Exception x)
            {
                Debug.WriteLine(x);
            }
            return null;
        }

#endregion


    }
}
