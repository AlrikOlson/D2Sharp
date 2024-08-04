using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d2.Net.App.Models;

namespace d2.Net.App.Services;

public class DiagramService : IDiagramService
{
    public async Task<IEnumerable<Diagram>> GetDiagramsAsync()
    {
        // TODO: Implement retrieving diagrams from the storage mechanism
        await Task.CompletedTask;
        return new List<Diagram>
        {
            new Diagram { Id = 1, Name = "System Context Diagram", Description = "High-level overview of the system", Level = 1, CreatedAt = DateTime.Now },
            new Diagram { Id = 2, Name = "Container Diagram", Description = "Containers and their interactions", Level = 2, CreatedAt = DateTime.Now },
            new Diagram { Id = 3, Name = "Component Diagram", Description = "Components within each container", Level = 3, CreatedAt = DateTime.Now }
        };
    }

    public async Task<Diagram?> GetDiagramByIdAsync(int diagramId)
    {
        // TODO: Implement retrieving a diagram by its ID from the storage mechanism
        await Task.CompletedTask;
        return new Diagram
        {
            Id = diagramId,
            Name = "Sample Diagram",
            Description = "This is a sample diagram.",
            Level = 1,
            CreatedAt = DateTime.Now,
            Script = "direction: right\nA -> B -> C"
        };
    }

    public async Task CreateDiagramAsync(Diagram diagram)
    {
        // TODO: Implement creating a new diagram in the storage mechanism
        await Task.CompletedTask;
    }

    public async Task UpdateDiagramAsync(Diagram diagram)
    {
        // TODO: Implement updating an existing diagram in the storage mechanism
        await Task.CompletedTask;
    }

    public async Task DeleteDiagramAsync(int diagramId)
    {
        // TODO: Implement deleting a diagram from the storage mechanism
        await Task.CompletedTask;
    }
}