using System;
using System.Collections.Generic;
using System.Text;
using Donations.Domain.Entities.Donations;
using FluentValidation;

namespace Donations.Application.UseCases.Donations.Commands.CreateDonation;

public sealed class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator()
    {
        RuleFor(command => command.BusinessId)
            .NotEmpty().WithMessage("El negocio que publica la donación es obligatorio.");

        RuleFor(command => command.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MinimumLength(Donation.TitleMinLength).WithMessage("El título debe tener entre 3 y 100 caracteres.")
            .MaximumLength(Donation.TitleMaxLength).WithMessage("El título debe tener entre 3 y 100 caracteres.");

        RuleFor(command => command.Description)
            .MaximumLength(Donation.DescriptionMaxLength).WithMessage("La descripción no puede superar 500 caracteres.");

        RuleFor(command => command.QuantityAmount)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");

        RuleFor(command => command.QuantityUnit)
            .IsInEnum().WithMessage("La unidad de medida no es válida. Use Kilogramos, Unidades o Litros.");

        RuleFor(command => command.FoodCategoryId)
            .NotEmpty().WithMessage("La categoría del alimento es obligatoria.");

        RuleFor(command => command.PickupAddress)
            .NotNull().WithMessage("La dirección de recogida es obligatoria.")
            .SetValidator(new PickupAddressInputValidator());

        RuleFor(command => command.AvailableUntil)
            .Must(BeWithinAvailabilityWindow)
            .WithMessage("La hora límite debe estar entre 1 hora y 7 días a partir de ahora.");
    }

    // Las fechas sin zona (Unspecified) se interpretan como UTC, igual que en el dominio.
    private static bool BeWithinAvailabilityWindow(DateTime availableUntil)
    {
        DateTime availableUntilUtc = availableUntil.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(availableUntil, DateTimeKind.Utc)
            : availableUntil.ToUniversalTime();

        DateTime now = DateTime.UtcNow;

        return availableUntilUtc >= now.Add(Donation.MinAvailability) &&
               availableUntilUtc <= now.Add(Donation.MaxAvailability);
    }
}
