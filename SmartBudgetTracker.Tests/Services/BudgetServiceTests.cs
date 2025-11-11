using Application.DTOs.Budget;
using Application.Interfaces;
using Application.Mappings;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
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
public class BudgetServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper; 
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly Mock<IRepository<Budget>> _mockBudgetRepo;
    private readonly Mock<IRepository<Category>> _mockCategoryRepo;
    private readonly Mock<IRepository<Transaction>> _mockTransactionRepo;

    private readonly BudgetService _sut; 
    private const int TestUserId = 1;

    public BudgetServiceTests()
    {
        var mappingConfig = new MapperConfiguration(cfg => {
            cfg.AddProfile(new MappingProfile());
        });
        _mapper = mappingConfig.CreateMapper();

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockChangeLogService = new Mock<IChangeLogService>();
        _mockBudgetRepo = new Mock<IRepository<Budget>>();
        _mockCategoryRepo = new Mock<IRepository<Category>>();
        _mockTransactionRepo = new Mock<IRepository<Transaction>>();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(TestUserId);
        _mockUnitOfWork.Setup(uow => uow.Repository<Budget>()).Returns(_mockBudgetRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Category>()).Returns(_mockCategoryRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.Repository<Transaction>()).Returns(_mockTransactionRepo.Object);

        _sut = new BudgetService(
            _mockUnitOfWork.Object,
            _mapper,
            _mockCurrentUserService.Object,
            _mockChangeLogService.Object
        );
    }

    [Fact]
    public async Task CreateBudgetAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new UpsertBudgetRequest { CategoryId = 1, Month = 10, Year = 2025, Amount = 1000 };
        var category = new Category { Id = 1, UserId = TestUserId };
        var budgetToCreate = new Budget { CategoryId = 1, Month = 10, Year = 2025, Amount = 1000, UserId = TestUserId };

        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>())).ReturnsAsync(category);
        _mockBudgetRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Budget, bool>>>())).ReturnsAsync((Budget)null); // No duplicates
        _mockBudgetRepo.Setup(r => r.AddAsync(It.IsAny<Budget>())).ReturnsAsync(new Budget { Id = 99 });

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<Budget>(request)).Returns(budgetToCreate);

        var sutWithMockedMapper = new BudgetService(_mockUnitOfWork.Object, mapperMock.Object, _mockCurrentUserService.Object, _mockChangeLogService.Object);

        // Act
        var result = await sutWithMockedMapper.CreateBudgetAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(99);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBudgetAsync_WhenBudgetAlreadyExists_ShouldReturnConflictFailure()
    {
        // Arrange
        var request = new UpsertBudgetRequest { CategoryId = 1, Month = 10, Year = 2025, Amount = 1000 };
        var category = new Category { Id = 1 };
        var existingBudget = new Budget { Id = 1, CategoryId = 1, Month = 10, Year = 2025 };

        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>())).ReturnsAsync(category);
        _mockBudgetRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Budget, bool>>>())).ReturnsAsync(existingBudget); // Duplicate found

        // Act
        var result = await _sut.CreateBudgetAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateBudgetAsync_WhenBudgetNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var request = new UpsertBudgetRequest { Amount = 1200 };
        _mockBudgetRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Budget, bool>>>())).ReturnsAsync((Budget)null);

        // Act
        var result = await _sut.UpdateBudgetAsync(99, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBudgetAsync_WhenBudgetExists_ShouldSoftDeleteAndReturnSuccess()
    {
        // Arrange
        var budgetToDelete = new Budget { Id = 1, UserId = TestUserId, IsDeleted = false };
        _mockBudgetRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Budget, bool>>>())).ReturnsAsync(budgetToDelete);

        // Act
        var result = await _sut.DeleteBudgetAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockBudgetRepo.Verify(r => r.UpdateAsync(It.Is<Budget>(b => b.IsDeleted == true)), Times.Once);
    }

    [Fact]
    public async Task GetBudgetProgressAsync_WithBudgetsAndSpending_ShouldReturnCorrectProgress()
    {
        // Arrange
        var request = new GetBudgetsRequest { Month = 10, Year = 2025 };
        var category1 = new Category { Id = 1, Name = "Groceries" };

        var budgets = new List<Budget>
        {
            new Budget { Id = 1, UserId = TestUserId, CategoryId = 1, Month = 10, Year = 2025, Amount = 1000, Category = category1 }
        }.AsQueryable().BuildMock();

        var transactions = new List<Transaction>
        {
            new Transaction { UserId = TestUserId, CategoryId = 1, Amount = 150, Type = TransactionType.Expense, TransactionDate = new DateTime(2025, 10, 5) },
            new Transaction { UserId = TestUserId, CategoryId = 1, Amount = 50, Type = TransactionType.Expense, TransactionDate = new DateTime(2025, 10, 15) }
        }.AsQueryable().BuildMock();

        _mockBudgetRepo.Setup(r => r.GetQueryable()).Returns(budgets);
        _mockTransactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetBudgetProgressAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var progress = result.Data.First();
        progress.CategoryName.Should().Be("Groceries");
        progress.BudgetedAmount.Should().Be(1000);
        progress.ActualSpending.Should().Be(200); // 150 + 50
        progress.RemainingAmount.Should().Be(800); // 1000 - 200
        progress.IsOverBudget.Should().BeFalse();
    }
}