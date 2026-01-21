using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using Npgsql;

namespace SearchTool_ServerSide.Repository
{
    public class MainCompanyRepository : GenericRepository<MainCompany>
    {
        private readonly SearchToolDBContext _context;
        private readonly IMapper _mapper;

        // ⚠️ Not used now because Name column is TEXT (not encrypted)
        private readonly string encryptionKey = "test_key";

        public MainCompanyRepository(SearchToolDBContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        internal async Task<MainCompany?> AddMainCompanyAsync(MainCompany mainCompany)
        {
            await using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ""MainCompanies"" (""Name"", ""SpecialtyId"")
                VALUES (@name, @specialtyId)
                RETURNING ""Id"", ""Name"", ""SpecialtyId"";
            ";

            cmd.Parameters.Add(new NpgsqlParameter("@name", mainCompany.Name));
            cmd.Parameters.Add(new NpgsqlParameter("@specialtyId", mainCompany.SpecialtyId));

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new MainCompany
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SpecialtyId = reader.GetInt32(2)
                };
            }

            return null;
        }

        internal async Task<IEnumerable<MainCompany>> GetAllMainCompaniesAsync()
        {
            return await _context.MainCompanies.ToListAsync();
        }

        internal async Task<MainCompany?> GetMainCompanyByIdAsync(int id)
        {
            // ✅ Since Name is TEXT in DB, no decrypt needed
            return await _context.MainCompanies
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
