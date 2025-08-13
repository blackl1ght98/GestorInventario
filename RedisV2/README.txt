Redis for Windows - https://github.com/tporadowski/redis
========================================================

This version of Redis (https://redis.io/) is an unofficial port to Windows OS
based on work contributed by Microsoft Open Technologies Inc. It is maintained
by Tomasz Poradowski (tomasz@poradowski.com, http://www.poradowski.com/en/).

Contents of this package:
- *.exe - various Redis for Windows executables compiled for x64 platfrom,
- *.pdb - accompanying PDB files useful for debugging purposes,
- license.txt - license information (BSD-like),
- RELEASENOTES.txt - Windows-specific release notes,
- 00-RELEASENOTES - changelog of original Redis project, those changes are
  ported back to this Windows version.

For more information - please visit https://github.com/tporadowski/redis

If you find this version of Redis useful and would like to support ongoing
development - please consider sponsoring my work at https://github.com/sponsors/tporadowski
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "ConnectionStringLocal": "127.0.0.1:6379"

  },
  "AuthMode": "AsymmetricDynamic", // "Symmetric", "AsymmetricFixed", "AsymmetricDynamic"
  "JwtIssuer": "GestorInvetarioEmisor",
  "JwtAudience": "GestorInventarioCliente",
  "JWT": {
    "PublicKey": "<RSAKeyValue><Modulus>toMqD/PTTEQguc7ttPPqQijikDm9nCHfvz37uY5Qu3bG6bbOtNRnjSAF+nURgL7JqJMvsNdhiUK5ZkD4Enrqku6sNR7kQkgeBlCoQMGii4VcxW5j4tOyhOB1lAUuAjf6cE8P8nvBCVCBzJAH9Spswa1cPxYqo4c70QasM+qAeOD9ynqHP5Kinqw9iMI65Scu/rMt7o/l0mesYYrFnq0MrNKLqfasOZc7Jhe03bFi1WoarS+tuy3QROzOlggegDxq/YcNwMr/b55CL6cJ2zuLutjjVMTGUlNGB0eaimRjtIE9vfOQiJcO/6yrPAz9iRn46rB1Z/ENYk+fWMomm7RveQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
    "PrivateKey": "<RSAKeyValue><Modulus>toMqD/PTTEQguc7ttPPqQijikDm9nCHfvz37uY5Qu3bG6bbOtNRnjSAF+nURgL7JqJMvsNdhiUK5ZkD4Enrqku6sNR7kQkgeBlCoQMGii4VcxW5j4tOyhOB1lAUuAjf6cE8P8nvBCVCBzJAH9Spswa1cPxYqo4c70QasM+qAeOD9ynqHP5Kinqw9iMI65Scu/rMt7o/l0mesYYrFnq0MrNKLqfasOZc7Jhe03bFi1WoarS+tuy3QROzOlggegDxq/YcNwMr/b55CL6cJ2zuLutjjVMTGUlNGB0eaimRjtIE9vfOQiJcO/6yrPAz9iRn46rB1Z/ENYk+fWMomm7RveQ==</Modulus><Exponent>AQAB</Exponent><P>5RiCKFGfJ+ctvH+8Ni4sMCWKinje4S7AwTlWvxPu3FSVU2Wm4UbSvtJSOxG9Zhoel75G9BiIye4cOWg6PgDx0sVgBky9itHhpTwqHO+VIruecSvwIrZP1KuSTLNx12kjxPIFMIB3VWqGkdCw4NVCh4prHhAQmwFzLeRGpnsci4s=</P><Q>y/IulBou4T7j0O0CIPvSNlRYZVgmZJy1c9FnoslZnKkx85hxaCSsFu6NuLwEB9N+JK3Sr4eN9c6zj63gV7Tl23QmD22oBmpwjPTBzkbTGKUVuq7rqnSsrrt9zdhorRxnMZCyjex/qazEiRN2t4kJReLBQP4QmtpvcIJyKLWfYYs=</Q><DP>fB7qLdQDCch7hBwkqaoccL12MQ3Jm3EMJ+Pb9sxi5mbBPJzfbEBF3/LtcGltFwthtc72fDtqqRTjn8qze3JhklMzclZTfwm1WiOdoW3AfD/wWNp4USY7XDrUmc/DBvVE1uhVHXEMtm9vl0LdAgMo92xsGq6TgJepgpyiFoKu9X8=</DP><DQ>YPOxZuCHlrah8GkrUOjFhuRT3WGpZr0EmZlbzgwwGIRqZaX7i4mbcY9YOhDPTbUhy2gCt0UWnFr2C4CaHLe3abruePklHl+tP6T/GQOcSKP6D3QmPjMXAD3LUXbmVB0jhXGHIGbkTZH/IDbrgdaYXOut+SqOVD8xKOgqQuYMbX8=</DQ><InverseQ>oygy0A+0lEF9NC5aKTKDFPV8d+chlb6sx5ZNRmPReKwqG4GVr0loZvaMUmbO/g4niQbQ3aEkRSgIIJa0ndSThyoDOQhxkpP6rFignz0O6zG/c/pBX1eKORM8FNJfrsHqS136IgLIVJsThkkvpchfuEYecO8O3NiJZ2wRztjHAGY=</InverseQ><D>encCl1etVYR1TRhbiksMyj3y4IHOB+D26LUnnnevFkr558LhapcHsLtnJ4q8Jt5eI/43RvsOmHKsQr+fdY8CrXr3FGHZGdyYQPaIH4OOlP30pQmQfpg8NksCukLLf3OeWRPECJofiid6IRAYNtqzxTWVK0OtcrMAYR6QF/nnILP9JMe7kGNf83wgZXv6qBASlIPC2kG1ZVNLF3PoD9F0A0WVW5nKRK48AzHpGs+RTqUP9K5cM5dDYYHPuepiGiSEQSTBSFFxI3yLUwqt4j1kskfm0SeRX2MpDyaOfPZ4SmO4ojdeoWjCo9Qrbb8gJhvXNM6XzMXYwXuM4eDe/oknvQ==</D></RSAKeyValue>"

  },
  "ClaveJWT": "Curso@.net#2023_Arelance_MiClaveSecretaMuyLarga",
  "DataBaseConection": {
    "DBHost": "GUILLERMO\\SQLEXPRESS",
    "DockerDbHost": "SQL-Server-Local",
    "DBName": "GestorInventario",
    "DBUserName": "sa",
    "DBPassword": "SQL#1234"
  },

  "Paypal": {
    "ClientId": "AdrY0_pBH4D4Vkk0q5X3x67Q_quNdemHOATPzVTld4WfRZLWyo4g6YKKIAtYXemZLS8IrsyU1MAGXlpe",
    "ClientSecret": "ECI6CeXhBZx4T72Y6VZHu0KxYYgGDnfb_9EkzfI4BjruYOBUJSHq34SGRTAEmBac5sbKzZeeM3d30u24",
    "Mode": "sandbox",
    "returnUrlSinDocker": "https://localhost:7056/Payment/Success",
    "returnUrlConDocker": "https://localhost:8081/Payment/Success"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "UserName": "fuentesbuenosvinosguillermo@gmail.com",
    "PassWord": "qelh evqa jlei xsbz"
  }


}