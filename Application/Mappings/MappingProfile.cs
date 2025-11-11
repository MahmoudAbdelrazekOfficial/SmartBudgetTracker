using Application.DTOs.Account;
using Application.DTOs.Budget;
using Application.DTOs.Category;
using Application.DTOs.Frequency;
using Application.DTOs.Notification;
using Application.DTOs.Profile;
using Application.DTOs.RecurringTransaction;
using Application.DTOs.Tag;
using Application.DTOs.Transaction;
using Application.DTOs.Transfere;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.Mappings;
public class MappingProfile: Profile
{
    public MappingProfile()
    {
        //Acount
        CreateMap<CreateAccountRequest, Account>()
            .ForMember(dest => dest.CurrentBalance, opt => opt.MapFrom(src => src.InitialBalance));
        CreateMap<UpdateAccountRequest, Account>();
        CreateMap<Account, AccountDto>();

        //Freqeuncy
        CreateMap<Frequency, FrequencyDto>();
        CreateMap<UpsertFrequencyRequest, Frequency>();

        //Profile

        CreateMap<ApplicationUser, ProfileDto>();
        CreateMap<UpdateProfileRequest, ApplicationUser>();

        //Category
        CreateMap<CreateCategoryRequest, Category>();
        CreateMap<UpdateCategoryRequest, Category>();
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.HasSubCategories, opt => opt.MapFrom(src => src.SubCategories.Any(sc => !sc.IsDeleted)));

        CreateMap<Category, CategoryDetailsDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory.Name))
            .ForMember(dest => dest.SubCategories, opt => opt.MapFrom(src => src.SubCategories.Where(sc => !sc.IsDeleted)));

        CreateMap<Category, CategoryLookupDto>();

        //Tag
        CreateMap<Tag, TagDto>();
        CreateMap<UpsertTagRequest, Tag>();

        //Transaction
        CreateMap<UpsertTransactionRequest, Transaction>()
            .ForMember(dest => dest.Splits, opt => opt.MapFrom(src => src.Splits));

        CreateMap<UpsertTransactionSplitRequest, TransactionSplit>();

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
            .ForMember(dest => dest.TagIds, opt => opt.MapFrom(src => src.TransactionTags.Select(tt => tt.TagId).ToList()))
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments)); 

        CreateMap<TransactionSplit, TransactionSplitDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

        CreateMap<TransactionAttachment, TransactionAttachmentDto>()
            .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => src.FilePath));

        //Budget
        CreateMap<UpsertBudgetRequest, Budget>();
        CreateMap<Budget, BudgetDto>()
            .ForMember(dest => dest.CategoryName,
                       opt => opt.MapFrom(src => src.Category!.Name));

        //Transfere 
        CreateMap<UpsertTransferRequest, Transfer>();
        CreateMap<Transfer, TransferDto>()
            .ForMember(dest => dest.FromAccountName,
                       opt => opt.MapFrom(src => src.FromAccount != null ? src.FromAccount.Name : string.Empty))
            .ForMember(dest => dest.ToAccountName,
                       opt => opt.MapFrom(src => src.ToAccount != null ? src.ToAccount.Name : string.Empty));

        // RecurringTransaction
        CreateMap<UpsertRecurringTransactionRequest, RecurringTransaction>();

        //Notification
        CreateMap<Notification, NotificationDto>();
    }
}
