@echo off
REM Atajo para dotnet-ef sobre la solución Donations.
REM   ef.cmd add <NombreMigracion>   -> dotnet ef migrations add
REM   ef.cmd remove                  -> dotnet ef migrations remove
REM   ef.cmd update                  -> dotnet ef database update

set PROJECT=Donations.Persistence
set STARTUP=Donations.Api

if "%1"=="add" (
    dotnet ef migrations add %2 --project %PROJECT% --startup-project %STARTUP% --output-dir Migrations
) else if "%1"=="remove" (
    dotnet ef migrations remove --project %PROJECT% --startup-project %STARTUP%
) else if "%1"=="update" (
    dotnet ef database update --project %PROJECT% --startup-project %STARTUP%
) else (
    echo Uso: ef.cmd [add ^<Nombre^> ^| remove ^| update]
)
