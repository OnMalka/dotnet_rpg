using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_rpg.Dtos.Fight;
using dotnet_rpg.Dtos.Skill;

namespace dotnet_rpg.Services.FightService
{
    public class FightService : IFightService
    {
        private readonly DataContext _context;
        private readonly ICharacterService _characterService;
        private readonly IMapper _mapper;
        public FightService(IMapper mapper, DataContext context, ICharacterService characterService)
        {
            _mapper = mapper;
            _context = context;
            _characterService = characterService;
        }

        public async Task<ServiceResponse<FightResultDto>> Fight(FightRequestDto request)
        {
            var response = new ServiceResponse<FightResultDto>
                {
                    Data = new FightResultDto()
                };

            try
            {
                var characters = await _context.Characters
                    .Include(c => c.Weapon)
                    .Include(c => c.Skills)
                    .Where(c => request.CharacterIds.Contains(c.Id))
                    .ToListAsync();

                bool defeated = false;
                while(!defeated)
                {
                    foreach(var attacker in characters)
                    {
                        var opponents = characters.Where(c => c.Id != attacker.Id).ToList();
                        var opponent = opponents[new Random().Next(opponents.Count)];

                        var damage = 0;
                        string attackUsed = string.Empty;

                        bool useWeapon = new Random().Next(2) == 0;
                        if(useWeapon && attacker.Weapon is not null)
                        {
                            attackUsed = attacker.Weapon.Name;
                            damage = await DoWeaponAttack(
                                _mapper.Map<GetCharacterDto>(attacker), 
                                _mapper.Map<GetOpponentDto>(opponent)
                            );
                        }
                        else if(!useWeapon && attacker.Skills is not null)
                        {
                            var skill = attacker.Skills[new Random().Next(attacker.Skills.Count)];
                            attackUsed = skill.Name;
                            damage = await DoSkillAttack(
                                _mapper.Map<GetCharacterDto>(attacker), 
                                _mapper.Map<GetOpponentDto>(opponent),
                                _mapper.Map<GetSkillDto>(skill)
                            );
                        } 
                        else
                        {
                            response.Data.Log
                                .Add($"{attacker.Name} wasn't able to attack!");
                            continue;
                        }

                        response.Data.Log
                            .Add($"{attacker.Name} attacks {opponent.Name} using {attackUsed} with {(damage >= 0 ? damage : 0)} damage");

                        if(opponent.HitPoints <= 0)
                        {
                            defeated = true;
                            attacker.Victories++;
                            opponent.Defeats++;
                            response.Data.Log
                                .Add($"{opponent.Name} has been defeated!");
                            response.Data.Log
                                .Add($"{attacker.Name} wins with {attacker.HitPoints} HP left!");
                            break;
                        }
                    }
                }

                characters.ForEach(c => {
                    c.Fights++;
                    c.HitPoints = 100;
                });

                await _context.SaveChangesAsync(); 
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<AttackResultDto>> SkillAttack(SkillAttackDto request)
        {
            var response = new ServiceResponse<AttackResultDto>();
            try
            {
                var attacker = (await _characterService.GetCharacterById(request.AttackerId)).Data;
                var opponent = (await _characterService.GetOpponentById(request.OpponentId)).Data;

                if (attacker is null || opponent is null || attacker.Skills is null)
                    throw new Exception("Something fishy is going on here");

                var skill = attacker.Skills.FirstOrDefault(s => s.Id == request.SkillId);
                if (skill is null)
                {
                    response.Success = false;
                    response.Message = $"{attacker.Name} dosen't know that skill!";
                    return response;
                }

                int damage = await DoSkillAttack(attacker, opponent, skill);

                if (opponent.HitPoints <= 0)
                    response.Message = $"{opponent.Name} has been defeated!";


                response.Data = new AttackResultDto()
                {
                    Attacker = attacker.Name,
                    Opponent = opponent.Name,
                    AttackerHp = attacker.HitPoints,
                    OpponentHp = opponent.HitPoints,
                    Damage = damage
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        private async Task<int> DoSkillAttack(GetCharacterDto attacker, GetOpponentDto opponent, GetSkillDto skill)
        {
            int damage = skill.Damage + (new Random().Next(attacker.Intelligence));
            damage -= new Random().Next(opponent.Defense);

            if (damage > 0)
                await _characterService.DamageOpponent(new DamageOpponentDto
                {
                    Damage = damage,
                    OpponentId = opponent.Id
                });
            return damage;
        }

        public async Task<ServiceResponse<AttackResultDto>> WeaponAttack(WeaponAttackDto request)
        {
            var response = new ServiceResponse<AttackResultDto>();
            try
            {
                var attacker = (await _characterService.GetCharacterById(request.AttackerId)).Data;
                var opponent = (await _characterService.GetOpponentById(request.OpponentId)).Data;

                if (attacker is null || opponent is null || attacker.Weapon is null)
                    throw new Exception("Something fishy is going on here");
                int damage = await DoWeaponAttack(attacker, opponent);

                if (opponent.HitPoints <= 0)
                    response.Message = $"{opponent.Name} has been defeated!";

                response.Data = new AttackResultDto()
                {
                    Attacker = attacker.Name,
                    Opponent = opponent.Name,
                    AttackerHp = attacker.HitPoints,
                    OpponentHp = opponent.HitPoints,
                    Damage = damage
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        private async Task<int> DoWeaponAttack(GetCharacterDto attacker, GetOpponentDto opponent)
        {
            if(attacker.Weapon is null)
                throw new Exception("Attacker has no weapon!");
                
            int damage = attacker.Weapon.Damage + (new Random().Next(attacker.Strength));
            damage -= new Random().Next(opponent.Defeats);

            if (damage > 0)
                await _characterService.DamageOpponent(new DamageOpponentDto
                {
                    Damage = damage,
                    OpponentId = opponent.Id
                });
            return damage;
        }
    }
}