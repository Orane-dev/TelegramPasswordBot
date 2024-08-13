FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TelegramPasswordBot.csproj", "./"]
RUN dotnet restore "./TelegramPasswordBot.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "TelegramPasswordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TelegramPasswordBot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ./selfsigned.crt /usr/local/share/ca-certificates/certificate.crt
COPY ./certificate.pfx /app/certificate.pfx

RUN update-ca-certificates

ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certificate.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=Qq123456

ENTRYPOINT ["dotnet", "TelegramPasswordBot.dll"]