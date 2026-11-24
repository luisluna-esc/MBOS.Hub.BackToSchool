using BackToSchool.Application.Dtos.Security;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Security;

namespace BackToSchool.Application.Interfaces.Security;

public interface IPermissionService
{
    Task<ResponseGetObject> GetAllPermissions(PaginationQueryFilter paginationQueryFilter, PermissionQueryFilter permissionQueryFilter);
    Task<ResponsePost> InsertPermission(PermissionDto permissionDto);
    Task<ResponsePost> UpdatePermission(long id, PermissionDto permissionDto);
    Task<ResponsePost> DeletePermission(long id);
}