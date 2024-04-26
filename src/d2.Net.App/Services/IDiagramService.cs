namespace d2.Net.App.Services;

public interface IDiagramService
{
    Task<List<Diagram>> GetDiagramsAsync();
    Task<Diagram> GetDiagramByIdAsync(int diagramId);
    Task CreateDiagramAsync(Diagram diagram);
    Task UpdateDiagramAsync(Diagram diagram);
    Task DeleteDiagramAsync(int diagramId);
}