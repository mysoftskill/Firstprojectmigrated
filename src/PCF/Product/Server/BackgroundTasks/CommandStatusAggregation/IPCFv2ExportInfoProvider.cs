namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using System;
    using System.Threading.Tasks;

    public interface IPCFv2ExportInfoProvider
    {
        Task<DateTime> GetExpectionWorkerLastestRunTimeAsync();
    }
}
