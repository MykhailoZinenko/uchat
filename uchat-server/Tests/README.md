# Убедитесь что сервер запущен
dotnet run --project uchat-server 5000

# В другом терминале запустите тесты
dotnet test uchat-server --filter "FullyQualifiedName~AuthFlowIntegrationTests"