namespace Maynard.Web;

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

public static class RouteHelper
{
    public static string GetRoutePath<TController>(string actionName) where TController : ControllerBase
    {
        Type controllerType = typeof(TController);
        MethodInfo method = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);

        return method != null
            ? GetRoutePath(controllerType, method)
            : throw new ArgumentException($"Action '{actionName}' not found on controller '{controllerType.Name}'");
    }

    public static string GetRoutePath(Type controllerType, MethodInfo method)
    {
        RouteAttribute controllerRoute = controllerType.GetCustomAttribute<RouteAttribute>();
        string controllerPath = controllerRoute?.Template ?? string.Empty;
    
        string controllerName = controllerType.Name.Replace("Controller", "");
        controllerPath = controllerPath.Replace("[controller]", controllerName.ToLower());

        RouteAttribute actionRoute = method.GetCustomAttribute<RouteAttribute>();
        if (actionRoute == null)
            return controllerPath;
        return actionRoute.Template.StartsWith(controllerPath) 
            ? actionRoute.Template 
            : $"{controllerPath}/{actionRoute.Template}".Replace("//", "/");
    }
}