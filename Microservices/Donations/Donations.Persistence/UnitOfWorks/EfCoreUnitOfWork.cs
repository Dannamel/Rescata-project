using Donations.Application.Contracts.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.UnitOfWorks
{
    public class EfCoreUnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;

        public EfCoreUnitOfWork(DataContext context)
        {
            _context = context;
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
