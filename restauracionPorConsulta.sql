RESTORE HEADERONLY 
FROM DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario.bak';

USE [master];
GO
RESTORE DATABASE [GestorInventario]
FROM DISK = N'C:\Users\Guillermo\Documents\GitHub\GestorInventario\GestorInventario-2026-07-26.bak'
WITH 
    FILE = 21,          -- ← cambia este número por el Position de la última
    REPLACE, 
    STATS = 5;
GO
