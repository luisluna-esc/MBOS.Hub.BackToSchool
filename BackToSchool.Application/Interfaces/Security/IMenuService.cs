using BackToSchool.Application.Dtos.Security;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Security;

namespace BackToSchool.Application.Interfaces.Security;

public interface IMenuService
{
    Task<ResponseGetObject> GetAllMenus(PaginationQueryFilter paginationQueryFilter, MenuQueryFilter menuQueryFilter);
    Task<ResponsePost> InsertMenu(MenuDto menuDto);
    Task<ResponsePost> UpdateMenu(long id, MenuDto menuDto);
    Task<ResponsePost> DeleteMenu(long id);
    Task<ResponseGetObject> GetMyMenu();
}