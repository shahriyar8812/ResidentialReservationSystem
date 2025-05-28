using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class CheckInRuleRepository : ICheckInRuleRepository
    {
        private readonly ApplicationDbContext _context;

        public CheckInRuleRepository(ApplicationDbContext context)
        {
            _context = context;
            System.Console.WriteLine("CheckInRuleRepository: Repository initialized with ApplicationDbContext.");
        }

        public async Task<CheckInRule> GetByUnitIdAsync(int unitId)
        {
            System.Console.WriteLine($"CheckInRuleRepository: Fetching rule for UnitId: {unitId}");
            var rule = await _context.CheckInRules
                .Where(r => r.UnitId == unitId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (rule == null)
            {
                System.Console.WriteLine($"CheckInRuleRepository: No rule found for UnitId: {unitId}");
            }
            else
            {
                System.Console.WriteLine($"CheckInRuleRepository: Found rule - Id: {rule.Id}, UnitId: {rule.UnitId}, CheckIn: {rule.AllowedCheckInDay}, CheckOut: {rule.AllowedCheckOutDay}");
            }
            return rule;
        }

        public async Task AddAsync(CheckInRule rule)
        {
            await _context.CheckInRules.AddAsync(rule);
            await _context.SaveChangesAsync();
            System.Console.WriteLine($"CheckInRuleRepository: Added new rule - Id: {rule.Id}, UnitId: {rule.UnitId}, CheckIn: {rule.AllowedCheckInDay}, CheckOut: {rule.AllowedCheckOutDay}");
        }

        public async Task UpdateAsync(CheckInRule rule)
        {
            var existingRule = await _context.CheckInRules.FindAsync(rule.Id);
            if (existingRule != null)
            {
                System.Console.WriteLine($"CheckInRuleRepository: Updating rule - Id: {rule.Id}, UnitId: {rule.UnitId}, Old CheckIn: {existingRule.AllowedCheckInDay}, Old CheckOut: {existingRule.AllowedCheckOutDay}, New CheckIn: {rule.AllowedCheckInDay}, New CheckOut: {rule.AllowedCheckOutDay}");
                existingRule.UnitId = rule.UnitId;
                existingRule.AllowedCheckInDay = rule.AllowedCheckInDay;
                existingRule.AllowedCheckOutDay = rule.AllowedCheckOutDay;
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"CheckInRuleRepository: Updated rule - Id: {existingRule.Id}, UnitId: {existingRule.UnitId}, CheckIn: {existingRule.AllowedCheckInDay}, CheckOut: {existingRule.AllowedCheckOutDay}");
            }
            else
            {
                System.Console.WriteLine($"CheckInRuleRepository: Rule with Id: {rule.Id} not found for update.");
            }
        }

        public async Task DeleteAsync(int id)
        {
            var rule = await _context.CheckInRules.FindAsync(id);
            if (rule != null)
            {
                System.Console.WriteLine($"CheckInRuleRepository: Deleting rule - Id: {rule.Id}, UnitId: {rule.UnitId}, CheckIn: {rule.AllowedCheckInDay}, CheckOut: {rule.AllowedCheckOutDay}");
                _context.CheckInRules.Remove(rule);
                await _context.SaveChangesAsync();
            }
            else
            {
                System.Console.WriteLine($"CheckInRuleRepository: Rule with Id: {id} not found for deletion.");
            }
        }
    }
}