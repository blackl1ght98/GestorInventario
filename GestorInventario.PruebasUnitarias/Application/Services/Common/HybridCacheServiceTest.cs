using GestorInventario.Application.Services.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using StackExchange.Redis;

namespace GestorInventario.PruebasUnitarias.Application.Services.Common;

public class HybridCacheServiceTest
{
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly IMemoryCache _memoryCache; // real, no mock — ver nota abajo
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly HybridCacheService _sut;

    public HybridCacheServiceTest()
    {
        _distributedCacheMock = new Mock<IDistributedCache>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _redisMock = new Mock<IConnectionMultiplexer>();

        _sut = new HybridCacheService(_distributedCacheMock.Object, _memoryCache, _redisMock.Object);
    }

    [Fact]
    public async Task SetStringAsync_RedisConectado_UsaDistributedCache()
    {
        _redisMock.Setup(r => r.IsConnected).Returns(true);

        await _sut.SetStringAsync("clave", "valor", TimeSpan.FromMinutes(5));

        _distributedCacheMock.Verify(d => d.SetAsync(
            "clave",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetStringAsync_RedisDesconectado_UsaMemoryCache()
    {
        _redisMock.Setup(r => r.IsConnected).Returns(false);

        await _sut.SetStringAsync("clave", "valor");

        _distributedCacheMock.Verify(d => d.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var valorGuardado = await _sut.GetStringAsync("clave");
        Assert.Equal("valor", valorGuardado);
    }

    [Fact]
    public async Task SetStringAsync_RedisEsNull_UsaMemoryCache()
    {
        var sutSinRedis = new HybridCacheService(_distributedCacheMock.Object, _memoryCache, redis: null);

        await sutSinRedis.SetStringAsync("clave2", "valor2");

        _distributedCacheMock.Verify(d => d.SetAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var valorGuardado = await sutSinRedis.GetStringAsync("clave2");
        Assert.Equal("valor2", valorGuardado);
    }

    [Fact]
    public async Task GetStringAsync_RedisConectado_LeeDeDistributedCache()
    {
        _redisMock.Setup(r => r.IsConnected).Returns(true);
        _distributedCacheMock.Setup(d => d.GetAsync("clave", It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("valor-redis"));

        var resultado = await _sut.GetStringAsync("clave");

        Assert.Equal("valor-redis", resultado);
    }

    [Fact]
    public async Task RemoveAsync_RedisConectado_EliminaDeDistributedCache()
    {
        _redisMock.Setup(r => r.IsConnected).Returns(true);

        await _sut.RemoveAsync("clave");

        _distributedCacheMock.Verify(d => d.RemoveAsync("clave", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RedisDesconectado_EliminaDeMemoryCache()
    {
        _redisMock.Setup(r => r.IsConnected).Returns(true);
        await _sut.SetStringAsync("clave3", "valor3"); // redis "activo" en el Setup pero mockeado, no persiste nada real

        _redisMock.Setup(r => r.IsConnected).Returns(false);
        _memoryCache.Set("clave3", "valor3"); // lo guardamos manualmente en memoria para la prueba de remove

        await _sut.RemoveAsync("clave3");

        var resultado = await _sut.GetStringAsync("clave3");
        Assert.Null(resultado);
    }
}