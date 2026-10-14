using System.Collections.Generic;
using System.Threading.Tasks;
using DataQI.Commons.Repository;

namespace CompletionSample
{
    public interface IProductRepository : ICrudRepository<Product, int>
    {
        // type here:
    }

    public interface INotARepository
    {
        // type here:
    }
}
