using Application.DTOs.RecurringTransaction;
using Application.Interfaces;
using Application.Mappings;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartBudgetTracker.Tests.Services;
public class RecurringTransactionServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly Mock<IRepository<RecurringTransaction>> _mockRecurringRepo;
    private readonly Mock<IRepository<Account>> _mockAccountRepo;
    private readonly Mock<IRepository<Category>> _mockCategoryRepo;
    private readonly Mock<IRepository<Frequency>> _mockFrequencyRepo;

    private readonly RecurringTransactionService _sut; 
    private const int TestUserId = 1;

    public RecurringTransactionServiceTests()
    {
        var mappingConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        });
        _mapper = mappingConfig.CreateMapper();

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockChangeLogService = new Mock<IChangeLogService>();
        _mockRecurringRepo = new Mock<IRepository<RecurringTransaction>>();
        _mockAccountRepo = new Mock<IRepository<Account>>();
        _mockCategoryRepo = new Mock<IRepository<Category>>();
        _mockFrequencyRepo = new Mock<IRepository<Frequency>>();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(TestUserId);
        _mockUnitOfWork.Setup(uow => uow.Repository<RecurringTransaction>()).Returns(_mockRecurringRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Account>()).Returns(_mockAccountRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Category>()).Returns(_mockCategoryRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Frequency>()).Returns(_mockFrequencyRepo.Object);

        _sut = new RecurringTransactionService(
            _mockUnitOfWork.Object,
            _mapper,
            _mockCurrentUserService.Object,
            _mockChangeLogService.Object
        );
    }

    #region CreateRecurringTransactionAsync Tests

    [Fact]
    public async Task CreateRecurringTransactionAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new UpsertRecurringTransactionRequest
        {
            AccountId = 1,
            CategoryId = 1,
            FrequencyId = 2, // Monthly
            Amount = 500,
            StartDate = DateTime.UtcNow
        };
        var account = new Account { Id = 1, UserId = TestUserId };
        var category = new Category { Id = 1 };
        var frequency = new Frequency { Id = 2, Name = "Monthly" };

        _mockAccountRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Account, bool>>>())).ReturnsAsync(account);
        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>())).ReturnsAsync(category);
        _mockFrequencyRepo.Setup(r => r.GetByIdAsync(request.FrequencyId)).ReturnsAsync(frequency);
        _mockRecurringRepo.Setup(r => r.AddAsync(It.IsAny<RecurringTransaction>())).ReturnsAsync(new RecurringTransaction { Id = 99 });

        // Act
        var result = await _sut.CreateRecurringTransactionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(99);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        _mockRecurringRepo.Verify(r => r.AddAsync(It.Is<RecurringTransaction>(rt => rt.NextDueDate == request.StartDate)), Times.Once);
    }

    [Fact]
    public async Task CreateRecurringTransactionAsync_WhenAccountIsInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpsertRecurringTransactionRequest { AccountId = 99 };
        _mockAccountRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Account, bool>>>())).ReturnsAsync((Account)null);

        // Act
        var result = await _sut.CreateRecurringTransactionAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Message.Should().Be("Account not found.");
    }

    #endregion

    #region UpdateRecurringTransactionAsync Tests

    [Fact]
    public async Task UpdateRecurringTransactionAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new UpsertRecurringTransactionRequest { Amount = 1500, AccountId = 1, CategoryId = 1, FrequencyId = 1, StartDate = DateTime.UtcNow };
        var existingRecurringTx = new RecurringTransaction { Id = 1, UserId = TestUserId, StartDate = DateTime.UtcNow.AddDays(-1) };

        _mockRecurringRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecurringTransaction, bool>>>())).ReturnsAsync(existingRecurringTx);
        _mockAccountRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Account, bool>>>())).ReturnsAsync(new Account { Id = 1 });
        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>())).ReturnsAsync(new Category { Id = 1 });
        _mockFrequencyRepo.Setup(r => r.GetByIdAsync(request.FrequencyId)).ReturnsAsync(new Frequency { Id = 1 });

        // Act
        var result = await _sut.UpdateRecurringTransactionAsync(1, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockRecurringRepo.Verify(r => r.UpdateAsync(It.IsAny<RecurringTransaction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRecurringTransactionAsync_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpsertRecurringTransactionRequest();
        _mockRecurringRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecurringTransaction, bool>>>())).ReturnsAsync((RecurringTransaction)null);

        // Act
        var result = await _sut.UpdateRecurringTransactionAsync(99, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion

    #region DeleteRecurringTransactionAsync Tests

    [Fact]
    public async Task DeleteRecurringTransactionAsync_WhenExists_ShouldSoftDeleteAndSucceed()
    {
        // Arrange
        var recurringTxToDelete = new RecurringTransaction { Id = 1, UserId = TestUserId, IsDeleted = false, IsActive = true };
        _mockRecurringRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecurringTransaction, bool>>>())).ReturnsAsync(recurringTxToDelete);

        // Act
        var result = await _sut.DeleteRecurringTransactionAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRecurringRepo.Verify(r => r.UpdateAsync(It.Is<RecurringTransaction>(rt => rt.IsDeleted == true && rt.IsActive == false)), Times.Once);
    }

    #endregion
}