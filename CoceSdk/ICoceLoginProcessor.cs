using System.Collections.Generic;
using SimApi.Communications;

namespace SimApi.CoceSdk;

public interface ICoceLoginProcessor
{
    SimApiLoginItem Process(SimApiLoginItem loginItem, GroupInfo[] groups);
}