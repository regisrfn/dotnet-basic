using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TM.ExtratorContInvest.Infra.AWS;

[TestClass]
public class S3GatewayTests
{
    private Mock<IAmazonS3> _mockS3Client;
    private Mock<ILogger<S3Gateway>> _mockLogger;
    private Mock<TransferUtility> _mockTransferUtility;
    private S3Gateway _s3Gateway;

    [TestInitialize]
    public void Setup()
    {
        _mockS3Client = new Mock<IAmazonS3>();
        _mockLogger = new Mock<ILogger<S3Gateway>>();
        _mockTransferUtility = new Mock<TransferUtility>(_mockS3Client.Object);
        _s3Gateway = new S3Gateway(_mockLogger.Object, _mockS3Client.Object, _mockTransferUtility.Object);
    }

    [TestMethod]
    public async Task MultipartUploadAsync_ShouldLogInformation_WhenUploadStarts()
    {
        // Arrange
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "testfile.txt");
        string fileName = "testfile.txt";

        _mockTransferUtility
            .Setup(tu => tu.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await _s3Gateway.MultipartUploadAsync(filePath, fileName);

        // Assert
        _mockLogger.Verify(
            logger => logger.LogInformation(
                It.Is<string>(s => s.Contains("Iniciando a transferência do arquivo")), 
                It.IsAny<object[]>()), 
            Times.Once);
    }

    [TestMethod]
    public async Task MultipartUploadAsync_ShouldLogError_WhenAmazonS3ExceptionOccurs()
    {
        // Arrange
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "testfile.txt");
        string fileName = "testfile.txt";
        var exception = new AmazonS3Exception("S3 error");

        _mockTransferUtility
            .Setup(tu => tu.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), default))
            .ThrowsAsync(exception);

        // Act
        await Assert.ThrowsExceptionAsync<AmazonS3Exception>(() => _s3Gateway.MultipartUploadAsync(filePath, fileName));

        // Assert
        _mockLogger.Verify(
            logger => logger.LogError(exception, It.Is<string>(s => s.Contains("Ocorreu uma exceção na conexão com S3")), It.IsAny<object[]>()),
            Times.Once);
    }

    [TestMethod]
    public async Task MultipartUploadAsync_ShouldLogError_WhenGenericExceptionOccurs()
    {
        // Arrange
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "testfile.txt");
        string fileName = "testfile.txt";
        var exception = new Exception("Generic error");

        _mockTransferUtility
            .Setup(tu => tu.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), default))
            .ThrowsAsync(exception);

        // Act
        await Assert.ThrowsExceptionAsync<Exception>(() => _s3Gateway.MultipartUploadAsync(filePath, fileName));

        // Assert
        _mockLogger.Verify(
            logger => logger.LogError(exception, It.Is<string>(s => s.Contains("Ocorreu uma exceção genérica ao transferir o arquivo para o S3")), It.IsAny<object[]>()),
            Times.Once);
    }
}
