namespace SimApi.AuthSDK;

public class SimApiAuthIamDto
{
    public class PermissionItem
    {
        public required string Identifier { get; init; }
        public required string Name { get; init; }
        public required string Group { get; init; }
        public required string Description { get; init; }
    }
}