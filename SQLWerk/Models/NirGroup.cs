namespace SQLWerk.Models;

public class NirGroup
{
    // Код и название — одно пользовательское значение, например «Робототехника».
    public string Code { get; set; } = "";
}

public class NirGroupEntry
{
    public string GroupCode { get; set; } = "";
    public FullModel Exhibit { get; set; } = new();
}
