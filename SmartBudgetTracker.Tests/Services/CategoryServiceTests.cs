using Application.DTOs.Category;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SmartBudgetTracker.Tests.Services;
public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IChangeLogService> _mockChangeLogService;
    private readonly Mock<IRepository<Category>> _mockCategoryRepo;

    private readonly CategoryService _sut; 

    public CategoryServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockChangeLogService = new Mock<IChangeLogService>();
        _mockCategoryRepo = new Mock<IRepository<Category>>();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(1);
        _mockUnitOfWork.Setup(uow => uow.Repository<Category>()).Returns(_mockCategoryRepo.Object);
        _sut = new CategoryService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockCurrentUserService.Object,
            _mockChangeLogService.Object
        );
    }

    [Fact]
    public async Task CreateCategoryAsync_WithValidData_ShouldReturnSuccessResultWithCreatedId()
    {
        var request = new CreateCategoryRequest { Name = "New Test", Type = TransactionType.Expense };
        var categoryToAdd = new Category { Name = "New Test", Type = TransactionType.Expense, UserId = 1 };
        var createdCategory = new Category { Id = 10, Name = "New Test", Type = TransactionType.Expense, UserId = 1 };

        _mockCategoryRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(new List<Category>());

        _mockMapper.Setup(m => m.Map<Category>(request)).Returns(categoryToAdd);
        _mockCategoryRepo.Setup(r => r.AddAsync(categoryToAdd)).ReturnsAsync(createdCategory);

        var result = await _sut.CreateCategoryAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(10);
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCategoryAsync_WithDuplicateName_ShouldReturnBadRequestFailure()
    {
        var request = new CreateCategoryRequest { Name = "Duplicate", Type = TransactionType.Expense };
        var existingCategory = new List<Category> { new Category { Id = 1, Name = "Duplicate", Type = TransactionType.Expense, UserId = 1 } };

        _mockCategoryRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(existingCategory);

        var result = await _sut.CreateCategoryAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithNonExistentCategory_ShouldReturnNotFoundFailure()
    {
        var request = new UpdateCategoryRequest { Name = "Updated Name" };

        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync((Category)null);

        var result = await _sut.UpdateCategoryAsync(99, request); 

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenUserDoesNotOwnCategory_ShouldReturnForbiddenFailure()
    {
        var categoryToDelete = new Category { Id = 5, Name = "Another User's Category", UserId = 2 }; // UserId = 2 (مستخدم آخر)

        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(categoryToDelete);

        var result = await _sut.DeleteCategoryAsync(5);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryHasSubCategories_ShouldReturnBadRequestFailure()
    {
        var categoryToDelete = new Category { Id = 1, Name = "Parent Category", UserId = 1 };
        var subCategories = new List<Category> { new Category { Id = 2, Name = "Sub", ParentCategoryId = 1 } };

        _mockCategoryRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>()))
            .ReturnsAsync(categoryToDelete);

        _mockCategoryRepo.Setup(r => r.GetAsync(c => c.ParentCategoryId == 1 && !c.IsDeleted))
            .ReturnsAsync(subCategories);

        var result = await _sut.DeleteCategoryAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        result.Message.Should().Contain("sub-categories");
    }
}

