using System.Collections.Generic;

namespace SQLWerk.Data.Abstractions;

public interface IBatchDeleteService
{
    (int Deleted, int Related) DeleteExhibits(IEnumerable<long> ids);
    (int Deleted, int Related) DeleteVuzes(IEnumerable<long> ids);
    (int Deleted, int Related) DeleteGrnti(IEnumerable<long> ids);
}
