using System;
using System.Threading;
using System.Threading.Tasks;

using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services.Interfaces;

namespace CalculateFilesHashCodes.Common
{
    public static class Extensions
    {
        public static async Task HandlingDataUntilReceivingCompleted<TI, TO>(this IDataService<TI, TO> service, Action action)
        {
            while (service.DataReceivingStatus != DataReceivingStatus.Completed)
            {
                await WaitIfDataIsEmpty(service, action);                
            }

            action.Invoke();
        }

        public static async Task HandlingDataUntilWorkStatusCompleted<TI, TO>(this IDataService<TI, TO> service, Action action)
        {
            while (service.WorkStatus != WorkStatus.Completed)
            {
                await WaitIfDataIsEmpty(service, action);
            }

            action.Invoke();
        }

        private static async Task WaitIfDataIsEmpty<TI, TO>(IDataService<TI, TO> service, Action action)
        {
            if (service.InputData.IsEmpty)
            {
                await Task.Delay(100);
            }
            else
            {
                action.Invoke();
            }
        }
    }
}
