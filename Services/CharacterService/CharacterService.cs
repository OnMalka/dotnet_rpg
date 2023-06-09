using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using dotnet_rpg.Dtos.Fight;
using Microsoft.EntityFrameworkCore;

namespace dotnet_rpg.Services.CharacterService
{
    public class CharacterService : ICharacterService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CharacterService(IMapper mapper, DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _mapper = mapper;
        }

        private int GetUserId() => int.Parse(_httpContextAccessor.HttpContext!.User
            .FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        public async Task<ServiceResponse<List<GetCharacterDto>>> AddCharacter(AddCharacterDto newCharacter)
        {
            var serviceResponse  = new ServiceResponse<List<GetCharacterDto>>();
            var character = _mapper.Map<Character>(newCharacter);
            character.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == GetUserId());

            _context.Characters.Add(character);
            await _context.SaveChangesAsync();

            serviceResponse.Data = 
                await _context.Characters
                .Where(c => c.User != null && c.User.Id == GetUserId())
                .Select(c=>_mapper.Map<GetCharacterDto>(c))
                .ToListAsync();
            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> DeleteCharacter(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();

            try {
            var character = await _context.Characters
                .FirstOrDefaultAsync(c=>c.Id==id && c.User != null && c.User.Id == GetUserId());

            if(character is null)
                throw new Exception($"Character with the id '{id}' not found.");

            _context.Characters.Remove(character);   

            await _context.SaveChangesAsync();  

            serviceResponse.Data = await _context.Characters
                .Where(c => c.User != null && c.User.Id == GetUserId())
                .Select(c=>_mapper.Map<GetCharacterDto>(c)).ToListAsync();
            }
            catch(Exception ex){
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetCharacterDto>>> GetAllCharacters()
        {
            var serviceResponse  = new ServiceResponse<List<GetCharacterDto>>();
            //                                                (c => c.User!.Id == userId) I think mine is better.
            var dbCharacters = await _context.Characters
                .Include(c => c.Weapon)
                .Include(c => c.Skills)
                .Where(c => c.User != null && c.User.Id == GetUserId())
                .ToListAsync();
            serviceResponse.Data =dbCharacters.Select(c=>_mapper.Map<GetCharacterDto>(c)).ToList();         
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> GetCharacterById(int id)
        {
            var serviceResponse  = new ServiceResponse<GetCharacterDto>();
            var DBcharacter = await _context.Characters
                .Include(c => c.Weapon)
                .Include(c => c.Skills)
                .FirstOrDefaultAsync(c => c.Id == id && c.User != null && c.User.Id == GetUserId());
            serviceResponse.Data =  _mapper.Map<GetCharacterDto>(DBcharacter);
            return serviceResponse;  
        }

        public async Task<ServiceResponse<GetOpponentDto>> GetOpponentById(int id)
        {
            var serviceResponse  = new ServiceResponse<GetOpponentDto>();
            var DBcharacter = await _context.Characters
                .FirstOrDefaultAsync(c => c.Id == id);
            serviceResponse.Data =  _mapper.Map<GetOpponentDto>(DBcharacter);
            return serviceResponse;  
        }

        public async Task<ServiceResponse<GetCharacterDto>> UpdateCharacter(UpdateCharacterDto updatedCharacter)
        {
            var serviceResponse = new ServiceResponse<GetCharacterDto>();

            try {
            var character =
                await _context.Characters
                .Include(c => c.User)
                .FirstOrDefaultAsync(c=>c.Id==updatedCharacter.Id);

            if(
                character is null || 
                character.User is null || 
                (character.User is not null && character.User.Id != GetUserId())
            )
                throw new Exception($"Character with the id '{updatedCharacter.Id}' not found.");

            character.Name = updatedCharacter.Name;
            character.HitPoints = updatedCharacter.HitPoints;
            character.Strength = updatedCharacter.Strength;
            character.Defense = updatedCharacter.Defense;
            character.Intelligence = updatedCharacter.Intelligence;
            character.Class = updatedCharacter.Class;     

            await _context.SaveChangesAsync();
            serviceResponse.Data = _mapper.Map<GetCharacterDto>(character);
            }
            catch(Exception ex){
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetCharacterDto>> AddCharacterSkill(AddCharacterSkillDto newCharacterSkill)
        {
            var response = new ServiceResponse<GetCharacterDto>();
            try
            {
                var character = await _context.Characters
                    .Include(c => c.Weapon)
                    .Include(c => c.Skills) //.ThenInclude(s => s.Effects) if Skill would have a list of effects
                    .FirstOrDefaultAsync(
                        c => c.Id == newCharacterSkill.CharacterId &&
                        c.User != null &&
                        c.User.Id == GetUserId()
                    );
                if(character is null)
                {
                    response.Success = false;
                    response.Message = "Character not found.";
                    return response;
                }

                var skill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.Id == newCharacterSkill.SkillId);
                    if(skill is null)
                {
                    response.Success = false;
                    response.Message = "Skill not found.";
                    return response;
                }

                character.Skills!.Add(skill);
                await _context.SaveChangesAsync();
                response.Data = _mapper.Map<GetCharacterDto>(character);
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<GetOpponentDto>> DamageOpponent(DamageOpponentDto damageOpponent)
        {
            var response = new ServiceResponse<GetOpponentDto>();
            try
            {
            var opponent = await _context.Characters.FirstOrDefaultAsync(C => C.Id == damageOpponent.OpponentId);

            if(opponent is null)
                throw new Exception($"Character with id {damageOpponent.OpponentId} was not found.");

            int newHitPoints = opponent.HitPoints - damageOpponent.Damage;
            opponent.HitPoints = newHitPoints > 0 ? newHitPoints : 0;
            await _context.SaveChangesAsync();

            response.Data = _mapper.Map<GetOpponentDto>(opponent);
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