//using Microsoft.ApplicationInsights;
using PCLStorage;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public delegate void SensorModelStopHandler();
    public delegate void ValueChangedHandler(SensorItem newValue);
    
    [DataContract]
    public class SensorModel :  ViewModel, ISensorModel
    {
        public event SensorModelStopHandler Stopped;
        public event ValueChangedHandler ValueChanged;
    
        [DataMember]
        public Guid Id { get; set; }

        
        string _name;
        [DataMember]
        public string Name {
            get
            {
                return _name;
            }
            set
            {
                SetValue(ref _name, value, "Name");
            }
        }

        string _tag;

        [DataMember]
        public string Tag
        {

            get
            {
                return _tag;
            }

            set
            {
                SetValue(ref _tag, value, "Tag");
            }
        }

        bool _isLive;
        [IgnoreDataMember]
        public bool IsLive
        {
            get
            {
                return _isLive;
            }
            set
            {
                SetValue(ref _isLive, value, "IsLive");
            }
        }

        [IgnoreDataMember]
        public IConnector Connector { get; set; }

        SensorInformation _info;
        [DataMember]
        public SensorInformation Information { get { return _info; } set { SetValue(ref _info, value, "Information"); } }

        [IgnoreDataMember]
        public List<SensorItem> History { get; set; } = new List<SensorItem>();

        [IgnoreDataMember]
        public DateTime? LastSync
        {
            get
            {
                if (History != null)
                {
                    var last = History.LastOrDefault(s=>s.itemType == SensorItemTypes.Summary);
                    if (last != null)
                        return last.timestamp;
                }
                return null;
            }
        }

        [IgnoreDataMember]
        public IEnumerable<SensorItem> AirHistory
        {
            get {
                if(History != null)
                {
                    return from h in History where h.itemType == SensorItemTypes.Airtime select h;
                }
                return null;
            }
        }

        [IgnoreDataMember]
        public IEnumerable<SensorItem> TurnHistory
        {
            get
            {
                if (History != null)
                {
                    return from h in History where h.itemType == SensorItemTypes.Turns select h;
                }
                return null;
            }
        }


        [IgnoreDataMember]
        public int Count {
            get {
                if (History != null)
                {
                    return History.Count;
                }
                else
                {
                    return 0;
                }
            }
        } 

        SensorItem _value = new SensorItem();
        [IgnoreDataMember]
        public SensorItem Value { get { return _value; } set { SetValue(ref _value, value, "Value"); } }

        SensorSummaryData _summary = new SensorSummaryData();
        [IgnoreDataMember]
        public SensorSummaryData Summary { get { return _summary; } set { SetValue(ref _summary, value, "Summary"); } }

        [IgnoreDataMember]
        public bool IsSubscribed
        {
            get
            {
                if (Id == default(Guid))
                    return true;
                if (Connector != null)
                    return Connector.IsConnected;
                else
                    return false;
            }
        }

        public async Task SetLogging(bool isLogging)
        {
            if (Connector != null)
            {
                await Connector.SetLogging(isLogging);
            }
                
        }

        public async Task SetAutoUpdates(bool isAutoUpdates)
        {
            if (Connector != null)
            {
                await Connector.SetAutoUpdates(isAutoUpdates);
            }

        }

       
        public SensorModel()
        {
        }

        bool isAppending = false;
        bool isStarting = false;
        bool isStarted = false;

        public void Start()
        {
            try
            {
                if (!isStarting && !isStarted)
                {
                    isStarting = true;
                    isStarted = true;
                }
                isStarting = false;
            }
            catch { }
        }

        public void SynchronizeTime(DateTime appConnectionTime, double connectedDeviceMs)
        {
            try
            {
                var count = History.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = History[i];
                    if (item != null && item.timestamp == DateTime.MinValue) // not sync'd yet and more than the connection time
                    {
                        item.timestamp = appConnectionTime.AddMilliseconds(item.offsetMs - connectedDeviceMs);
                        Debug.WriteLine($"SYNC TIME {item.timestamp} {item.offsetMs}");
                    }
                }

            }
            catch(Exception x)
            {
                Debug.WriteLine(x);
            }
        }


        public void Append(SensorItem e)
        {
            try
            {
                InvokeHelper.Invoke(() =>
                {
                    Value = e;
                    History.Add(e);
                    ValueChanged?.Invoke(e);
                    NotifyPropertyChanged("History");
                });
            }
            catch(Exception x) {
                Debug.WriteLine(x);
            }
        }

        public void Save()
        {
            InvokeHelper.Invoke(() =>
            {
                ValueChanged?.Invoke(null);
            });
        }

        public async Task Forget()
        {
            Stop(); // stop data
                           // unsubscribe from the bluetooth
            if (Connector != null)
            {
                await Connector.Unsubscribe();
                Connector = null;
            }
            // untag the sensor
            Tag = null;
            NotifyPropertyChanged("IsConnected");
        }

        public async Task Subscribe()
        {
            if (Connector == null)
            {
                Connector = new SensorKitConnector(this);
            }
            await Connector.Subscribe();
        }

        public async Task Unsubscribe()
        {
            await Connector?.Unsubscribe();
        }

        public void Stop()
        {
            try
            {
                Save();
                isStarted = false;
                Stopped?.Invoke();
            }
            catch(Exception x) {
                Debug.WriteLine(x);
            }
        }


        
    }
}




