using System.Collections.Generic;
using System.Threading.Tasks;
using d2.Net.App.Models;

namespace d2.Net.App.Services;

public interface IDiagramService
{
    Task<IEnumerable<Diagram>> GetDiagramsAsync();
    Task<Diagram?> GetDiagramByIdAsync(int diagramId);
    Task CreateDiagramAsync(Diagram diagram);
    Task UpdateDiagramAsync(Diagram diagram);
    Task DeleteDiagramAsync(int diagramId);
}