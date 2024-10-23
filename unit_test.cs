using Itau.QA.DominioCorporativo.Core.Entities;
using Itau.QA.DominioCorporativo.Infra.Data.Context;
using Itau.QA.DominioCorporativo.Infra.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Itau.QA.DominioCorporativo.Tests.Util;

namespace Itau.QA.DominioCorporativo.Tests.Repositories
{
    [TestClass]
    public class DominioRepositoryTests
    {
        private Mock<IElasticCacheService> _cacheMock;
        private Mock<MdmDominiosContext> _contextMock;
        private DominioRepository _dominioRepository;

        [TestInitialize]
        public void Setup()
        {
            _cacheMock = new Mock<IElasticCacheService>();

            // Mock DbContextOptions and DbSets
            var options = new DbContextOptionsBuilder<MdmDominiosContext>()
                              .UseInMemoryDatabase(databaseName: "TestDatabase")
                              .Options;
            _contextMock = new Mock<MdmDominiosContext>(options);

            // Mock the DbSet for Dominio
            var dominioDbSetMock = new Mock<DbSet<Dominio>>();
            _contextMock.Setup(c => c.Set<Dominio>()).Returns(dominioDbSetMock.Object);

            // Initialize the repository
            _dominioRepository = new DominioRepository(_contextMock.Object, _cacheMock.Object);
        }


        [TestMethod]
        public async Task ObterDominio_ReturnsFromCache_WhenCacheExists()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1); // Using TestDataUtils
            var expectedDominio = TestDataUtils.CreateDominio(1); // Using TestDataUtils
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync(expectedDominio);

            // Act
            var result = await _dominioRepository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDominio.Codigo, result.Codigo);
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            _contextMock.Verify(c => c.Set<Dominio>(), Times.Never); // Database should not be hit
        }

        [TestMethod]
        public async Task ObterDominio_ReturnsFromDatabase_WhenCacheIsNull()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1); // Using TestDataUtils
            var expectedDominio = TestDataUtils.CreateDominio(1); // Using TestDataUtils
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            var dominioDbSetMock = new Mock<DbSet<Dominio>>();
            _contextMock.Setup(c => c.Set<Dominio>())
                        .Returns(dominioDbSetMock.Object);

            var data = new List<Dominio> { expectedDominio }.AsQueryable();
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Provider).Returns(data.Provider);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Expression).Returns(data.Expression);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.ElementType).Returns(data.ElementType);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            // Act
            var result = await _dominioRepository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDominio.Codigo, result.Codigo);
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            _contextMock.Verify(c => c.Set<Dominio>(), Times.Once); // Database should be hit
        }

        [TestMethod]
        public async Task ObterDominio_InsertsIntoCache_AfterFetchingFromDatabase()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1); // Using TestDataUtils
            var expectedDominio = TestDataUtils.CreateDominio(1); // Using TestDataUtils
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            var dominioDbSetMock = new Mock<DbSet<Dominio>>();
            _contextMock.Setup(c => c.Set<Dominio>())
                        .Returns(dominioDbSetMock.Object);

            var data = new List<Dominio> { expectedDominio }.AsQueryable();
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Provider).Returns(data.Provider);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Expression).Returns(data.Expression);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.ElementType).Returns(data.ElementType);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            // Act
            var result = await _dominioRepository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDominio.Codigo, result.Codigo);
            _cacheMock.Verify(c => c.InserirCache(keyCache, expectedDominio), Times.Once); // Cache should be updated
        }

        [TestMethod]
        public async Task ObterDominio_ReturnsNull_WhenNoCacheAndNoDatabaseRecord()
        {
            // Arrange
            var dominioValor = TestDataUtils.CreateDominioValor(1, 1); // Using TestDataUtils
            var keyCache = JsonSerializer.Serialize(dominioValor);

            _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                      .ReturnsAsync((Dominio)null); // No cache hit

            var dominioDbSetMock = new Mock<DbSet<Dominio>>();
            _contextMock.Setup(c => c.Set<Dominio>())
                        .Returns(dominioDbSetMock.Object);

            var data = new List<Dominio>().AsQueryable(); // No data in the database
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Provider).Returns(data.Provider);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.Expression).Returns(data.Expression);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.ElementType).Returns(data.ElementType);
            dominioDbSetMock.As<IQueryable<Dominio>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            // Act
            var result = await _dominioRepository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNull(result);
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            _cacheMock.Verify(c => c.InserirCache(It.IsAny<string>(), It.IsAny<Dominio>()), Times.Never); // No insert into cache
        }
    }
}
