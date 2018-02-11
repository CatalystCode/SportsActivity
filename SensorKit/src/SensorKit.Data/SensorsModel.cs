using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensorKitSDK;
using System.Runtime.Serialization;
using PCLStorage;
using System.Diagnostics;

namespace SDCWinter
{
    public class SensorsModel : ViewModel
    {
        private DataLoader CacheDataLoader { get; set; }
        static string _folder = "sensors";
        static string _userSensorsFile = "userSensors";

        private string _userId;
       
        private ObservableCollection<SensorDataModel> _userSensors = new ObservableCollection<SensorDataModel>();

        [IgnoreDataMember]
        public static Dictionary<SensorItemTypes, string> sensorItemTypeDict = new Dictionary<SensorItemTypes, string> {
                {SensorItemTypes.Null,"\ue12f"},
                {SensorItemTypes.Airtime,"\ue1d5"} 
        };

        public static string GetSensorItemIconById(SensorItemTypes sensorItemType)
        {
            if (sensorItemTypeDict.ContainsKey(sensorItemType))
                return sensorItemTypeDict[sensorItemType];
            else
                return sensorItemTypeDict[SensorItemTypes.Null];
        }

        /// <summary>
        /// All sensors user used to connect
        /// </summary>
        public ObservableCollection<SensorDataModel> UserSensors
        {
            get { return _userSensors; }
            set
            {
                SetValue(ref _userSensors, value, "UserSensors");
            }
        }

        public IEnumerable<ISensorModel> CombinedSensors
        {
            get
            {
                //return (from hist in UserSensors where !SensorKit.Instance.ExternalSensors.Any(s=> hist.Instance.Name == s.Name)
                //           select hist)
                //           .Union(from online in SensorKit.Instance.ExternalSensors select online);

                return from hist in UserSensors select hist;
            }
        }

        double _stressAvg = 0.0; // percentage
        [IgnoreDataMember]
        public double stressAvg { get { return _stressAvg; } set { SetValue(ref _stressAvg, value, "stressAvg"); } }

        double _stressMax = 0.0; // percentage
        [IgnoreDataMember]
        public double stressMax { get { return _stressMax; } set { SetValue(ref _stressMax, value, "stressMax"); } }

        int _turnCount = 0; // max
        [IgnoreDataMember]
        public int TurnCount { get { return _turnCount; } set { SetValue(ref _turnCount, value, "TurnCount"); } }

        int _airCount = 0; // air
        [IgnoreDataMember]
        public int AirCount { get { return _airCount; } set { SetValue(ref _airCount, value, "AirCount"); } }

        double _tgmax = 0.0;
        [IgnoreDataMember]
        public double tgmax { get { return _tgmax; } set { SetValue(ref _tgmax, value, "tgmax"); } }

        double _tgavg = 0.0;
        [DataMember]
        public double tgavg { get { return _tgavg; } set { SetValue(ref _tgavg, value, "tgavg"); } }

        double _airgmax = 0.0;
        [DataMember]
        public double airgmax { get { return _airgmax; } set { SetValue(ref _airgmax, value, "airgmax"); } }

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


        public SensorsModel(string userId)
        {
            CacheDataLoader = new DataLoader(true); // swallow exceptions
            _userId = userId;
            _userSensorsFile = $"userSensors_{_userId.Substring("Winter:".Length).ToLower()}";
            Report = new ReportingModel();
            Report.InitSensorSummary(this);
        }

        public async Task UpdateOnlineUserSensors()
        {
            // add online sensors to user sensors if they don't exist
            if (SensorKit.Instance?.ExternalSensors != null)
            {
                bool isDirty = false;
                foreach (var onlineSensor in SensorKit.Instance.ExternalSensors)
                {
                    if (!UserSensors.Any(s => s.Instance.Id == onlineSensor.Id))
                    {
                        var onlineModel = new SensorDataModel() { UserId = CommonState.Account.UserProfile.UserIdGuid, Instance = onlineSensor };
                        UserSensors.Add(onlineModel);
                        isDirty = true;
                    }
                }
                if (isDirty)
                {
                    await Task.Run(async () => await SaveAsync());
                }
            }
        }

        public void UpdateCombinedSensorMeasurements()
        {
            foreach(var sensor in UserSensors)
            {
                stressAvg = (stressAvg == 0.0)? sensor.stressAvg : (stressAvg + sensor.stressAvg) * 0.5;
                stressMax = Math.Max(stressMax, sensor.stress);
                TurnCount = Math.Max(TurnCount, sensor.TurnCount);
                AirCount = Math.Max(AirCount, sensor.AirCount);
                tgmax = Math.Max(tgmax, sensor.TurnGMax);
                airgmax = Math.Max(airgmax, sensor.AirGMax);
            }
            OnPropertyChanged("Report");
        }

        public async Task ScanAsync()
        {
            await SensorKit.Instance.StartScanning();
        }

        public async Task Sync(SensorDataModel s)
        {
            await SensorKit.Instance.StopScanning();
            
            await Task.Run(async () =>
            {
                await s.Instance.Subscribe();
            });
        }

        public async Task LoadLocalAsync()
        {
            try
            {
                if (UserSensors?.Count == 0)
                {
                    await CacheDataLoader.LoadAsync(
                       () => JsonCache.GetFromCache<List<SensorDataModel>>(_folder, _userSensorsFile),
                       result =>
                       {
                           if (result != null && result.Count > 0)
                           {
                               UserSensors = new ObservableCollection<SensorDataModel>(result);
                           }
                       });
                }
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }
        }

        public async Task Delete(SensorDataModel sensor)
        {
            await sensor.Forget();
            UserSensors.Remove(sensor);
            await SaveAsync();
        }

        bool isSavingUserSensors = false;

        public async Task SaveAsync()
        {
            try
            {
                if (!isSavingUserSensors && UserSensors != null && UserSensors.Count() > 0)
                {
                    isSavingUserSensors = true;
                    await JsonCache.Set<SensorDataModel[]>(_folder, _userSensorsFile, UserSensors.ToArray());
                }
            }
            catch (Exception x)
            {
                Logger.WriteLine(x);
            }
            isSavingUserSensors = false;
        }

        bool isUploading = false;

        public async Task UploadMissingSensorDataAsync()
        {
            Debug.WriteLine("UPLOADING SENSOR DATA...");

            if (isUploading)
                return;

            isUploading = true;

            foreach (var model in UserSensors)
            {

                if (CommonState.IsOnline)
                {
                    try
                    {
                        var folder = await FileSystem.Current.LocalStorage.GetFolderAsync($"{_folder}");
                        if (folder != null)
                        {
                            var processedFolder = await folder.CreateFolderAsync("processed", PCLStorage.CreationCollisionOption.OpenIfExists);
                            var files = await folder.GetFilesAsync();
                            if (files != null && files.Count() > 0)
                            {
                                foreach (var file in files)
                                {
                                    if (file.Name.EndsWith(".csv"))
                                    {
                                        try
                                        {
                                            if (await SensorKit.UploadAsync(file.Name, file.Path))
                                            {
                                                await file.MoveAsync($"{processedFolder.Path}/{file.Name}", PCLStorage.NameCollisionOption.ReplaceExisting);
                                            }
                                        }
                                        catch (Exception x)
                                        {
                                            Debug.WriteLine(x);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }

            isUploading = false;
        }

        public async Task UploadLoggerData()
        {
            if (CommonState.IsOnline)
            {
                try
                {
                    // PCLStorage.PortablePath.Combine(PCLStorage.FileSystem.Current.LocalStorage.Path, "sensors", $"{fileName}")
                    var folder = await FileSystem.Current.LocalStorage.GetFolderAsync($"{_folder}");
                    if (folder != null)
                    {
                        var processedFolder = await folder.CreateFolderAsync("processed", PCLStorage.CreationCollisionOption.OpenIfExists);
                        var files = await folder.GetFilesAsync();
                        if (files != null && files.Count() > 0)
                        {
                            foreach (var file in files)
                            {
                                if (file.Name.EndsWith(".txt"))
                                {
                                    try
                                    {
                                        if (await SensorKit.UploadAsync(file.Name, file.Path))
                                        {
                                            await file.MoveAsync($"{processedFolder.Path}/{file.Name}", PCLStorage.NameCollisionOption.ReplaceExisting);
                                        }
                                    }
                                    catch (Exception x)
                                    {
                                        Debug.WriteLine(x);
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }




    }
}
