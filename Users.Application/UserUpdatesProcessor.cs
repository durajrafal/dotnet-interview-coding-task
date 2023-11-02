using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Users.Persistence;

namespace Users.Application;

public class UserUpdatesProcessor
{
    private readonly UserContext _context;

    public UserUpdatesProcessor(UserContext context)
    {
        _context = context;
    }

    public async Task Process(StreamReader stream)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var counter = 0;
            var partialSize = 1000;

            var jsonReader = new JsonTextReader(stream)
            {
                SupportMultipleContent = true
            };
            var jsonSerializer = new JsonSerializer();

            while (jsonReader.Read())
            {
                var user = jsonSerializer.Deserialize<UserProfile>(jsonReader);
                await ProcessUser(user);
                counter++;
                if (counter >= partialSize)
                {
                    await _context.SaveChangesAsync();
                    counter = 0;
                    _context.ChangeTracker.Clear();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            // TODO: add some more logic to handle exception
        }

        return;
    }

    private async Task ProcessUser(UserProfile user)
    {
        var userEntity = await _context.UserProfiles.FirstOrDefaultAsync(p => p.Id == user.Id);
        if (userEntity is not null)
        {
            userEntity.FirstName = user.FirstName;
            userEntity.LastName = user.LastName;
            userEntity.Email = user.Email;
            userEntity.PhoneNumber = user.PhoneNumber;
            userEntity.Address = user.Address;
        }
        else
        {
            _context.UserProfiles.Add(user);
        }
    }
}