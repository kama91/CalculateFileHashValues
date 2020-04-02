using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CalculateFilesHashCodes.Interfaces;
using CalculateFilesHashCodes.Models;
using CalculateFilesHashCodes.Utils;

namespace CalculateFilesHashCodes.Services
{
    public class FileHashService : IDataService<FileNode>
    {
        private readonly IDataService<string> _fileScannerService;

        public StatusService Status { get; set; }
        public ConcurrentQueue<FileNode> DataQueue { get; } = new ConcurrentQueue<FileNode>();
        
        public FileHashService(IDataService<string> fileScannerService)
        {
            _fileScannerService = fileScannerService;
        }

        public Task StartCalculate(string typeHashSum = "md5")
        {
            return Task.Run(() => CalculateHashCodeAndAddToQueue(typeHashSum));
        }

        public void CalculateHashCodeAndAddToQueue(string typeHashSum)
        {
            Status = StatusService.Running;

            _fileScannerService.HandlingData(() => AddHashToQueue(typeHashSum));

            Status = StatusService.Complete;
            Console.WriteLine("FileHashService has finished work");
        }

        private void AddHashToQueue(string typeHashSum)
        {
            while (_fileScannerService.DataQueue.TryDequeue(out var filePath))
            {
                try
                {
                    DataQueue.Enqueue(new FileNode(filePath, BitConverter.ToString(GetHashCode(filePath, typeHashSum))));
                }
                catch (Exception ex)
                {
                    ErrorService.CurrentErrorService.DataQueue.Enqueue(new ErrorNode {Info = ex.Source + ex.Message + ex.StackTrace});
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static byte[] GetHashCode(string filePath, string typeHashSum)
        {
            using var fileStream = File.OpenRead(filePath);
            byte[] hashValue;
            if (!Enum.TryParse(typeof(HashCodeAlgorithmEnum), typeHashSum, true, out var hashAlgorithm)) return null;
            switch (hashAlgorithm)
            {
                case HashCodeAlgorithmEnum.Md5:
                    hashValue = MD5.Create().ComputeHash(fileStream);
                    break;
                case HashCodeAlgorithmEnum.Sha1:
                    hashValue = SHA1.Create().ComputeHash(fileStream);
                    break;
                case HashCodeAlgorithmEnum.Sha256:
                    hashValue = SHA256.Create().ComputeHash(fileStream);
                    break;
                case HashCodeAlgorithmEnum.Sha384:
                    hashValue = SHA384.Create().ComputeHash(fileStream);
                    break;
                case HashCodeAlgorithmEnum.Sha512:
                    hashValue = SHA512.Create().ComputeHash(fileStream);
                    break;
                case HashCodeAlgorithmEnum.Base64:
                    var binaryData = new byte[fileStream.Length];
                    long bytesRead = fileStream.Read(binaryData, 0, (int) fileStream.Length);

                    if (bytesRead != fileStream.Length)
                        throw new Exception($"Number of bytes read ({bytesRead}) does not match file size ({fileStream.Length}).");

                    var base64String = Convert.ToBase64String(binaryData, 0, binaryData.Length);
                    Console.WriteLine($"File: {fileStream.Name} - BASE64 Hash: {base64String}");
                    hashValue = null;
                    break;
                default:
                    ShowHelp();
                    return null;
            }

            return hashValue;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Use one of the following algorithms\t\tBASE64\n\t\tMD5\n\t\tSHA1\n\t\tSHA256\n\t\tSHA384\n\t\tSHA512\n");
        }
    }
}
