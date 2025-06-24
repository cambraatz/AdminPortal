namespace AdminPortal.Server.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<(string? companyName, string message)> GetCompanyNameAsync(string companyKey);
        Task<(string? companyKey, string message)> GetCompanyKeyAsync(string companyName);
        Task<(bool success, string message)> UpdateCompanyAsync(string prevCompanyKey, string newCompanyName);
    }
}
