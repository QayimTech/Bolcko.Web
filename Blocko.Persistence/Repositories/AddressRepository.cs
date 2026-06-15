using Blocko.Persistence.Common;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;

namespace Blocko.Persistence.Repositories
{
    public class AddressRepository : GenericRepository<Address>, IAddressRepository
    {
        public AddressRepository(BlockoDbContext context) : base(context)
        {
        }
    }
}
