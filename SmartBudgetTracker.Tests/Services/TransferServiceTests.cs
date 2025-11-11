using Application.DTOs.Transfere;
using Application.Interfaces;
using Application.Mappings;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using MockQueryable;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartBudgetTracker.Tests.Services;
public class TransferServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly Mock<IRepository<Transfer>> _mockTransferRepo;
    private readonly Mock<IRepository<Account>> _mockAccountRepo;

    private readonly TransferService _sut; // System Under Test
    private const int TestUserId = 1;

    public TransferServiceTests()
    {
        var mappingConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        });
        _mapper = mappingConfig.CreateMapper();

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockChangeLogService = new Mock<IChangeLogService>();
        _mockTransferRepo = new Mock<IRepository<Transfer>>();
        _mockAccountRepo = new Mock<IRepository<Account>>();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(TestUserId);
        _mockUnitOfWork.Setup(uow => uow.Repository<Transfer>()).Returns(_mockTransferRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Account>()).Returns(_mockAccountRepo.Object);

        _sut = new TransferService(
            _mockUnitOfWork.Object,
            _mapper,
            _mockCurrentUserService.Object,
            _mockChangeLogService.Object
        );
    }

    #region CreateTransferAsync Tests

    [Fact]
    public async Task CreateTransferAsync_WithValidData_ShouldSucceedAndAdjustBalances()
    {
        // Arrange
        var request = new UpsertTransferRequest { FromAccountId = 1, ToAccountId = 2, Amount = 100 };
        var fromAccount = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 1000 };
        var toAccount = new Account { Id = 2, UserId = TestUserId, CurrentBalance = 500 };

        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(fromAccount)))).ReturnsAsync(fromAccount);
        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(toAccount)))).ReturnsAsync(toAccount);
        _mockTransferRepo.Setup(r => r.AddAsync(It.IsAny<Transfer>())).ReturnsAsync(new Transfer { Id = 99 });

        // Act
        var result = await _sut.CreateTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(99);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        fromAccount.CurrentBalance.Should().Be(900); // 1000 - 100
        toAccount.CurrentBalance.Should().Be(600);   // 500 + 100
        _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateTransferAsync_WhenFromAccountNotFound_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpsertTransferRequest { FromAccountId = 99, ToAccountId = 2, Amount = 100 };
        var toAccount = new Account { Id = 2, UserId = TestUserId, CurrentBalance = 500 };

        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(new Account { Id = 99 })))).ReturnsAsync((Account)null);
        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(toAccount)))).ReturnsAsync(toAccount);

        // Act
        var result = await _sut.CreateTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransferAsync_WithSameFromAndToAccount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpsertTransferRequest { FromAccountId = 1, ToAccountId = 1, Amount = 100 };

        // Act
        var result = await _sut.CreateTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Message.Should().Be("Source and destination accounts cannot be the same.");
    }

    #endregion

    #region UpdateTransferAsync Tests

    [Fact]
    public async Task UpdateTransferAsync_WhenTransferNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpsertTransferRequest();
        var emptyList = new List<Transfer>().AsQueryable().BuildMock();
        _mockTransferRepo.Setup(r => r.GetQueryable()).Returns(emptyList);

        // Act
        var result = await _sut.UpdateTransferAsync(99, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransferAsync_WithValidData_ShouldSucceedAndCorrectlyAdjustBalances()
    {
        // Arrange
        var fromAccount = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 900 };
        var toAccount = new Account { Id = 2, UserId = TestUserId, CurrentBalance = 600 };
        var existingTransfer = new Transfer
        {
            Id = 1,
            UserId = TestUserId,
            Amount = 100, // Original Amount
            FromAccount = fromAccount,
            FromAccountId = 1,
            ToAccount = toAccount,
            ToAccountId = 2
        };
        var transfersList = new List<Transfer> { existingTransfer }.AsQueryable().BuildMock();
        _mockTransferRepo.Setup(r => r.GetQueryable()).Returns(transfersList);

        var request = new UpsertTransferRequest { FromAccountId = 1, ToAccountId = 2, Amount = 50 }; // New Amount

        // We need to setup FindAsync because the update logic re-fetches the accounts.
        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(fromAccount)))).ReturnsAsync(fromAccount);
        _mockAccountRepo.Setup(r => r.FindAsync(It.Is<Expression<Func<Account, bool>>>(ex => ex.Compile()(toAccount)))).ReturnsAsync(toAccount);

        // Act
        var result = await _sut.UpdateTransferAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Initial: from=900, to=600.
        // Revert old transfer (+100 to From, -100 to To) -> from=1000, to=500
        // Apply new transfer (-50 from From, +50 to To) -> from=950, to=550
        fromAccount.CurrentBalance.Should().Be(950);
        toAccount.CurrentBalance.Should().Be(550);
    }

    #endregion

    #region DeleteTransferAsync Tests

    [Fact]
    public async Task DeleteTransferAsync_WhenTransferExists_ShouldSucceedAndRevertBalances()
    {
        // Arrange
        var fromAccount = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 900 };
        var toAccount = new Account { Id = 2, UserId = TestUserId, CurrentBalance = 600 };
        var transferToDelete = new Transfer
        {
            Id = 1,
            UserId = TestUserId,
            Amount = 100,
            FromAccount = fromAccount,
            ToAccount = toAccount
        };
        var transfersList = new List<Transfer> { transferToDelete }.AsQueryable().BuildMock();
        _mockTransferRepo.Setup(r => r.GetQueryable()).Returns(transfersList);

        // Act
        var result = await _sut.DeleteTransferAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Revert transfer: +100 to fromAccount, -100 to toAccount
        fromAccount.CurrentBalance.Should().Be(1000);
        toAccount.CurrentBalance.Should().Be(500);
        _mockTransferRepo.Verify(r => r.UpdateAsync(It.Is<Transfer>(t => t.IsDeleted == true)), Times.Once);
    }

    #endregion
}