using Donations.Application.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Donations.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DataContext _context;

        public Repository(DataContext context)
        {
            _context = context;
        }

        public Task<T> CreateAsync(T entity)
        {
            _context.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<T?> GetByIdAsync(Guid id)
        {
            T? entity = _context.Find<T>(id);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(T entity)
        {
            _context.Update(entity);
            return Task.CompletedTask;
        }
    }
}
