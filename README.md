Build:

dotnet publish uchat-server/uchat-server.csproj -c Release
dotnet publish uchat-client/uchat-client.csproj -c Release

Run:

Запуск сервера (порт обязателен):
.\uchat_server.exe 5000

Запуск клиента (server_ip, port обязательны, client_id опционален):
.\uchat.exe localhost 5000
.\uchat.exe localhost 5000 my_client_id

Development:

Запуск через dotnet (без сборки бинарников):
dotnet run --project uchat-server 5000
dotnet run --project uchat-client localhost 5000

