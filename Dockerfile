FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Устанавливаем переменные окружения для .NET
ENV ASPNETCORE_URLS=http://*:10000
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Устанавливаем русскую локаль (исправленная версия)
RUN apt-get update && \
    apt-get install -y locales && \
    sed -i '/ru_RU.UTF-8/s/^# //' /etc/locale.gen && \
    locale-gen ru_RU.UTF-8

ENV LANG=ru_RU.UTF-8
ENV LANGUAGE=ru_RU:ru
ENV LC_ALL=ru_RU.UTF-8

EXPOSE 10000

ENTRYPOINT ["dotnet", "MyTaskBot.dll"]
