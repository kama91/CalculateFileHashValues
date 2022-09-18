using System;
using System.Threading;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Services;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Common
{
    public static class Extensions
    {
        public static async Task HandlingData<T>(this IDataService<T> service, Action action)
        {
            while (service.Status != ServiceStatus.Completed)
            {
                if (service.DataQueue.IsEmpty)
                {
                    await Task.Delay(100);
                }
                else
                {
                    action.Invoke();
                }                
            }

            action.Invoke();
        }
    }
}
