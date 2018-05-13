using System.Drawing;

namespace Airmech.Replays.GenericAppTree
{
    public class AppTreeItem : AppTreeNode
    {
        public AppTreeItem(string selectionGroup, string header, object dataContext)
            : base(selectionGroup, header)
        {
            DataContext = dataContext;
        }

        public AppTreeItem(string selectionGroup, string header, object dataContext, Bitmap icon)
            : base(selectionGroup, header, icon)
        {
            DataContext = dataContext;
        }
    }
}