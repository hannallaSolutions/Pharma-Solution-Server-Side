
using SearchTool_ServerSide.Dtos;
using SearchTool_ServerSide.Repository;

namespace SearchTool_ServerSide.Services
{
    public class ScriptsService
    {
        private readonly ScriptsRepository _repo;

        public ScriptsService(ScriptsRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResponse<SimpleScriptDto>> GetScriptsSimpleAsync(int pageNumber, int pageSize,int branchId)
            => _repo.GetScriptsSimpleAsync(pageNumber, pageSize,branchId);
    }
}
