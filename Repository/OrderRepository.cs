using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class OrderRepository : GenericRepository<Order>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;
        public OrderRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        internal async Task<IEnumerable<Order>> GetAllOrdersByUserId(string UserEmail)
        {
            var user = await _context.Users
                .Include(x => x.Branch)
                .ThenInclude(b => b.MainCompany)
                .FirstOrDefaultAsync(u => u.Email == UserEmail);

            if (user == null)
            {
                return new List<Order>();
            }

            var mainCompanyName = user.Branch.MainCompany.Name;

            // Get all user emails in the same main company
            var companyUserEmails = await _context.Users
                .Include(u => u.Branch)
                .ThenInclude(b => b.MainCompany)
                .Where(u => u.Branch.MainCompany.Name == mainCompanyName)
                .Select(u => u.Email)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => companyUserEmails.Contains(o.UserEmail))
                .ToListAsync();

            return orders;
        }
    }
}