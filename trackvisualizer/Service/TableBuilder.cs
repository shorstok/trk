using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trackvisualizer.Service
{
    public class TableBuilder
    {
        struct SparseItem
        {
            public string V;
            public int XCell;
            public int YCell;

            public int Xspan;
            public int Yspan;
        }

        public struct CondensedItem
        {
            public string V;

            public bool Skip;

            public int Xspan;
            public int Yspan;
        }

        readonly List<SparseItem> _rawUserData = new List<SparseItem>();

        public int GetFreeColumn()
        {
            int rt = 0;
            foreach (SparseItem s in _rawUserData)
            {
                if ((s.XCell + s.Xspan - 1) >= rt)
                    rt = (s.XCell + s.Xspan - 1) + 1;
            }
            return rt;
        }

        public void Setval(int x, int y, string val, int rowspan=1, int colspan=1)
        {
            foreach (SparseItem s in _rawUserData)
            {
                if (s.XCell == x && s.YCell == y)
                {
                    _rawUserData.Remove(s);
                    break;
                }
            }

            _rawUserData.Add(new SparseItem { XCell = x, YCell = y, V = val, Xspan = colspan, Yspan = rowspan});
        }

                
        /// <summary>
        /// removes empty virtual columns, makes table from cell array.
        /// </summary>        
        public CondensedItem[,] Condense()
        {
            Hashtable xCoords = new Hashtable(), yCoords = new Hashtable();
            List<int> absentY = new List<int>(),absentX = new List<int>();

            int xmax, ymax;

            if (_rawUserData.Count == 0)
                return null;

            var xmin = xmax = _rawUserData.First().XCell;
            var ymin = ymax = _rawUserData.First().YCell;

            foreach (SparseItem s in _rawUserData)
            {
                if (!xCoords.ContainsKey(s.XCell))
                    xCoords.Add(s.XCell, true);

                if(!yCoords.ContainsKey(s.YCell))
                    yCoords.Add(s.YCell, true);

                if (s.YCell < ymin)
                    ymin = s.YCell;
                if (s.XCell < xmin)
                    xmin = s.XCell;

                if (s.XCell > xmax)
                    xmax = s.XCell;
                if (s.YCell > ymax)
                    ymax = s.YCell;

                for (int c = 1; c < s.Xspan; ++c)
                {
                    if (!xCoords.ContainsKey(s.XCell + c))
                        xCoords.Add(s.XCell + c, true);

                    if (s.XCell + c > xmax)
                        xmax = s.XCell + c;
                }

                for (int c = 1; c < s.Yspan; ++c)
                {
                    if (!yCoords.ContainsKey(s.YCell + c))
                        yCoords.Add(s.YCell + c, true);

                    if (s.YCell + c > ymax)
                        ymax = s.YCell +c ;

                }
            }
            
            // find what columns / rows are 100% empty [17/12/2009 LysakA]

            for(int yc=ymin;yc!=(ymax+1);++yc)
            {
                if (!yCoords.ContainsKey(yc))
                    absentY.Add(yc);
            }

            for (int xc = xmin; xc != (xmax + 1); ++xc)
            {
                if (!xCoords.ContainsKey(xc))
                    absentX.Add(xc);
            }

            // real size [17/12/2009 LysakA]

            var xs = (xmax - xmin) - absentX.Count;
            var ys = (ymax - ymin) - absentY.Count;

            // correct cell indexes & make resulting table
            var condensedData = new CondensedItem[xs+1,ys+1];

            foreach (SparseItem s in _rawUserData)
            {
                var xnew = s.XCell - absentX.Count(x => x < s.XCell) - xmin;
                var ynew = s.YCell - absentY.Count(y => y < s.YCell) - ymin;

                condensedData[xnew,ynew] = new CondensedItem{ V = s.V, Xspan = s.Xspan, Yspan = s.Yspan};
            }

            return condensedData;
        }

    }


    
}
