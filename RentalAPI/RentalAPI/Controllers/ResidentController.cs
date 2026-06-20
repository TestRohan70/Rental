using Microsoft.AspNetCore.Mvc;
using RentalAPI.DTO;
using RentalAPI.Models;

using RentalAPI.Repository;

namespace RentalAPI.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class ResidentController : ControllerBase
    {
        private readonly IResidentRepository _repository;

        public ResidentController(IResidentRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _repository.GetAll();

            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _repository.GetById(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateResidentDto dto)
        {
            try
            {
                var result = await _repository.Register(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Resident resident)
        {
            var result = await _repository.Update(id, resident);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            int id)
        {
            var result =
                await _repository.Delete(id);

            if (!result)
                return NotFound();

            return Ok("Deleted Successfully");
        }
    }
}