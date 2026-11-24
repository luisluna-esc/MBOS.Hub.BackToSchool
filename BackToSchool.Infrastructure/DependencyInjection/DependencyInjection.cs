using BackToSchool.Application.Dtos.Security;
using BackToSchool.Application.Interfaces.Audit;
using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.Application.Interfaces.Security;
using BackToSchool.Application.Services;
using BackToSchool.Application.Validators.Security;
using BackToSchool.core.Configuration;
using BackToSchool.core.Interfaces;
using BackToSchool.Infrastructure.Authorization;
using BackToSchool.Infrastructure.Context;
using BackToSchool.Infrastructure.Context.Core;
using BackToSchool.Infrastructure.Interceptors;
using BackToSchool.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BackToSchool.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Missing connection string 'DefaultConnection' in appsettings.");

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                   .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.Configure<PaginationOptions>(configuration.GetSection("Pagination"));
        services.AddScoped(sp => sp.GetRequiredService<IOptions<PaginationOptions>>().Value);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Seguridad (igual que en los demás sistemas M-BOS)
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IValidator<RoleDto>, RoleValidator>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IValidator<PermissionDto>, PermissionValidator>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IValidator<AppUserDto>, AppUserValidator>();
        services.AddScoped<IAppUserRoleService, AppUserRoleService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IValidator<MenuDto>, MenuValidator>();
        services.AddScoped<IRoleMenuService, RoleMenuService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // BackToSchool
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ISaleOrderService, SaleOrderService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
