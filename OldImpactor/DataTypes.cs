using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldImpactor
{
    class DataTypes
    {
        public static string PATH = "REFERENCE\\repos\\OldImpactorCL\\OldImpactor\\bin\\Debug\\net6.0\\";
        public static string CLAUSEGEN_TPTP = "REFERENCE\\LLVerifier_BuildMay2024\\Run\\bin\\TPTPClausegen\\";


        /**
         * an enum representation of the outcome of each run of proveLadder
         */
        public enum Outcome
        {
            PROVEN_NO_ERROR,
            PROVEN_WITH_ERROR,
            NOT_PROVEN
        }

        /**
         * A variable found in the ubar process, stores a lot of information about where it was found
         */
        public struct UbarVar
        {
            public UbarVar(string name, int index, bool isLeftSide, string derivedFrom, int iteration)
            {
                this.name = name;
                this.index = index;
                this.iteration = iteration;
                this.isLeftSide = isLeftSide;
                this.derivedFrom = derivedFrom;
            }
            public string name;
            public int index;
            public int iteration;
            public bool isLeftSide;
            public string derivedFrom;

            public override string ToString()
            {
                return name + "," + index + "," + isLeftSide + "," + derivedFrom + "," + iteration;
            }
            public bool equals(UbarVar other)
            {
                return this.name == other.name && this.index == other.index;
            }

        }



        /**
         * a conceptual representation of a wt2 rung
         */
        public struct RawRung
        {
            public RawRung(List<string> eqauation, string coil)
            {
                this.eqauation = eqauation;
                this.coil = coil;
            }
            public List<string> eqauation;
            public string coil;

            public bool has(string str)
            {
                foreach (string line in eqauation)
                {
                    if (line.Contains('"') && line.Split('"')[1] == str) return true;
                }
                //Console.WriteLine(coil);
                return coilHas(str);
            }
            public bool coilHas(string str)
            {
                return coil.Contains('"') && coil.Split('"')[1] == str;
            }
            public string coilVar()
            {
                if (coil.Contains('"'))
                {
                    return coil.Split('"')[1];
                }
                return coil;
            }
            public int lowerIndexBound = -1;
            public int upperIndexBound = -1;
            public void determineBounds(string[] ladderLines)
            {
                int coilIndex = -1;
                for (int i = 0; i < ladderLines.Length; i++)
                {
                    if (ladderLines[i] == coil)
                    {
                        coilIndex = i;
                    }
                }
                lowerIndexBound = coilIndex;
                while (!ladderLines[lowerIndexBound].Contains("ORIGIN"))
                {
                    lowerIndexBound--;
                }
                upperIndexBound = coilIndex;
                while (!ladderLines[upperIndexBound].Contains("END RUNG"))
                {
                    upperIndexBound++;
                }
            }



            private List<string> vars = null;
            public List<string> variables()
            {
                if (vars == null)
                {
                    vars = new List<string>
                    {
                        
                    };
                    if (coil.Split('"').Length > 1) 
                    {
                        vars.Add(coil.Split('"')[1]);
                    }
                    foreach (string line in eqauation)
                    {
                        if (line.Contains('"'))
                        {
                            vars.Add(line.Split('"')[1]);
                        }

                    }
                }

                return vars;
            }
        }

        private static string[] filterDirs = new string[] {"2_2_56"};

        /**
         * using an LLVerifier generated directory of directories of cond files, we make a new directory containing only the ones that pass by default
         * we also make a lookup table so we can do allChapter testing later.
         */
        public static void findPassingConds(string ladderPath, string condDirPath)
        {
            int condNo = 0;
            List<string> lookup = new List<string>();
            List<string> condDirs = new List<string>(Directory.EnumerateDirectories(condDirPath));
            foreach (string condDir in condDirs)
            {
                if (filterDirs.Length > 0)
                {
                    bool cont = false;
                    foreach (string filter in filterDirs)
                    {
                        if (condDir.Contains(filter))
                        {
                            cont = true;
                            break;
                        }
                    }
                    if (!cont) { continue; }
                }


                List<string> conds = new List<string>(Directory.EnumerateFiles(condDir));

                foreach (string cond in conds)
                {
                    Console.WriteLine(cond);
                    ProcessStartInfo StartInfo = new ProcessStartInfo
                    {
                        FileName = CLAUSEGEN_TPTP + "clausegen.exe",
                        Arguments =
                       "--ladderfile=" + ladderPath +
                       " --safetyfile=" + cond +
                       " --proofstrategy=\"inductive\"" +
                       " --generateladder=\"yes\"" +
                       " --performslicing=\"no\"",
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardOutput = true,
                        WorkingDirectory = CLAUSEGEN_TPTP
                    };



                    bool shouldInclude = false;

                    Process process = Process.Start(StartInfo);
                    while (!process.StandardOutput.EndOfStream)
                    {
                        
                        string v = process.StandardOutput.ReadLine();
                        if (v.Contains("%")) 
                        {
                            shouldInclude = true;
                        }

                        if (v.Contains("CounterSatisfiable") || v.Contains("error"))
                        {
                            
                            shouldInclude = false;
                            break;
                        }

                    }
                    if (shouldInclude)
                    {
                        lookup.Add(condNo + " -> " + condDir);
                        File.Copy(cond, PATH + "ValidConds\\group" + condNo + ".cond");
                        condNo++;

                    }
                    else 
                    {
                        Console.WriteLine("Not Included");
                    }
                    process.Close();
                }

            }
            File.WriteAllLines(PATH + "LookupTable.txt", lookup);
        }

        public static Dictionary<string, List<int>> LOOKUP = null;
        /**
         * finds a lookupTable file that is arrow seperated much like a csv is comma seperated
         * COND NUMBER -> CHAPTER
         * becomes CHAPTERs are keys, containing a List of COND NUMBERS.
         * 
         * LOOKUP is a cache of this output.
         * the lookup table does not change throught the program, as it is determined by findPassingConds
         */
        public static Dictionary<string, List<int>> makeLookup(string lookupPath)
        {
            if (LOOKUP != null)
            {
                return LOOKUP;
            }
            Dictionary<string, List<int>> table = new Dictionary<string, List<int>>();

            string[] file = File.ReadAllLines(lookupPath);
            foreach (string line in file)
            {
                string[] v = line.Split(" -> ");
                if (table.ContainsKey(v[1]))
                {
                    List<int> conds = table[v[1]];
                    conds.Add(int.Parse(v[0]));
                }
                else
                {
                    table[v[1]] = new List<int>() { int.Parse(v[0]) };

                }
            }
            LOOKUP = table;
            return table;
        }

        /*
         * work out what chapter a cond is originally part of, assuming its in the lookup table *somewhere*
         */
        public static string getChapterFromCondPath(string condPath)
        {
            if (LOOKUP == null) 
            {
                makeLookup(PATH + "LookupTable.txt");
            }
            //REFERENCE\OldImpactor\OldImpactor\bin\Debug\net6.0\ValidConds\group72.cond
            string[] splitPath = condPath.Split("\\");
            int condNo = int.Parse(splitPath[splitPath.Length - 1].Replace("group", "").Replace(".cond", ""));

            foreach (string chapter in LOOKUP.Keys) 
            {
                foreach (int cond in LOOKUP[chapter]) 
                {
                    if (cond == condNo) 
                    {
                        return chapter;
                    }
                }
            }
            return null;
        }

    }


}
