using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class AddressService : IAddressService
    {
        private readonly ApplicationDbContext _context;

        public AddressService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AddressVM>> GetUserAddressesAsync(string userId)
        {
            return await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressVM
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();
        }

        public async Task<AddressVM?> GetAddressByIdAsync(int id, string userId)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null) return null;

            return new AddressVM
            {
                Id = address.Id,
                FullName = address.FullName,
                PhoneNumber = address.PhoneNumber,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            };
        }

        public async Task<int> SaveAddressAsync(AddressVM model, string userId)
        {
            if (model.Id > 0)
            {
                var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);
                if (address != null)
                {
                    address.FullName = model.FullName;
                    address.PhoneNumber = model.PhoneNumber;
                    address.AddressLine1 = model.AddressLine1;
                    address.AddressLine2 = model.AddressLine2;
                    address.City = model.City;
                    address.State = model.State;
                    address.PostalCode = model.PostalCode;
                    address.Country = model.Country;
                    address.UpdatedAt = DateTime.UtcNow;

                    if (model.IsDefault && !address.IsDefault)
                    {
                        var currentDefaults = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                        foreach (var d in currentDefaults) d.IsDefault = false;
                        address.IsDefault = true;
                    }

                    await _context.SaveChangesAsync();
                    return address.Id;
                }
            }

            // Create new
            bool isFirst = !await _context.Addresses.AnyAsync(a => a.UserId == userId);
            var newAddress = new Address
            {
                UserId = userId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                PostalCode = model.PostalCode,
                Country = model.Country,
                IsDefault = isFirst || model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            if (newAddress.IsDefault && !isFirst)
            {
                var currentDefaults = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var d in currentDefaults) d.IsDefault = false;
            }

            await _context.Addresses.AddAsync(newAddress);
            await _context.SaveChangesAsync();
            return newAddress.Id;
        }

        public async Task SetDefaultAddressAsync(int id, string userId)
        {
            var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            foreach (var addr in addresses)
            {
                addr.IsDefault = (addr.Id == id);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAddressAsync(int id, string userId)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address != null)
            {
                address.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
