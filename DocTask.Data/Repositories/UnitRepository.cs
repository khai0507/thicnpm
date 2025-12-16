using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocTask.Data.Repositories;

public class UnitRepository : IUnitRepository
{
    private readonly ApplicationDbContext _context;

    public UnitRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit?> GetUnitByIdAsync(int unitId)
    {
        return await _context.Units
            .Include(u => u.Org)
            .Include(u => u.Users)
            .FirstOrDefaultAsync(u => u.UnitId == unitId);
    }

    public async Task<List<Unit>> GetAssignableUnitsAsync(int fromUnitId)
    {
        var assignableUnits = new List<Unit>();
        
        // Lấy đơn vị hiện tại
        var currentUnit = await GetUnitByIdAsync(fromUnitId);
        if (currentUnit == null) return assignableUnits;

        // Nếu là unit cha (UnitParent = null)
        if (currentUnit.UnitParent == null)
        {
            // Unit cha có thể giao cho:
            // 1. Các unit cha khác
            var otherParentUnits = await _context.Units
                .Include(u => u.Org)
                .Where(u => u.UnitParent == null && u.UnitId != fromUnitId)
                .ToListAsync();
            assignableUnits.AddRange(otherParentUnits);

            // 2. Tất cả đơn vị con của mình (đệ quy)
            var childUnits = await GetChildUnitsAsync(fromUnitId);
            assignableUnits.AddRange(childUnits);

            // Lấy tất cả đơn vị con của các đơn vị con (đệ quy)
            foreach (var child in childUnits)
            {
                var grandChildren = await GetAssignableUnitsAsync(child.UnitId);
                assignableUnits.AddRange(grandChildren);
            }
        }
        else
        {
            // Nếu là unit con
            // Chỉ có thể giao cho các đơn vị con khác có cùng cha
            var siblingUnits = await _context.Units
                .Include(u => u.Org)
                .Where(u => u.UnitParent == currentUnit.UnitParent && u.UnitId != fromUnitId)
                .ToListAsync();
            assignableUnits.AddRange(siblingUnits);
        }

        return assignableUnits.Distinct().ToList();
    }

    public async Task<List<Unit>> GetChildUnitsAsync(int parentUnitId)
    {
        return await _context.Units
            .Include(u => u.Org)
            .Where(u => u.UnitParent == parentUnitId)
            .ToListAsync();
    }

    public async Task<bool> CanAssignToUnitAsync(int fromUnitId, int targetUnitId)
    {
        var fromUnit = await GetUnitByIdAsync(fromUnitId);
        var targetUnit = await GetUnitByIdAsync(targetUnitId);
        
        if (fromUnit == null || targetUnit == null) return false;
        if (fromUnitId == targetUnitId) return false; // Không thể giao cho chính mình
        
        // Nếu đơn vị giao là unit cha (UnitParent = null)
        if (fromUnit.UnitParent == null)
        {
            // Unit cha có thể giao cho:
            // 1. Các unit cha khác
            if (targetUnit.UnitParent == null)
            {
                return true;
            }
            
            // 2. Tất cả đơn vị con của mình (đệ quy)
            return await IsChildUnitAsync(fromUnitId, targetUnitId);
        }
        else
        {
            // Nếu đơn vị giao là unit con
            // Chỉ có thể giao cho các đơn vị con khác có cùng cha
            return fromUnit.UnitParent == targetUnit.UnitParent;
        }
    }

    public async Task<bool> IsChildUnitAsync(int parentUnitId, int childUnitId)
    {
        var childUnit = await GetUnitByIdAsync(childUnitId);
        if (childUnit == null) return false;
        
        // Kiểm tra trực tiếp
        if (childUnit.UnitParent == parentUnitId) return true;
        
        // Kiểm tra đệ quy
        if (childUnit.UnitParent != null)
        {
            return await IsChildUnitAsync(parentUnitId, childUnit.UnitParent.Value);
        }
        
        return false;
    }

    public async Task<List<Unit>> GetParentUnitsAsync(int childUnitId)
    {
        var parents = new List<Unit>();
        var currentUnit = await GetUnitByIdAsync(childUnitId);
        
        while (currentUnit?.UnitParent != null)
        {
            var parent = await GetUnitByIdAsync(currentUnit.UnitParent.Value);
            if (parent != null)
            {
                parents.Add(parent);
                currentUnit = parent;
            }
            else
            {
                break;
            }
        }
        
        return parents;
    }

    // Các method không cần thiết - để trống để implement interface
    public Task<List<Unit>> GetUnitsByOrgIdAsync(int orgId) => throw new NotImplementedException();
    public Task<bool> IsParentUnitAsync(int childUnitId, int parentUnitId) => throw new NotImplementedException();
    public Task<List<Unit>> GetUnitsInHierarchyAsync(int unitId) => throw new NotImplementedException();
    public Task<Unit?> CreateUnitAsync(Unit unit) => throw new NotImplementedException();
    public Task<Unit?> UpdateUnitAsync(int unitId, Unit unit) => throw new NotImplementedException();
    public Task<bool> DeleteUnitAsync(int unitId) => throw new NotImplementedException();
}