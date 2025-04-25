using System.Security.Cryptography;

var rsa = new RSACryptoServiceProvider(2048); // 2048 es el tamaño de la clave
Console.WriteLine("Clave privada:");
Console.WriteLine(rsa.ToXmlString(true)); // true para incluir la clave privada en la salida
Console.WriteLine();
Console.WriteLine("Clave pública:");
Console.WriteLine(rsa.ToXmlString(false));
