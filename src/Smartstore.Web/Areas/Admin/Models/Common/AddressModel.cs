using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Admin.Models.Common
{
    //[LocalizedDisplay("Admin.Address.Fields.")]
    //public partial class AddressModel : EntityModelBase
    //{
    //    [LocalizedDisplay("Address.Fields.Title")]
    //    public string Title { get; set; }

    //    [LocalizedDisplay("*FirstName")]
    //    public string FirstName { get; set; }

    //    [LocalizedDisplay("*LastName")]
    //    public string LastName { get; set; }

    //    [LocalizedDisplay("*Email")]
    //    public string Email { get; set; }

    //    [LocalizedDisplay("*EmailMatch")]
    //    public string EmailMatch { get; set; }

    //    [LocalizedDisplay("*Company")]
    //    public string Company { get; set; }

    //    [LocalizedDisplay("*Country")]
    //    public int? CountryId { get; set; }

    //    [LocalizedDisplay("*Country")]
    //    public string CountryName { get; set; }

    //    [LocalizedDisplay("*StateProvince")]
    //    public int? StateProvinceId { get; set; }

    //    [LocalizedDisplay("*StateProvince")]
    //    public string StateProvinceName { get; set; }

    //    [LocalizedDisplay("*City")]
    //    public string City { get; set; }

    //    [LocalizedDisplay("*Address1")]
    //    public string Address1 { get; set; }

    //    [LocalizedDisplay("*Address2")]
    //    public string Address2 { get; set; }

    //    [LocalizedDisplay("*ZipPostalCode")]
    //    public string ZipPostalCode { get; set; }

    //    [LocalizedDisplay("*PhoneNumber")]
    //    public string PhoneNumber { get; set; }

    //    [LocalizedDisplay("*FaxNumber")]
    //    public string FaxNumber { get; set; }

    //    public List<SelectListItem> AvailableCountries { get; set; } = new();
    //    public List<SelectListItem> AvailableStates { get; set; } = new();

    //    public string FormattedAddress { get; set; }

    //    public bool TitleEnabled { get; set; }
    //    public bool FirstNameEnabled { get; set; }
    //    public bool FirstNameRequired { get; set; }
    //    public bool LastNameEnabled { get; set; }
    //    public bool LastNameRequired { get; set; }
    //    public bool EmailEnabled { get; set; }
    //    public bool EmailRequired { get; set; }
    //    public bool ValidateEmailAddress { get; set; }
    //    public bool CompanyEnabled { get; set; }
    //    public bool CompanyRequired { get; set; }
    //    public bool CountryEnabled { get; set; }
    //    public bool StateProvinceEnabled { get; set; }
    //    public bool CityEnabled { get; set; }
    //    public bool CityRequired { get; set; }
    //    public bool StreetAddressEnabled { get; set; }
    //    public bool StreetAddressRequired { get; set; }
    //    public bool StreetAddress2Enabled { get; set; }
    //    public bool StreetAddress2Required { get; set; }
    //    public bool ZipPostalCodeEnabled { get; set; }
    //    public bool ZipPostalCodeRequired { get; set; }
    //    public bool PhoneEnabled { get; set; }
    //    public bool PhoneRequired { get; set; }
    //    public bool FaxEnabled { get; set; }
    //    public bool FaxRequired { get; set; }
    //}

    //public partial class AddressValidator : AbstractValidator<AddressModel>
    //{
    //    public AddressValidator(Localizer T)
    //    {
    //        RuleFor(x => x.FirstName)
    //            .NotEmpty()
    //            .When(x => x.FirstNameEnabled && x.FirstNameRequired);

    //        RuleFor(x => x.LastName)
    //            .NotEmpty()
    //            .When(x => x.LastNameEnabled && x.LastNameRequired);

    //        RuleFor(x => x.Email)
    //            .NotEmpty()
    //            .EmailAddress()
    //            .When(x => x.EmailEnabled && x.EmailRequired);

    //        RuleFor(x => x.Company)
    //            .NotEmpty()
    //            .When(x => x.CompanyEnabled && x.CompanyRequired);

    //        RuleFor(x => x.CountryId)
    //            .NotEmpty()
    //            .When(x => x.CountryEnabled);

    //        RuleFor(x => x.CountryId)
    //            .NotEqual(0)
    //            .WithMessage(T("*Country.Required"))
    //            .When(x => x.CountryEnabled);

    //        RuleFor(x => x.City)
    //            .NotEmpty()
    //            .When(x => x.CityEnabled && x.CityRequired);

    //        RuleFor(x => x.Address1)
    //            .NotEmpty()
    //            .When(x => x.StreetAddressEnabled && x.StreetAddressRequired);

    //        RuleFor(x => x.Address2)
    //            .NotEmpty()
    //            .When(x => x.StreetAddress2Enabled && x.StreetAddress2Required);

    //        RuleFor(x => x.ZipPostalCode)
    //            .NotEmpty()
    //            .When(x => x.ZipPostalCodeEnabled && x.ZipPostalCodeRequired);

    //        RuleFor(x => x.PhoneNumber)
    //            .NotEmpty()
    //            .When(x => x.PhoneEnabled && x.PhoneRequired);

    //        RuleFor(x => x.FaxNumber)
    //            .NotEmpty()
    //            .When(x => x.FaxEnabled && x.FaxRequired);

    //        RuleFor(x => x.EmailMatch)
    //            .NotEmpty()
    //            .Equal(x => x.Email)
    //            .WithMessage(T("*EmailMatch.MustMatchEmail"))
    //            .When(x => x.ValidateEmailAddress);
    //    }
    //}

    //public static partial class AddressMappingExtensions
    //{
    //    public static async Task<AddressModel> MapAsync(this Address entity, dynamic parameters = null)
    //    {
    //        var to = new AddressModel();
    //        await MapAsync(entity, to, parameters);

    //        return to;
    //    }

    //    public static async Task MapAsync(this Address entity, AddressModel to, dynamic parameters = null)
    //    {
    //        await MapperFactory.MapAsync(entity, to, parameters);
    //    }
    //}

    //public class AddressMapper : Mapper<Address, AddressModel>
    //{
    //    private readonly IAddressService _addressService;

    //    public AddressMapper(IAddressService addressService)
    //    {
    //        _addressService = addressService;
    //    }

    //    protected override void Map(Address from, AddressModel to, dynamic parameters = null)
    //        => throw new NotImplementedException();

    //    public override async Task MapAsync(Address from, AddressModel to, dynamic parameters = null)
    //    {
    //        Guard.NotNull(from, nameof(from));
    //        Guard.NotNull(to, nameof(to));

    //        MiniMapper.Map(from, to);

    //        to.CountryName = from.Country?.Name;
    //        to.StateProvinceName = from.StateProvince?.Name;
    //        to.EmailMatch = from.Email;
    //        to.FormattedAddress = await _addressService.FormatAddressAsync(from, true);
    //    }
    //}
}
