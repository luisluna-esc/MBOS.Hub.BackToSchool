FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MBOS.Hub.BackToSchool.sln ./
COPY BackToSchool.Web/BackToSchool.Web.csproj BackToSchool.Web/
COPY BackToSchool.Application/BackToSchool.Application.csproj BackToSchool.Application/
COPY BackToSchool.core/BackToSchool.core.csproj BackToSchool.core/
COPY BackToSchool.Infrastructure/BackToSchool.Infrastructure.csproj BackToSchool.Infrastructure/
RUN dotnet restore BackToSchool.Web/BackToSchool.Web.csproj

COPY BackToSchool.Web/ BackToSchool.Web/
COPY BackToSchool.Application/ BackToSchool.Application/
COPY BackToSchool.core/ BackToSchool.core/
COPY BackToSchool.Infrastructure/ BackToSchool.Infrastructure/

RUN dotnet publish BackToSchool.Web/BackToSchool.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BackToSchool.Web.dll"]
