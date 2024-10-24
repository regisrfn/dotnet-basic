using Itau.QA.DominioCorporativo.Core.Entities;
using Itau.QA.DominioCorporativo.Infra.Data.Context;
using Itau.QA.DominioCorporativo.Infra.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Itau.QA.DominioCorporativo.Tests.Util;

namespace Itau.QA.DominioCorporativo.Tests.Repositories
{
    [TestClass]
    public class DominioRepositoryTests
    {
        private Mock<IElasticCacheService> _cacheMock;
        private DbContextOptions<MdmDominiosContext> _dbContextOptions;

        [TestInitialize]
        public void Setup()
        {
            _cacheMock = new Mock<IElasticCacheService>();

            // Create a new in-memory database for each test
            _dbContextOptions = new DbContextOptionsBuilder<MdmDominiosContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;
        }

        [TestMethod]
        public async Task ObterDominio_ReturnsFromCache_WhenCacheExists()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
            var expectedDominio = TestDataUtils.CreateDominio(1);
            var keyCache = JsonSerializer.Serialize(dominioValor);

            // Mock cache to return the expectedDominio
            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync(expectedDominio);

            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                var repository = new DominioRepository(context, _cacheMock.Object);

                // Act
                var result = await repository.ObterDominio(dominioValor);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedDominio.Codigo, result.Codigo);
                _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            }
        }

        [TestMethod]
        public async Task ObterDominio_ReturnsFromDatabase_WhenCacheIsNull()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
            var expectedDominio = TestDataUtils.CreateDominio(1);
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            // Act: Add the expectedDominio to the in-memory database
            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                context.Dominios.Add(expectedDominio);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                var repository = new DominioRepository(context, _cacheMock.Object);
                var result = await repository.ObterDominio(dominioValor);

                Assert.IsNotNull(result);
                Assert.AreEqual(expectedDominio.Codigo, result.Codigo);
                _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            }
        }

        [TestMethod]
        public async Task ObterDominio_InsertsIntoCache_AfterFetchingFromDatabase()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
            var expectedDominio = TestDataUtils.CreateDominio(1);
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            // Act: Add the expectedDominio to the in-memory database
            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                context.Dominios.Add(expectedDominio);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                var repository = new DominioRepository(context, _cacheMock.Object);
                var result = await repository.ObterDominio(dominioValor);

                Assert.IsNotNull(result);
                Assert.AreEqual(expectedDominio.Codigo, result.Codigo);

                // Check that the result was inserted into the cache
                _cacheMock.Verify(c => c.InserirCache(keyCache, expectedDominio), Times.Once);
            }
        }

        [TestMethod]
        public async Task ObterDominio_ReturnsNull_WhenNoCacheAndNoDatabaseRecord()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            // Act & Assert
            using (var context = new MdmDominiosContext(_dbContextOptions))
            {
                var repository = new DominioRepository(context, _cacheMock.Object);
                var result = await repository.ObterDominio(dominioValor);

                Assert.IsNull(result);
                _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
                _cacheMock.Verify(c => c.InserirCache(It.IsAny<string>(), It.IsAny<Dominio>()), Times.Never); // No insert into cache
            }
        }
    }
}
