using System.Collections.Generic;
using System.Threading.Tasks;
using DataQI.EntityFrameworkCore.Repository;

namespace CompletionSample
{
    public interface IProductRepository : IEntityRepository<Product, int>
    {
        // type here:
    }

    public interface INotARepository
    {
        // type here:
    }
}
