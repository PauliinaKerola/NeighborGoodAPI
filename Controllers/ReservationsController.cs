using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeighborGoodAPI.Models;
using System.Text.Json;

namespace NeighborGoodAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly NGDbContext _context;

        public ReservationsController(NGDbContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations()
        {
            return await _context.Reservations.ToListAsync();
        }

        // GET: api/Reservations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Reservation>> GetReservation(int id)
        {
            return await _context.Reservations.Include(r => r.Reserver).Include(r => r.Item).SingleOrDefaultAsync(r => r.Id == id);
        }

        // GET: api/Reservations/ItemsReservedByUser/{userId}
        [HttpGet("ItemsReservedByUser/{userId}")]
        public async Task<ActionResult<List<Reservation>?>> ItemsReservedByUser(string userId)
        {
            System.Diagnostics.Debug.WriteLine(DateTime.Now);
            return await _context.Reservations.Where(r => r.Reserver.Auth0Id == userId && r.ReservationDate >= DateTime.Now)
                                                .Include(r => r.Item)
                                                .ThenInclude(i => i.Owner)
                                                .ThenInclude(p => p.Address)
                                                .OrderBy(r => r.Item.Id)
                                                .OrderBy(r => r.ReservationDate)
                                                .ToListAsync()
                                                ;
        }

        // GET: api/ItemsReservedFromUser/{userId}
        [HttpGet("ItemsReservedFromUser/{userId}")]
        public async Task<ActionResult<List<Reservation>?>> ItemsReservedFromUser(string userId)
        {
            return await _context.Reservations.Where(r => r.Item.Owner.Auth0Id == userId)
                                                .Include(r => r.Reserver)
                                                .Include(r => r.Item)
                                                .ThenInclude(i => i.Owner)
                                                .ThenInclude(p => p.Address)
                                                .OrderBy(r => r.Item.Id)
                                                .OrderBy(r => r.ReservationDate)
                                                .ToListAsync();

        }

        // GET: api/Reservations/5
        [HttpGet("ReservationByItemId/{itemId}")]
        public async Task<ActionResult<List<Reservation>>?> GetReservationByItemid(int itemId)
        {
            return await _context.Reservations.Include(r => r.Reserver).Include(r => r.Item).Where(r => r.Item.Id == itemId).ToListAsync();
        }

        // PUT: api/Reservations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation(int id, Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return BadRequest();
            }

            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Reservations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Reservation>> PostReservation(Reservation reservation)
        {
            var item = await _context.Items.FindAsync(reservation.Item.Id);
            if(item == null)
            {
                return NotFound("Tuotetta ei löytynyt, varauksen teko epäonnistui");
            }
            if(_context.Reservations.Any(r => r.Item.Id == reservation.Item.Id && r.ReservationDate == reservation.ReservationDate))
            {
                return BadRequest("Päivälle on jo varaus");
            }

            var reserver = await _context.Profiles.FindAsync(reservation.Reserver.Id);
            if(reserver == null) {
                return NotFound("Käyttäjää ei löytynyt, varauksen teko epäonnistui");
            }

            Reservation newRes = new()
            {
                Item = item,
                ReservationDate = reservation.ReservationDate,
                Reserver = reserver
            };

            _context.Reservations.Add(newRes);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReservation", new { id = newRes.Id }, newRes);
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
}
