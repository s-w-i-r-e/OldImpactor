using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OldImpactor.DataTypes;
using static OldInpactor.OldInpactor;

namespace OldImpactor
{

    /**
     * A rung shuffler can be used to shuffle...rungs....
     * it changes which variables are attatched to which cell in the rung
     * this can be used for other complex rung alterations
     */
    class RungShuffler
    {
        int CI = -1;
        RawRung rung;
        string[,] cells;
        int maxAnd = -1;
        int maxOr = -1;
        public RungShuffler(RawRung rung) 
        {
            this.rung = rung;

            //we don't know the dimensions of the cell matrix here, so we just go for the minimum guaranteed

            cells = new string[rung.eqauation.Count+5, rung.eqauation.Count+5];

            List<string> all = new List<string>(rung.eqauation)
            {
                rung.coil
            };

            foreach (string cell in all) 
            {
                string[] parts = cell.Split(" ");
                for(int i = 0; i < parts.Length; i++) 
                {
                    if (parts[i] == "CELL") 
                    {
                        CI = i;
                    }

                    if (CI != -1) 
                    {
                        //cell coordinates
                        if ((i - 1) == CI)
                        {
                            string[] rawCoords = parts[i].Replace("(","").Replace(")","").Split(",");

                            //technically int.Parse deals with leading zeros, however not always so we are just going to make it not a problem
                            //https://stackoverflow.com/questions/1677516/int-parse-with-leading-zeros
                            if (rawCoords[0].StartsWith("0")) 
                            {
                                rawCoords[0].Replace("0", "");
                            }
                            if (rawCoords[1].StartsWith("0"))
                            {
                                rawCoords[1].Replace("0", "");
                            }
                            int or = int.Parse(rawCoords[0]);
                            int and = int.Parse(rawCoords[1]);
                            if (or > maxOr) 
                            {
                                maxOr = or;
                            }
                            if (and > maxAnd) 
                            {
                                maxAnd = and;
                            }

                            //[CELL (02, 03) IS ]EMPTY WITH TOP LEFT LINKS
                            //Console.WriteLine(cell);
                            cells[or, and] 
                                = cell.Split(" IS ")[1];
                        }
                    }
                }
            }

            
        }

        /*
         * basically, we convert the matrix cells[,] to a slightly easier to iterate Dictionary<int[],string>
         */
        public Dictionary<int[], string> rebindCells() 
        {
            Dictionary<int[], string> reboundCells = new Dictionary<int[], string>();
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    if (cells[i, j] != null)
                    {
                        reboundCells[new int[] { i, j }] = cells[i, j];
                    }
                }
            }
            return reboundCells;
        }

        /*
         * The namesake method, takes each variable in a rung and puts it somewhere else in the rung (that is not the coil)
         */ 
        public RungShuffler reshuffle() 
        {
            Dictionary<int[], string> reboundCells = rebindCells();
            List<string> variables = new List<string>();
            foreach(int[] key in reboundCells.Keys) 
            {
                
                string v = reboundCells[key];
                if (v.Contains('"')) 
                {
                    string variable = v.Split('"')[1];
                    variables.Add(variable);
                }
                
            }
            Random r = new Random();
            foreach (int[] key in reboundCells.Keys)
            {
                string newCell = variables[r.Next(variables.Count)];
                string oldCell = reboundCells[key];
                while (newCell == oldCell) 
                {
                    newCell = variables[r.Next(variables.Count)];
                }

                if (oldCell.Contains('"'))
                {
                    string[] variable = oldCell.Split('"');
                    variable[1] = newCell;
                    string rebind = String.Join('"', variable);

                    reboundCells[key] = rebind;
                }
            }

            foreach (int[] key in reboundCells.Keys)
            {
                if (!reboundCells[key].Contains("COIL")) 
                {
                    cells[key[0], key[1]] = reboundCells[key];
                }
                
            }
            return this;
        }

        /*
         * an experiment more than anything else, fixing "broken" rungs using observations
         * not actually used right now.
         */
        public RungShuffler fix() 
        {
            //Observation: 0i,01 is always TOP BOTTOM LINKS
            writeToConsole("Observation 1: 0i,01 is always TOP BOTTOM LINKS");
            for(int i = 1; i<maxOr; i++) 
            {
                string[] cell = cells[i, 1].Split(" WITH ");
                cells[i, 1] = cell[0] + " WITH TOP BOTTOM LINKS";
            }

            //Observation the last element (the highest or and and) is always IS EMPTY WITH TOP LEFT LINKS if it is not at 01,XX
            if (maxOr > 1) 
            {
                writeToConsole("Observation 2: the last element (the highest or and and) is always IS EMPTY WITH TOP LEFT LINKS if it is not at 01,XX");
                
                int andLength = 1;
                while (cells[maxOr, andLength] != null) 
                {
                    andLength++;
                }
                andLength--;

                cells[maxOr, andLength] = cells[maxOr, andLength].Split(" IS ")[0] + " IS EMPTY WITH TOP LEFT LINKS";
            }
            //Observation EMPTY cells always have at least TOP links


            return this;
        }

        /*
         * re-write an inputted ladder using the rungs in this ladder, allowing for rung manipulation, like reshuffling
         */
        public string[] rewriteLadder(string[] ladderLines) 
        {
            rung.determineBounds(ladderLines);
            string[] writable = toWritableLines().ToArray();
            for (int i = 0; i < writable.Length; i++) 
            {
                ladderLines[i+rung.lowerIndexBound+1] = writable[i];
            } 
            return ladderLines;
        }

        /*
         * a utility method to output what this RungShuffler looks like by returning each cell as a string, like in a normal wt2 file.
         */
        public List<string> toWritableLines() 
        {
            List<string> lines = new List<string>();
            for(int or = 0; or < cells.GetLength(0); or++) 
            {
                for (int and = 0; and < cells.GetLength(1); and++) 
                {
                    string orw = "" + or;
                    string andw = "" + and;
                    if (orw.Length == 1) 
                    {
                        orw = "0" + orw;
                    }
                    if (andw.Length == 1)
                    {
                        andw = "0" + andw;
                    }


                    if (cells[or, and] != null) 
                    {
                        lines.Add("    CELL ("+orw+","+andw+") IS "+cells[or, and]);
                    }
                }
            }
            return lines;
        }
    }
}
