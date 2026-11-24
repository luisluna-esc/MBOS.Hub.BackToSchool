using System.Net;
using BackToSchool.Application.Dtos.Audit;
using BackToSchool.Application.Interfaces.Audit;
using BackToSchool.core.Configuration;
using BackToSchool.core.Entities.Audit;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.Interfaces;
using BackToSchool.core.QueryFilters.Audit;
using BackToSchool.core.QueryFilters.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BackToSchool.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;

    public AuditLogService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
    }

    public async Task<ResponseGetObject> GetAllAuditLogs(PaginationQueryFilter paginationQueryFilter, AuditLogQueryFilter auditLogQueryFilter)
    {
        var pageSize = paginationQueryFilter.PageSize > 0 ? paginationQueryFilter.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = paginationQueryFilter.PageNumber > 0 ? paginationQueryFilter.PageNumber : _paginationOptions.InitialPageNumber;

        var query = _unitOfWork.Repository<AuditLog>().Query();

        if (auditLogQueryFilter.AppUserId.HasValue)
        {
            query = query.Where(a => a.AppUserId == auditLogQueryFilter.AppUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(auditLogQueryFilter.TableName))
        {
            query = query.Where(a => a.TableName == auditLogQueryFilter.TableName);
        }

        if (auditLogQueryFilter.RecordId.HasValue)
        {
            query = query.Where(a => a.RecordId == auditLogQueryFilter.RecordId.Value);
        }

        if (!string.IsNullOrWhiteSpace(auditLogQueryFilter.Action))
        {
            query = query.Where(a => a.Action == auditLogQueryFilter.Action);
        }

        var totalRecords = await query.CountAsync();

        var auditLogs = await query
            .OrderByDescending(a => a.LogDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ResponseGetObject
        {
            Data = new PagedResult<AuditLogDto>
            {
                Items = auditLogs.Select(ToDto),
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            Messages = [],
            StatusCode = HttpStatusCode.OK
        };
    }

    private static AuditLogDto ToDto(AuditLog auditLog) => new()
    {
        AuditLogId = auditLog.AuditLogId,
        AppUserId = auditLog.AppUserId,
        TableName = auditLog.TableName,
        RecordId = auditLog.RecordId,
        LogDate = auditLog.LogDate,
        Action = auditLog.Action
    };
}