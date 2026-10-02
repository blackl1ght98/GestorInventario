BACKUP DATABASE [GestorInventario]
TO DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\GestorInventario.bak'
WITH 
    FORMAT,          -- sobrescribe el archivo si ya existe
    INIT,            -- empieza un nuevo archivo de backup
    NAME = N'GestorInventario-Full',
    STATS = 10;