using Microsoft.AspNetCore.Mvc;
using POS.Application.Common.Dto;

namespace POS.API.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult ToActionResult<T>(this ControllerBase controller, ApiResponse<T> response)
        {
            if (response == null)
                return controller.NotFound();

            if (!response.Success)
            {
                return response.Message.ToLower().Contains("not found") 
                    ? controller.NotFound(response) 
                    : controller.BadRequest(response);
            }

            return controller.Ok(response);
        }

        public static IActionResult ToActionResult(this ControllerBase controller, ApiResponse response)
        {
            if (response == null)
                return controller.NotFound();

            if (!response.Success)
            {
                return response.Message.ToLower().Contains("not found") 
                    ? controller.NotFound(response) 
                    : controller.BadRequest(response);
            }

            return controller.Ok(response);
        }
    }
}