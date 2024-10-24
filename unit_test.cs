[TestClass]
public class DominioRepositoryTests
{
    private Mock<IElasticCacheService> _cacheMock;
    private DbContextOptions<MdmDominiosContext> _dbContextOptions;

    // This will hold the Dominio that we add to the in-memory database
    private Dominio _expectedDominio;

    [TestInitialize]
    public async Task Setup()
    {
        _cacheMock = new Mock<IElasticCacheService>();

        // Create a new in-memory database for each test
        _dbContextOptions = new DbContextOptionsBuilder<MdmDominiosContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
            .Options;

        // Create the data to be used in the tests
        _expectedDominio = TestDataUtils.CreateDominio(1); // Create sample data

        // Act: Add the expectedDominio to the in-memory database
        using (var context = new MdmDominiosContext(_dbContextOptions))
        {
            context.Dominios.Add(_expectedDominio);
            await context.SaveChangesAsync(); // Make sure the data is saved in the in-memory database
        }
    }

    [TestMethod]
    public async Task ObterDominio_ReturnsFromCache_WhenCacheExists()
    {
        // Arrange
        var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
        var keyCache = JsonSerializer.Serialize(dominioValor);

        // Mock cache to return the expectedDominio
        _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                  .ReturnsAsync(_expectedDominio); // Returning data from cache

        // Act: Use the repository to get the data
        using (var context = new MdmDominiosContext(_dbContextOptions))
        {
            var repository = new DominioRepository(context, _cacheMock.Object);
            var result = await repository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_expectedDominio.Codigo, result.Codigo);
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
        }
    }

    [TestMethod]
    public async Task ObterDominio_ReturnsFromDatabase_WhenCacheIsNull()
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

            Assert.IsNotNull(result); // Data should be found in the database
            Assert.AreEqual(_expectedDominio.Codigo, result.Codigo);
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
        }
    }

    [TestMethod]
    public async Task ObterDominio_InsertsIntoCache_AfterFetchingFromDatabase()
    {
        // Arrange
        var dominioValor = TestDataUtils.CreateDominioValor(1, 1);
        var keyCache = JsonSerializer.Serialize(dominioValor);

        _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                  .ReturnsAsync((Dominio)null); // No cache hit

        // Act
        using (var context = new MdmDominiosContext(_dbContextOptions))
        {
            var repository = new DominioRepository(context, _cacheMock.Object);
            var result = await repository.ObterDominio(dominioValor);

            // Assert
            Assert.IsNotNull(result); // Data found in the database
            Assert.AreEqual(_expectedDominio.Codigo, result.Codigo);

            // Check that the result was inserted into the cache
            _cacheMock.Verify(c => c.InserirCache(keyCache, _expectedDominio), Times.Once);
        }
    }

    [TestMethod]
    public async Task ObterDominio_ReturnsNull_WhenNoCacheAndNoDatabaseRecord()
    {
        // Arrange
        var dominioValor = TestDataUtils.CreateDominioValor(2, 2); // Use a different value that doesn't exist
        var keyCache = JsonSerializer.Serialize(dominioValor);

        _cacheMock.Setup(c => c.ObterCache<Dominio>(keyCache))
                  .ReturnsAsync((Dominio)null); // No cache hit

        // Act & Assert
        using (var context = new MdmDominiosContext(_dbContextOptions))
        {
            var repository = new DominioRepository(context, _cacheMock.Object);
            var result = await repository.ObterDominio(dominioValor);

            Assert.IsNull(result); // No data should be found
            _cacheMock.Verify(c => c.ObterCache<Dominio>(keyCache), Times.Once);
            _cacheMock.Verify(c => c.InserirCache(It.IsAny<string>(), It.IsAny<Dominio>()), Times.Never); // No insert into cache
        }
    }
}
