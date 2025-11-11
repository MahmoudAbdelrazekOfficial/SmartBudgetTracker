using Application.DTOs.Transaction;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
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
public class TransactionServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly Mock<IRepository<Transaction>> _mockTransactionRepo;
    private readonly Mock<IRepository<Account>> _mockAccountRepo;
    private readonly Mock<IRepository<Category>> _mockCategoryRepo;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<INotificationDispatcher> _mockNotificationDispatcher;


    private readonly TransactionService _sut; 
    private const int TestUserId = 1;

    public TransactionServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockChangeLogService = new Mock<IChangeLogService>();
        _mockTransactionRepo = new Mock<IRepository<Transaction>>();
        _mockAccountRepo = new Mock<IRepository<Account>>();
        _mockCategoryRepo = new Mock<IRepository<Category>>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockMemoryCache = new Mock<IMemoryCache>();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(TestUserId);
        _mockUnitOfWork.Setup(uow => uow.Repository<Transaction>()).Returns(_mockTransactionRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Account>()).Returns(_mockAccountRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Category>()).Returns(_mockCategoryRepo.Object);

        _sut = new TransactionService(
        _mockUnitOfWork.Object,
        _mockMapper.Object,
        _mockCurrentUserService.Object,
        _mockChangeLogService.Object,
        _mockWebHostEnvironment.Object,
        _mockHttpContextAccessor.Object,
        _mockMemoryCache.Object,
        _mockNotificationService.Object, 
        _mockNotificationDispatcher.Object
);
    }

    #region CreateTransactionAsync Tests

    [Fact]
    public async Task CreateTransactionAsync_WithValidData_ShouldReturnSuccessResultWithCreatedId()
    {
        // Arrange
        var request = new UpsertTransactionRequest { AccountId = 1, CategoryId = 1, Amount = 100, Type = TransactionType.Expense };
        var account = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 500 };
        var transactionToAdd = new Transaction { AccountId = 1, CategoryId = 1, Amount = 100, Type = TransactionType.Expense };
        var createdTransaction = new Transaction { Id = 99, AccountId = 1, CategoryId = 1, Amount = 100, Type = TransactionType.Expense };

        _mockAccountRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Account, bool>>>())).ReturnsAsync(account);
        _mockMapper.Setup(m => m.Map<Transaction>(request)).Returns(transactionToAdd);
        _mockTransactionRepo.Setup(r => r.AddAsync(transactionToAdd)).ReturnsAsync(createdTransaction);

        // Act
        var result = await _sut.CreateTransactionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(99);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        account.CurrentBalance.Should().Be(400); // 500 - 100
        _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenAccountNotFound_ShouldReturnBadRequestFailure()
    {
        // Arrange
        var request = new UpsertTransactionRequest { AccountId = 99 };
        _mockAccountRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Account, bool>>>())).ReturnsAsync((Account)null);

        // Act
        var result = await _sut.CreateTransactionAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

   

    #region UpdateTransactionAsync Tests

    [Fact]
    public async Task UpdateTransactionAsync_WhenTransactionNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var request = new UpsertTransactionRequest();
        var emptyList = new List<Transaction>().AsQueryable().BuildMock();
        _mockTransactionRepo.Setup(r => r.GetQueryable()).Returns(emptyList);

        // Act
        var result = await _sut.UpdateTransactionAsync(99, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new UpsertTransactionRequest { AccountId = 1, CategoryId = 1, Amount = 50, Type = TransactionType.Expense };
        var account = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 500 };
        var existingTransaction = new Transaction
        {
            Id = 1,
            UserId = TestUserId,
            Amount = 100,
            Type = TransactionType.Expense,
            Account = account,
            AccountId = 1
        };
        var mockQueryable = new List<Transaction> { existingTransaction }.AsQueryable().BuildMock();

        _mockTransactionRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // THIS IS THE FIX: Tell the mock mapper how to behave
        _mockMapper.Setup(m => m.Map(request, existingTransaction))
            .Callback<UpsertTransactionRequest, Transaction>((src, dest) =>
            {
                // Simulate the mapping for the properties your service relies on
                dest.AccountId = src.AccountId;
                dest.CategoryId = src.CategoryId.Value;
                dest.Amount = src.Amount;
                dest.Type = src.Type;
                dest.Description = src.Description;
                dest.TransactionDate = src.TransactionDate;
            });

        // Act
        var result = await _sut.UpdateTransactionAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeTrue(); // This will now pass
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        // Original balance 500. Revert old transaction: 500 + 100 = 600. Apply new transaction: 600 - 50 = 550.
        account.CurrentBalance.Should().Be(550);
        _mockTransactionRepo.Verify(r => r.UpdateAsync(It.IsAny<Transaction>()), Times.Once);
        _mockAccountRepo.Verify(r => r.UpdateAsync(It.IsAny<Account>()), Times.Once);
    }

    #endregion

    #region DeleteTransactionAsync Tests

    [Fact]
    public async Task DeleteTransactionAsync_WhenTransactionNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var emptyList = new List<Transaction>().AsQueryable().BuildMock();
        _mockTransactionRepo.Setup(r => r.GetQueryable()).Returns(emptyList);

        // Act
        var result = await _sut.DeleteTransactionAsync(99);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WithValidTransaction_ShouldSoftDeleteAndReturnSuccess()
    {
        // Arrange
        var account = new Account { Id = 1, UserId = TestUserId, CurrentBalance = 400 };
        var transactionToDelete = new Transaction { Id = 1, UserId = TestUserId, Amount = 100, Type = TransactionType.Expense, Account = account };
        var mockQueryable = new List<Transaction> { transactionToDelete }.AsQueryable().BuildMock();

        _mockTransactionRepo.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _sut.DeleteTransactionAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        account.CurrentBalance.Should().Be(500); // Balance is restored: 400 + 100
        _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        _mockTransactionRepo.Verify(r => r.UpdateAsync(It.Is<Transaction>(t => t.IsDeleted)), Times.Once);
    }

    #endregion
}