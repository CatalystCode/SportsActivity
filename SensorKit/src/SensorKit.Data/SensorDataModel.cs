using SensorKitSDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SDCWinter
{
    [DataContract]
    public class SensorDataModel : ViewModel, ISensorModel
    {
        private DataLoader CacheDataLoader { get; set; }
        static string _folder = "sensors";
        static string _file = "sensor";

        // item types for saving
        SensorItemTypes[] saveItemTypes = new SensorItemTypes[] { SensorItemTypes.Airtime, SensorItemTypes.Diving, SensorItemTypes.Turns };

        #region ISensorModel

        public Guid Id
        {
            get
            {
                return Instance.Id;
            }

            set
            {
                Instance.Id = value;
            }
        }

        public string Name
        {
            get
            {
                return Instance.Name;
            }

            set
            {
                Instance.Name = value;
            }
        }

        public string Tag
        {
            get
            {
                return Instance.Tag;
            }

            set
            {
                Instance.Tag = value;
            }
        }

        public bool IsLive
        {
            get
            {
                return Instance.IsLive;
            }

            set
            {
                Instance.IsLive = value;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                return Instance.IsSubscribed;
            }
        }

        public async Task SetLogging(bool isLogging)
        {
            await Instance.SetLogging(isLogging);
        }

        public async Task SetAutoUpdates(bool isAutoUpdates)
        {
            await Instance.SetAutoUpdates(isAutoUpdates);
        }

        public async Task Forget()
        {
            await Instance.Forget();
            await DeleteAsync();
        }

        #endregion

        #region Data

        private string _userId;
        [DataMember]
        public string UserId { get => _userId; set => _userId = value; }

        private ReportingModel _report;
        [IgnoreDataMember]
        public ReportingModel Report
        {
            get
            {
                return _report;
            }
            set
            {
                SetValue(ref _report, value, "Report");
            }
        }

        private SensorModel _sensor;
        [DataMember]
        public SensorModel Instance
        {
            get
            {
                return _sensor;
            }
            set
            {
                SetValue(ref _sensor, value, "Instance");
                Report = new ReportingModel();
                Report.InitSensor(this);
                Instance.ValueChanged += Sensor_ValueChanged;
                //Instance.SegmentsChanged += Sensor_SegmentsChanged;
            }
        }

        #endregion

        #region Measures

        int _airCount = 0;
        [DataMember]
        public int AirCount { get { return _airCount; } set { SetValue(ref _airCount, value, "AirCount"); } }

        int _turnCount = 0;
        [DataMember]
        public int TurnCount { get { return _turnCount; } set { SetValue(ref _turnCount, value, "TurnCount"); } }

        double _maxAltitude = 0.0;
        [DataMember]
        public double MaxAltitude { get { return _maxAltitude; } set { SetValue(ref _maxAltitude, value, "MaxAltitude"); } }

        double _maxDuration = 0.0;
        [DataMember]
        public double MaxDuration { get { return _maxDuration; } set { SetValue(ref _maxDuration, value, "MaxDuration"); } }

        double _maxG = 0.0;
        [DataMember]
        public double MaxG { get { return _maxDuration; } set { SetValue(ref _maxG, value, "MaxG"); } }

        double _totalAltitude = 0.0;
        [DataMember]
        public double TotalAltitude { get { return _totalAltitude; } set { SetValue(ref _totalAltitude, value, "TotalAltitude"); } }

        double _totalDuration = 0.0;
        [DataMember]
        public double TotalDuration { get { return _totalDuration; } set { SetValue(ref _totalDuration, value, "TotalDuration"); } }


        int _steps = 0;
        [DataMember]
        public int steps { get { return _steps; } set { SetValue(ref _steps, value, "steps"); } }

        int _air = 0;
        [DataMember]
        public int air { get { return _air; } set { SetValue(ref _air, value, "air"); } }
        
        double _airgmax = 0.0;
        [DataMember]
        public double airgmax { get { return _airgmax; } set { SetValue(ref _airgmax, value, "airgmax", "AirGMax"); } }

        double _airgavg = 0.0;
        [DataMember]
        public double airgavg { get { return _airgavg; } set { SetValue(ref _airgavg, value, "airgavg", "AirGAvg"); } }

        double _airaltmax = 0.0;
        [DataMember]
        public double airaltmax { get { return _airaltmax; } set { SetValue(ref _airaltmax, value, "airaltmax"); } }

        int _turns = 0;
        [DataMember]
        public int turns { get { return _turns; } set { SetValue(ref _turns, value, "turns"); } }

        double _tgmax = 0.0;
        [DataMember]
        public double tgmax { get { return _tgmax; } set { SetValue(ref _tgmax, value, "tgmax", "TurnGMax"); } }

        double _tgavg = 0.0;
        [DataMember]
        public double tgavg { get { return _tgavg; } set { SetValue(ref _tgavg, value, "tgavg", "TurnGAvg"); } }

        double _travg = 0.0;
        [DataMember]
        public double travg { get { return _travg; } set { SetValue(ref _travg, value, "travg"); } }

        double _intensity = 0.0;
        [DataMember]
        public double intensity { get { return _intensity; } set { SetValue(ref _intensity, value, "intensity", "IntensityG"); } }


        [IgnoreDataMember]
        public double IntensityG
        {
            get
            {
                return MathHelper.ConvertToG(intensity);
            }
        }

        double _stress = 0.0; // percentage
        [DataMember]
        public double stress { get { return _stress; } set { SetValue(ref _stress, value, "stress"); } }

        double _stressAvg = 0.0; // percentage
        [DataMember]
        public double stressAvg { get { return _stressAvg; } set { SetValue(ref _stressAvg, value, "stressAvg"); } }


        [IgnoreDataMember]
        public double AirGMax
        {
            get
            {
                return MathHelper.ConvertToG(airgmax);
            }
        }

        [IgnoreDataMember]
        public double AirGAvg
        {
            get
            {
                return MathHelper.ConvertToG(airgavg);
            }
        }



        [IgnoreDataMember]
        public double TurnGMax
        {
            get
            {
                return MathHelper.ConvertToG(tgmax);
            }
        }

        [IgnoreDataMember]
        public double TurnGAvg
        {
            get
            {
                return MathHelper.ConvertToG(tgavg);
            }
        }

        #endregion

        public SensorDataModel()
        {
            CacheDataLoader = new DataLoader(true); // swallow exceptions
        }

        private void Sensor_ValueChanged(SensorItem newValue)
        {
            if (newValue != null)
            {
                if (newValue.altitude > MaxAltitude)
                {
                    MaxAltitude = newValue.altitude;
                }
                if (newValue.duration > MaxDuration)
                {
                    MaxDuration = newValue.duration;
                }
                if (newValue.force > MaxG)
                {
                    MaxG = newValue.force;
                }
                TotalAltitude += newValue.altitude;
                TotalDuration += newValue.duration;
                intensity += newValue.force * newValue.duration;
                double stress_value = MathHelper.CalculateGStressPercent(newValue.GForce, newValue.duration);
                if(stress_value > stress)
                {
                    stress = stress_value;
                }
                stressAvg = (stressAvg == 0.0)? stress_value : (stressAvg + stress_value) * 0.5;
                if (Instance.History != null)
                {
                    var items = Instance.History.Where(s => s.itemType == newValue.itemType && s.timestamp.Year >= 2017);
                    if(items != null)
                    {
                        if(newValue.itemType == SensorItemTypes.Airtime)
                        {
                            AirCount = items.Count();
                        }else if(newValue.itemType == SensorItemTypes.Turns)
                        {
                            TurnCount = items.Count();
                        }
                    }

                }
            }
            else
            {
                if(Instance?.Summary != null)
                {
                    air = Instance.Summary.air;
                    airaltmax = Instance.Summary.airaltmax;
                    airgmax = Instance.Summary.airgmax;
                    airgavg = Instance.Summary.airgavg;
                    steps = Instance.Summary.steps;
                    tgavg = Instance.Summary.tgavg;
                    tgmax = Instance.Summary.tgmax;
                    travg = Instance.Summary.travg;
                    turns = Instance.Summary.turns;
                }


                // saving the data
                Task.Run(async () =>
                {
                    await SaveAsync();
                    await CommonState.Account.Sensors.SaveAsync();
                    await CommonState.Account.Sensors.UploadMissingSensorDataAsync();
                });
            }

        }

        #region Saving and Loading

        bool isSaving = false;

        public async Task SaveAsync()
        {
            if (isSaving)
                return;

            isSaving = true;

            try
            {
                if (Instance?.History?.Count > 0)
                {
                    var dataByDate = from h in Instance.History
                                     group h by h.timestamp.Date into g
                                     select new { date = g.Key, data = g };

                    foreach (var group in dataByDate)
                    {
                        if (group.date != null && group.data.Count() > 0)
                        {
                            await JsonCache.Set(_folder, $"{_file}_{group.date:yyyyMMdd}_{UserId}_{Instance.Id}", group.data.ToArray());
                        }
                    }

                    string tag = (Instance.Tag == null) ? SensorTagsEnum.Default.ToString() : Instance.Tag.ToString();
                    foreach (var itemType in saveItemTypes)
                    {
                        if (Instance.History.Any(s => s.itemType == itemType))
                        {
                            var items = Instance.History.Where(s => s.itemType == itemType && s.timestamp.Year >= 2017).OrderBy(s=>s.timestamp);
                            var first = Instance.History.First();
                            var fileName = $"exp_{itemType.ToString().ToLower()}_{first.timestamp:yyyyMMddHHmmssfff}_{CommonState.LocationData.CurrentActivity.activityTypeId}_{UserId}_{Instance.Id}_{tag}_{SensorKit.MakeSafeFilename(Instance.Name)}.csv";
                            var filePath = PCLStorage.PortablePath.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, _folder, $"{fileName}");
                            switch (itemType)
                            {
                                case SensorItemTypes.Airtime:
                                    File.AppendAllLines(filePath, items.Select(r => $"{r.timestamp:yyyy-MM-dd HH:mm:ss.fffffffzzz},{r.duration:F8},{r.altitude:F8},{r.force:F8}"));
                                    break;
                                case SensorItemTypes.Turns:
                                    File.AppendAllLines(filePath, items.Select(r => $"{r.timestamp:yyyy-MM-dd HH:mm:ss.fffffffzzz},{r.duration:F8},{r.radius:F8},{r.force:F8},{r.forceAvg:F8}"));
                                    break;
                                case SensorItemTypes.Raw:
                                    File.AppendAllLines(filePath, items.Select(r => $"{r.timestamp:yyyy-MM-dd HH:mm:ss.fffffffzzz},{r.wx:F8},{r.wy:F8},{r.wz:F8},{r.ax:F8},{r.ay:F8},{r.az:F8},{r.orientation}"));
                                    break;
                            }

                            Debug.WriteLine($"SAVED {fileName}");

                        }
                    }
                }
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }

            isSaving = false;
        }

        public async Task LoadLastHistoryAsync()
        {
            try
            {
                if (Instance?.History?.Count == 0)
                {
                    var folderPath = PCLStorage.PortablePath.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, _folder);
                    var files = Directory.GetFiles(folderPath, $"{_file}_????????_{UserId}_{Instance.Id}.json");
                    if(files != null && files.Count() > 0)
                    {
                        var file = files.LastOrDefault(); // assuming they are sorted
                        await CacheDataLoader.LoadAsync(
                           () => JsonCache.GetFromCache<List<SensorItem>>(_folder, $"{_file}_{UserId}_{Instance.Id}"),
                           result =>
                           {
                               if (result != null && result.Count > 0)
                               {
                                   Instance.History = new List<SensorItem>(result);
                               }
                        });
                    }
                    
                }
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }
        }

        public async Task DeleteAsync(SensorItem item)
        {
            try
            {
                if (Instance.History.Contains(item))
                {

                    Instance.History.Remove(item);
                }
                await JsonCache.Set<SensorItem[]>(_folder, $"{_file}_{UserId}_{Instance.Id}", Instance.History.ToArray());
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }
        }

        public async Task DeleteAsync()
        {
            try
            {
                // deletes all historical files
                var folderPath = PCLStorage.PortablePath.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, _folder);
                var files = Directory.GetFiles(folderPath, $"{_file}_????????_{UserId}_{Instance.Id}.json");
                if(files != null && files.Count() > 0)
                {
                    foreach(var f in files)
                    {
                        File.Delete(f);
                    }
                }
                
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }
        }

        #endregion



    }

    
}
