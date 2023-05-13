using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_rpg.Dtos.Fight;

namespace dotnet_rpg.Services.FightService
{
    public class FightService : IFightService
    {
        private readonly DataContext _context;
        private readonly ICharacterService _characterService;
        public FightService(DataContext context, ICharacterService characterService)
        {
            _context = context;
            _characterService = characterService;
        }

        public async Task<ServiceResponse<AttackResultDto>> SkillAttack(SkillAttackDto request)
        {
            var response = new ServiceResponse<AttackResultDto>();
            try
            {
                var attacker = (await _characterService.GetCharacterById(request.AttackerId)).Data;
                var opponent = (await _characterService.GetOpponentById(request.OpponentId)).Data;

                if(attacker is null || opponent is null || attacker.Skills is null)
                    throw new Exception("Something fishy is going on here");

                var skill = attacker.Skills.FirstOrDefault(s => s.Id == request.SkillId);
                if(skill is null)
                {
                    response.Success = false;
                    response.Message = $"{attacker.Name} dosen't know that skill!";
                    return response;
                }

                int damage = skill.Damage + (new Random().Next(attacker.Intelligence));
                damage -= new Random().Next(opponent.Defense);

                if(damage > 0)
                    opponent.HitPoints -= damage;

                if(opponent.HitPoints <= 0)
                    response.Message = $"{opponent.Name} has been defeated!";

                await _characterService.DamageOpponent(new DamageOpponentDto 
                {
                    Damage = damage,
                    OpponentId = opponent.Id
                });

                response.Data = new AttackResultDto() 
                {
                    Attacker = attacker.Name,
                    Opponent = opponent.Name,
                    AttackerHp = attacker.HitPoints,
                    OpponentHp = opponent.HitPoints,
                    Damage = damage
                };
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<AttackResultDto>> WeaponAttack(WeaponAttackDto request)
        {
            var response = new ServiceResponse<AttackResultDto>();
            try
            {
                var attacker = (await _characterService.GetCharacterById(request.AttackerId)).Data;
                var opponent = (await _characterService.GetOpponentById(request.OpponentId)).Data;

                if(attacker is null || opponent is null || attacker.Weapon is null)
                    throw new Exception("Something fishy is going on here");

                int damage = attacker.Weapon.Damage + (new Random().Next(attacker.Strength));
                damage -= new Random().Next(opponent.Defeats);

                if(damage > 0)
                    opponent.HitPoints -= damage;

                if(opponent.HitPoints <= 0)
                    response.Message = $"{opponent.Name} has been defeated!";

                await _characterService.DamageOpponent(new DamageOpponentDto 
                {
                    Damage = damage,
                    OpponentId = opponent.Id
                });

                response.Data = new AttackResultDto() 
                {
                    Attacker = attacker.Name,
                    Opponent = opponent.Name,
                    AttackerHp = attacker.HitPoints,
                    OpponentHp = opponent.HitPoints,
                    Damage = damage
                };
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
    }
}