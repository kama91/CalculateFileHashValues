namespace CalculateFilesHashCodes.DAL.Interfaces
{
    public interface IDbContext
    {
        void CreateConnectionDb(string dbName);
        void OpenConnection();
        void ClearConnection();
        void ExecuteQuery(string query);
    }
}
