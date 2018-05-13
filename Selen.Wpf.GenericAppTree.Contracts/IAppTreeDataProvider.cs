using System.Drawing;

namespace Airmech.Replays.GenericAppTree.Contracts
{
    public interface IAppTreeDataProvider
    {
        string Name { get; }
        string Path { get; }
        Bitmap Icon { get; }

        bool CanBeDisplayed { get; }
    }
}