using Application.DTOs.Account;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartBudgetTracker.Tests.Services;
public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IChangeLogService> _changeLogServiceMock;

    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _changeLogServiceMock = new Mock<IChangeLogService>();

        _accountService = new AccountService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _currentUserServiceMock.Object,
            _changeLogServiceMock.Object);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnAccountDto_WhenAccountExistsAndBelongsToUser()
    {
        // ARRANGE
        var userId = 1;
        var accountId = 101;
        var account = new Account { Id = accountId, UserId = userId, Name = "Test Bank" };
        var accountDto = new AccountDto { Id = accountId, Name = "Test Bank" };

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        var accountRepositoryMock = new Mock<IRepository<Account>>();
        accountRepositoryMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);
        _mapperMock.Setup(m => m.Map<AccountDto>(account)).Returns(accountDto);

        // ACT 
        var result = await _accountService.GetAccountByIdAsync(accountId);

        // ASSERT 
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // ARRANGE
        var userId = 1;
        var nonExistentAccountId = 999;

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);

        var accountRepositoryMock = new Mock<IRepository<Account>>();

        accountRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentAccountId)).ReturnsAsync((Account)null);

        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);

        // ACT 
        var result = await _accountService.GetAccountByIdAsync(nonExistentAccountId);

        // ASSERT 
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Account not found.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnNotFound_WhenAccountExistsButBelongsToAnotherUser()
    {
        // ARRANGE
        var currentUserId = 1;
        var otherUserId = 2;
        var accountId = 101;

        var account = new Account { Id = accountId, UserId = otherUserId, Name = "Other User's Bank" };

        _currentUserServiceMock.Setup(s => s.UserId).Returns(currentUserId);

        var accountRepositoryMock = new Mock<IRepository<Account>>();
        accountRepositoryMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);
        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);

        // ACT 
        var result = await _accountService.GetAccountByIdAsync(accountId);

        // ASSERT 
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldCreateAndReturnAccountId_WhenNameIsUnique()
    {
        // ARRANGE
        var userId = 1;
        var newAccountId = 102;
        var request = new CreateAccountRequest { Name = "New Savings", InitialBalance = 1000, Type = Domain.Enums.AccountType.CreditCard };

        var newAccount = new Account { Id = newAccountId, Name = request.Name, UserId = userId };

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        var accountRepositoryMock = new Mock<IRepository<Account>>();
        accountRepositoryMock.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .ReturnsAsync(new List<Account>());

        _mapperMock.Setup(m => m.Map<Account>(request)).Returns(newAccount);
        accountRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Account>())).ReturnsAsync(newAccount);
        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);

        // ACT
        var result = await _accountService.CreateAccountAsync(request);

        // ASSERT
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(newAccountId, result.Data);
    }

    [Fact]
    public async Task CreateAccountAsync_ShouldReturnBadRequest_WhenAccountNameAlreadyExists()
    {
        // ARRANGE
        var userId = 1;
        var request = new CreateAccountRequest { Name = "Existing Name" };

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);

        var accountRepositoryMock = new Mock<IRepository<Account>>();
        accountRepositoryMock.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .ReturnsAsync(new List<Account> { new Account { Name = "Existing Name" } });
        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);

        // ACT
        var result = await _accountService.CreateAccountAsync(request);

        // ASSERT
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAccountAsync_ShouldSucceed_WhenDataIsValid()
    {
        var userId = 1;
        var accountId = 101;
        var request = new UpdateAccountRequest { Name = "Updated Name" };
        var existingAccount = new Account { Id = accountId, UserId = userId, Name = "Old Name", IsActive = true };

        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);

        var accountRepositoryMock = new Mock<IRepository<Account>>();

        accountRepositoryMock.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(existingAccount);

        accountRepositoryMock.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .ReturnsAsync(new List<Account>());
        _unitOfWorkMock.Setup(uow => uow.Repository<Account>()).Returns(accountRepositoryMock.Object);

        var result = await _accountService.UpdateAccountAsync(accountId, request);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}