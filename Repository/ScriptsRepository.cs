
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Data;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Dtos;

using ServerSide.Models;

namespace SearchTool_ServerSide.Repository
{
    public class ScriptsRepository 
    {
        private readonly SearchToolDBContext _context;
        
public ScriptsRepository(SearchToolDBContext context) 
        {
            _context = context;
        }



        public async Task<PagedResponse<SimpleScriptDto>> GetScriptsSimpleAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var baseQuery = _context.Scripts
                .AsNoTracking()
                .OrderByDescending(s => s.Date);

            var totalCount = await baseQuery.CountAsync();

            var data = await baseQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SimpleScriptDto
                {
                    Id = s.Id,
                    ScriptCode = s.ScriptCode,
                    Date = s.Date,

                    BranchId = s.BranchId,
                    BranchName = s.Branch.Name,

                    ItemsCount = s.ScriptItems.Count,

                    TotalNetProfit = s.ScriptItems.Sum(i =>
                        i.PatientPayment + i.InsurancePayment - i.AcquisitionCost
                    ),

                    Items = s.ScriptItems.Select(i => new SimpleScriptItemDto
                    {
                        Id = i.Id,
                        RxNumber = i.RxNumber,
                        PF = i.PF,
                        Quantity = i.Quantity,

                        AcquisitionCost = i.AcquisitionCost,
                        Discount = i.Discount,
                        InsurancePayment = i.InsurancePayment,
                        PatientPayment = i.PatientPayment,
                        NetProfit = i.PatientPayment + i.InsurancePayment - i.AcquisitionCost,

                        NDCCode = i.NDCCode,

                        DrugId = i.DrugId,
                        DrugName = i.Drug.Name,

                        InsuranceId = i.InsuranceId,
                      //  InsuranceName = i.Insurance.Name,
                       // InsuranceName = null,
InsuranceName =
    i.Insurance.InsurancePCN != null && i.Insurance.InsurancePCN.Insurance != null
        ? i.Insurance.InsurancePCN.Insurance.Name
        : null,



                        UserEmail = i.UserEmail,
                        PrescriberName = i.Prescriber != null ? i.Prescriber.Name : null
                    }).ToList()
                })
                .ToListAsync();

            return new PagedResponse<SimpleScriptDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }
    }
}
