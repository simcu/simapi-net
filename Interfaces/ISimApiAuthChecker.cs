using SimApi.Communications;

namespace SimApi.Interfaces;

public interface ISimApiAuthChecker
{
    public void Run(SimApiLoginItem loginItem, string token);
}