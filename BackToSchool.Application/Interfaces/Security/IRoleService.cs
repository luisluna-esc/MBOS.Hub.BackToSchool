using BackToSchool.Application.Dtos.Security;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Security;

namespace BackToSchool.Application.Interfaces.Security;

public interface IRoleService
{
    Task<ResponseGetObject> GetAllRoles(PaginationQueryFilter paginationQueryFilter, RoleQueryFilter roleQueryFilter);
    Task<ResponsePost> InsertRole(RoleDto roleDto);
    Task<ResponsePost> UpdateRole(long id, RoleDto roleDto);
    Task<ResponsePost> DeleteRole(long id);
}