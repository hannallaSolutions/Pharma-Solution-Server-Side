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
        INSERT INTO ""MainCompanies"" (""Name"", ""SpecialtyId"", ""ClassTypeId"")
        VALUES (@name, @specialtyId, @classTypeId)
        RETURNING ""Id"", ""Name"", ""SpecialtyId"", ""ClassTypeId"";
    ";

            cmd.Parameters.Add(new NpgsqlParameter("@name", mainCompany.Name));
            cmd.Parameters.Add(new NpgsqlParameter("@specialtyId", mainCompany.SpecialtyId));
            cmd.Parameters.Add(new NpgsqlParameter("@classTypeId", mainCompany.ClassTypeId ?? 2));

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new MainCompany
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SpecialtyId = reader.GetInt32(2),
                    ClassTypeId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
                };
            }

            return null;
        }

internal async Task<MainCompany> CreateAsync(MainCompany mainCompany)
{
    await _context.MainCompanies.AddAsync(mainCompany);
    await _context.SaveChangesAsync();
    return mainCompany;
}

      
        internal async Task<IEnumerable<object>> GetAllMainCompaniesAsync()
{
    return await _context.MainCompanies
        .Select(x => new
        {
            id = x.Id,
            name = x.Name,
            specialtyId = x.SpecialtyId,
            specialtyName = x.Specialty != null ? x.Specialty.Name : null,
            classTypeId = x.ClassTypeId,
            classTypeName = x.ClassType != null ? x.ClassType.Name : null
        })
        .ToListAsync();
}

        internal async Task<MainCompany?> GetMainCompanyByIdAsync(int id)
        {
            // ✅ Since Name is TEXT in DB, no decrypt needed
            return await _context.MainCompanies
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        //edit each row
      internal async Task<bool> EditMainCompanyAsync(int id, MainCompany updatedCompany)
{
    var existingCompany = await _context.MainCompanies.FindAsync(id);

    if (existingCompany == null)
    {
        return false;
    }

    existingCompany.Name = updatedCompany.Name;
    existingCompany.SpecialtyId = updatedCompany.SpecialtyId;
    existingCompany.ClassTypeId = updatedCompany.ClassTypeId;

    await _context.SaveChangesAsync();
    return true;
}
        

        internal async Task<bool> DeleteMainCompanyAsync(int id)
{
    var existingCompany = await _context.MainCompanies.FindAsync(id);

    if (existingCompany == null)
    {
        return false;
    }

    _context.MainCompanies.Remove(existingCompany);
    await _context.SaveChangesAsync();

    return true;
}

    }
}
