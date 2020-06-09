using System;
using System.Threading;
using CalculateFilesHashCodes.Interfaces;

namespace CalculateFilesHashCodes.Common
{
    public static class Extension
    {
        public static void HandlingData<T>(this IDataService<T> service, Action action)
        {
            while (service.Status != StatusService.Complete)
            {
                if (service.DataQueue.IsEmpty)
                {
                    Thread.Sleep(300);
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
