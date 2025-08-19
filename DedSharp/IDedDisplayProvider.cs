using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedSharp
{
    public interface IDedDisplayProvider {
        public bool IsPixelOn(int row, int column);
        public bool RowNeedsUpdate(int row);
    }
}
