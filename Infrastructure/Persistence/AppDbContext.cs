using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Frequency> Frequencies { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionSplit> TransactionSplits { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TransactionTag> TransactionTags { get; set; }
    public DbSet<TransactionAttachment> TransactionAttachments { get; set; }
    public DbSet<Transfer> Transfers { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<RecurringTransaction> RecurringTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Frequency>().HasData(
            new Frequency { Id = 1, Name = "Weekly" },
            new Frequency { Id = 2, Name = "Monthly" },
            new Frequency { Id = 3, Name = "Yearly" }
        );
     
        //Defualt data for categories
        modelBuilder.Entity<Category>().HasData(
        new Category
        {
            Id = 1,
            Name = "Food", 
            Type = TransactionType.Expense,
            Icon = "fas fa-utensils" 
        },
        new Category
        {
            Id = 2,
            Name = "Transportation", 
            Type = TransactionType.Expense,
            Icon = "fas fa-bus"
        },
        new Category
        {
            Id = 3,
            Name = "Salary", 
            Type = TransactionType.Income,
            Icon = "fas fa-money-bill-wave"
        },
        new Category
        {
            Id = 4,
            Name = "Shopping", 
            Type = TransactionType.Expense,
            Icon = "fas fa-shopping-cart"
        }
    );
        modelBuilder.Entity<Transaction>()
        .HasOne(t => t.RecurringTransaction)
        .WithMany()
        .HasForeignKey(t => t.RecurringTransactionId)
        .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RecurringTransaction>()
        .HasOne(rt => rt.User)
        .WithMany()
        .HasForeignKey(rt => rt.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    }


}