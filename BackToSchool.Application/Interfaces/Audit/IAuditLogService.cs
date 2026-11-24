using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Audit;
using BackToSchool.core.QueryFilters.Pagination;

namespace BackToSchool.Application.Interfaces.Audit;

public interface IAuditLogService
{
    Task<ResponseGetObject> GetAllAuditLogs(PaginationQueryFilter paginationQueryFilter, AuditLogQueryFilter auditLogQueryFilter);
}