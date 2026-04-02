using Microsoft.AspNetCore.Authorization;

namespace SearchTool_ServerSide.Authorization
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permissionName)
        {
            Policy = $"Permission:{permissionName}";// policy = permission:feedback.view
        }
    }
}