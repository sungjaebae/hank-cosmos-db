namespace HankCosmosDB.Todos
{
    //public record Todo(string Id, string title, string body, bool isCompleted);
    public record Todo
    {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public required string Body { get; init; }
        public required bool IsCompleted { get; init; }
    }
}
