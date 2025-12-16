using DocTask.Core.Models;

namespace DocTask.Core.Interfaces.Repositories;

public interface IUnitRepository
{
    Task<Unit?> GetUnitByIdAsync(int unitId);
    Task<List<Unit>> GetAssignableUnitsAsync(int fromUnitId);
    Task<List<Unit>> GetChildUnitsAsync(int parentUnitId);
    Task<bool> CanAssignToUnitAsync(int fromUnitId, int targetUnitId);
    Task<bool> IsChildUnitAsync(int parentUnitId, int childUnitId);
    Task<List<Unit>> GetParentUnitsAsync(int childUnitId);
}