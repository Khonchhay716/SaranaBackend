using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.API.Attributes;
using POS.API.Extensions;
using POS.Application.Common.Dto;
using POS.Application.Features.Coupon;
using POS.Application.Features.Person;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CouponController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [RequirePermission("coupon:read")]
        public async Task<ActionResult<PaginatedResult<CouponInfo>>> GetCoupons([FromQuery] CouponListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("coupon:create")]
        public async Task<IActionResult> CreateCoupon([FromBody] CouponCreateCommand command)
        {
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("coupon:read")]
        public async Task<IActionResult> GetCoupon(int id)
        {
            var query = new CouponQuery { Id = id };
            var result = await _mediator.Send(query);
            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("coupon:update")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] CouponUpdateCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("coupon:delete")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var command = new CouponDeleteCommand { Id = id };
            var result = await _mediator.Send(command);
            return this.ToActionResult(result);
        }
    }
}