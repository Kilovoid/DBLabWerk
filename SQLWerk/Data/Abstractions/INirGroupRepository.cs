using SQLWerk.Models;
using System.Collections.Generic;

namespace SQLWerk.Data.Abstractions;

public interface INirGroupRepository
{
    void EnsureCreated();
    List<NirGroup> GetGroups();
    void CreateGroup(string code);
    bool DeleteGroup(string groupCode);
    List<long> GetExhibitIds(string groupCode);
    int AddMembers(string groupCode, IEnumerable<long> exhibitIds);
    int AddMembers(IEnumerable<string> groupCodes, IEnumerable<long> exhibitIds);
    int RemoveMembers(string groupCode, IEnumerable<long> exhibitIds);
}
