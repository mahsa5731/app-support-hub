using System.Reflection;

namespace AppSupportHub.Web;

public static class AssemblyReference
{
    public static Assembly Assembly => typeof(AssemblyReference).Assembly;
}
