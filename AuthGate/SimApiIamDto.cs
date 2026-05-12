namespace SimApi.AuthGate;

public class SimApiIamDto
{
    public class PermissionItem
    {
        public required string Identifier { get; init; }
        public required string Name { get; init; }
        public required string Group { get; init; }
        public required string Description { get; init; }
    }
}