using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Utils;
using Microsoft.EntityFrameworkCore;

namespace CalculateFilesHashCodes.Services
{
    public class DbService : IDataService<object>
    {
        private readonly IDataService<FileNode> _fileHashService;
        private readonly HashCodeDbContext _hashSumDbContext;
        private readonly SqLiteDbOperation _dbOperation;

        public StatusService Status { get; set; }
        public ConcurrentQueue<object> DataQueue { get; }
       
        public DbService(IDataService<FileNode> fileHashService, DbContext hashSumDbContext)
        {
            _fileHashService = fileHashService;
            _hashSumDbContext = hashSumDbContext as HashCodeDbContext;
        }

        public DbService(IDataService<FileNode> fileHashService, SqLiteDbOperation dbOperation)
        {
            _fileHashService = fileHashService;
            _dbOperation = dbOperation;
        }

        public void StartWriteToDb()
        {
            Status = StatusService.Running;
            try
            {
                _dbOperation.CreateConnectionDb("HashDb.db");
                _dbOperation.OpenConnection();
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
            _dbOperation.ClearConnection();
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
            _dbOperation.ExecuteCommand(@"CREATE TABLE IF NOT EXISTS [FileNodes] (
                    [Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,    
                    [FileName] text NOT NULL,
                    [HashValue] text NOT NULL);");
            while (_fileHashService.DataQueue.TryDequeue(out var item))
            {
                _dbOperation.ExecuteCommand($"INSERT INTO FileNodes (FileName, HashValue) VALUES ('{item.FilePath}', '{item.HashValue}')");
                //_hashSumDbContext.FileNodes.Add(item);
                //_hashSumDbContext.SaveChanges(); it's slooooooow...
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
            _dbOperation.ExecuteCommand(@"CREATE TABLE IF NOT EXISTS [ErrorNodes] (
                    [Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [Info] text NOT NULL);");
            while (ErrorService.CurrentErrorService.DataQueue.TryDequeue(out var errorNode))
            {
                _dbOperation.ExecuteCommand($"INSERT INTO ErrorNodes (Info) VALUES ('{errorNode.Info.Replace("'", string.Empty)}')");
                //_hashSumDbContext.ErrorNodes.Add(errorNode);
                //_hashSumDbContext.SaveChanges();
            }
        }
    }
}