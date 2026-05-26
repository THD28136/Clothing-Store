FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["MTKPM_Clothing_Store_web.csproj", "./"]

RUN dotnet restore "MTKPM_Clothing_Store_web.csproj"

COPY . .

RUN dotnet publish "MTKPM_Clothing_Store_web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "MTKPM_Clothing_Store_web.dll"]