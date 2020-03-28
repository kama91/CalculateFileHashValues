namespace CalculateFilesHashCodes.Interfaces
{
    public interface IDbOperation
    {
        void CreateConnectionDb(string dbName);
        void OpenConnection();
        void ClearConnection();
        void ExecuteCommand(string textCommand);

    }
}
