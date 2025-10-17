namespace SzConnyApp.SenzingV4.Commands;

public interface IGetEntityCommand : ICommand
{
    long EntityId { get; set; }
}