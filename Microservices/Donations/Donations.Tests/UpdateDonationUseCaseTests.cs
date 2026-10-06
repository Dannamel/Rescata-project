using Donations.Application.Contracts.Persistence;
using Donations.Application.Contracts.Repositories;
using Donations.Application.Exceptions;
using Donations.Application.UseCases.Donations.Commands.UpdateDonation;
using Donations.Application.Utilities.Pagination;
using Donations.Domain.Common.ValueObjects;
using Donations.Domain.Entities.Donations;
using Donations.Domain.Entities.Donations.ValueObjects;

namespace Donations.Tests;

[TestClass]
public sealed class UpdateDonationUseCaseTests
{
    [TestMethod]
    public async Task Handle_DonacionExistente_ActualizaYPersiste()
    {
        Donation donation = CreateDonation();
        DonationsRepositoryFake repository = new(donation);
        UnitOfWorkFake unitOfWork = new();
        UpdateDonationUseCase useCase = new(repository, unitOfWork);
        DateTime newAvailableUntil = DateTime.UtcNow.AddHours(4);
        UpdateDonationCommand command = new()
        {
            Id = donation.Id,
            Title = "  Pan integral  ",
            Description = "Pan en buen estado.",
            QuantityAmount = 20m,
            QuantityUnit = QuantityUnit.Unidades,
            AvailableUntil = newAvailableUntil
        };

        await useCase.Handle(command);

        Assert.AreEqual("Pan integral", donation.Title);
        Assert.AreEqual("Pan en buen estado.", donation.Description);
        Assert.AreEqual(new Quantity(20m, QuantityUnit.Unidades), donation.Quantity);
        Assert.AreEqual(newAvailableUntil, donation.AvailableUntil);
        Assert.AreSame(donation, repository.UpdatedDonation);
        Assert.AreEqual(1, unitOfWork.CommitCount);
    }

    [TestMethod]
    public async Task Handle_DonacionInexistente_LanzaNotFoundException()
    {
        DonationsRepositoryFake repository = new(null);
        UnitOfWorkFake unitOfWork = new();
        UpdateDonationUseCase useCase = new(repository, unitOfWork);
        UpdateDonationCommand command = new() { Id = Guid.NewGuid() };

        NotFoundException exception = await Assert.ThrowsExactlyAsync<NotFoundException>(
            () => useCase.Handle(command));

        Assert.AreEqual("La donación no existe.", exception.Message);
        Assert.IsNull(repository.UpdatedDonation);
        Assert.AreEqual(0, unitOfWork.CommitCount);
    }

    private static Donation CreateDonation() =>
        new(Guid.NewGuid(),
            "Pan del día",
            "Pan fresco.",
            new Quantity(10m, QuantityUnit.Kilogramos),
            Guid.NewGuid(),
            new Address(RoadTypeEnum.Calle, "10", "43A", "25", RoadSuffixEnum.Sur, "Medellín"),
            DateTime.UtcNow.AddHours(2));

    private sealed class DonationsRepositoryFake(Donation? donation) : IDonationsRepository
    {
        public Donation? UpdatedDonation { get; private set; }

        public Task<Donation?> GetByIdAsync(Guid id) => Task.FromResult(donation);

        public Task UpdateAsync(Donation entity)
        {
            UpdatedDonation = entity;
            return Task.CompletedTask;
        }

        public Task<Donation> CreateAsync(Donation entity) => throw new NotSupportedException();

        public Task<PaginationResponse<Donation>> GetPagedListAsync(
            PaginationRequest pagination,
            DonationStatus? status,
            Guid? foodCategoryId) => throw new NotSupportedException();

        public Task<bool> FoodCategoryExistsAsync(Guid foodCategoryId) => throw new NotSupportedException();
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int CommitCount { get; private set; }

        public Task CommitAsync()
        {
            CommitCount++;
            return Task.CompletedTask;
        }
    }
}
