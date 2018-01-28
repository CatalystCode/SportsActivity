using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SensorKitSDK
{
    public interface ISensorModel
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Tag { get; set; }
        bool IsLive { get; set; }
        bool IsSubscribed { get; }
        Task SetLogging(bool isLogging);
        Task SetAutoUpdates(bool isAutoUpdates);

    }
}
