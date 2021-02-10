using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CalculateFilesHashCodes.Common;
using CalculateFilesHashCodes.DAL.Interfaces;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Services;

namespace CalculateFilesHashCodes.DAL
{
    public class DbService : IDataService<object>
    {
        private readonly IDataService<FileNode> _fileHashService;
        private readonly IDbContext _dbContext;

        public StatusService Status { get; set; }
        public ConcurrentQueue<object> DataQueue { get; }
       
        public DbService(
            IDataService<FileNode> fileHashService,
            IDbContext dbContext)
        {
            _fileHashService = fileHashService;
            _dbContext = dbContext;
        }

        public void StartWriteToDb()
        {
            Status = StatusService.Running;
            try
            {
                _dbContext.CreateConnectionDb("HashDb.db");
                _dbContext.OpenConnection();
            }
            catch (Exception ex)
            {
                ErrorService.CurrentErrorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                Console.WriteLine(ex.Message);
            }

            Parallel.Invoke(() =>
            {
                WriteDataToDb();
                WriteErrorToDb();
            });

            Status = StatusService.Complete;
            _dbContext.ClearConnection();
            Console.WriteLine("DbService has finished work");
        }

        public void WriteDataToDb()
        {
            try
            {
               _fileHashService.HandlingData(MakeWriteData);
            }
            catch (Exception ex)
            {
                ErrorService.CurrentErrorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                Console.WriteLine(ex.Message);
            }
        }

        private void MakeWriteData()
        {
            _dbContext.ExecuteQuery(@"CREATE TABLE IF NOT EXISTS [FileNodes] (
                    [Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,    
                    [FileName] text NOT NULL,
                    [HashValue] text NOT NULL);");
            while (_fileHashService.DataQueue.TryDequeue(out var item))
            {
                _dbContext.ExecuteQuery($"INSERT INTO FileNodes (FileName, HashValue) VALUES ('{item.FilePath}', '{item.HashValue}')");
            }
        }

        public void WriteErrorToDb()
        {
            try
            {
               _fileHashService.HandlingData(MakeWriteError);
            }
            catch (Exception ex)
            {
                ErrorService.CurrentErrorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                Console.WriteLine(ex.Message);
            }
        }

        private void MakeWriteError()
        {
            _dbContext.ExecuteQuery(@"CREATE TABLE IF NOT EXISTS [ErrorNodes] (
                    [Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [Info] text NOT NULL);");
            while (ErrorService.CurrentErrorService.DataQueue.TryDequeue(out var errorNode))
            {
                _dbContext.ExecuteQuery($"INSERT INTO ErrorNodes (Info) VALUES ('{errorNode.Info.Replace("'", string.Empty)}')");
            }
        }
    }
}