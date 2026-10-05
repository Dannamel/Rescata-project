using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace Donations.Application.UseCases.Donations.Commands.CreateDonation;

public sealed class PickupAddressInputValidator : AbstractValidator<PickupAddressInput>
{
    public PickupAddressInputValidator()
    {
        RuleFor(address => address.RoadType)
            .IsInEnum().WithMessage("El tipo de vía no es válido.");

        RuleFor(address => address.RoadNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El número de la vía es obligatorio.")
            .MaximumLength(16).WithMessage("El número de la vía no puede superar 16 caracteres.");

        RuleFor(address => address.CrossRoadNumber)
            .NotEmpty().WithMessage("El número de la vía que cruza es obligatorio.");

        RuleFor(address => address.PlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.");

        RuleFor(address => address.RoadSuffix)
            .IsInEnum().WithMessage("El sufijo de la vía no es válido.");

        RuleFor(address => address.City)
            .NotEmpty().WithMessage("La ciudad es obligatoria.");
    }
}
