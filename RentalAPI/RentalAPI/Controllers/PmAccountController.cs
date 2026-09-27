using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentalAPI.Models;
using RentalAPI.Services;

namespace RentalAPI.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class PmAccountController : ControllerBase
    {
        private readonly IPmAccountService _service;

        public PmAccountController(IPmAccountService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] PmAccount account)
        {
            var result = await _service.CreateAsync(account);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] PmAccount account)
        {
            var result = await _service.UpdateAsync(id, account);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                message = "PmAccount deleted successfully"
            });
        }
    }
}
