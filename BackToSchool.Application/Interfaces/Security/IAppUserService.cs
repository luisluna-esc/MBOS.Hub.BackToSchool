using BackToSchool.Application.Dtos.Security;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Security;

namespace BackToSchool.Application.Interfaces.Security;

public interface IAppUserService
{
    Task<ResponseGetObject> GetAllAppUsers(PaginationQueryFilter paginationQueryFilter, AppUserQueryFilter appUserQueryFilter);
    Task<ResponsePost> InsertAppUser(AppUserDto appUserDto);
    Task<ResponsePost> UpdateAppUser(long id, AppUserDto appUserDto);
    Task<ResponsePost> DeleteAppUser(long id);
    Task<ResponsePost> SetPassword(long id, string newPassword);
}