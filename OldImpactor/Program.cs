using OldImpactor;
using System.Collections;
using System.Diagnostics;
using static OldImpactor.DataTypes;
using static OldImpactor.UbarUtils;

namespace OldInpactor 
{
    class OldInpactor
    {
        private static List<Outcome> outcomes = new List<Outcome>();
        private static int totalFoundErrors = 0;
        private static List<string> injectionTypes = new List<string>();
        private static List<string> consoleOut = new List<string>();
        
        public static void Main(string[] args)
        {



            string ladderPath = PATH + "810.wt2";
            string condDirPath = PATH +"810_ValidConds\\";
            List<string> conds = new List<string>(Directory.EnumerateFiles(condDirPath));
            string[] ladderLines = File.ReadAllLines(ladderPath);
            //findPassingConds(ladderPath,condDirPath);



            Random random = new Random();
            List<RawRung> rungs = createRawLadder(ladderLines);
            

            for (int i = 0; i < 10; i++)
            {
                writeToConsole("Test " + i + ":");
                string cond = conds[random.Next(conds.Count)];
                writeToConsole("Using " + cond);
                InjectRelative(ladderPath, cond);
            }


            standardOut();
            

        }


        /*
         * A method to finalise data and save it
         */
        private static void standardOut() 
        {
            writeToConsole("total found errors: " + totalFoundErrors + "/100");
            writeToConsole("rungs with 1 equation: " + oneEquationCount);
            string output = PATH + "Outcomes.txt";
            string[] v = new string[outcomes.Count];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = outcomes[i].ToString();
            }
            File.WriteAllLines(output, v);
            File.WriteAllLines(PATH + "NoErrors.txt", noErrors);

            List<string> passes = new List<string>();
            foreach (string chapter in chapterPasses.Keys)
            {
                passes.Add(chapter + " -> " + chapterPasses[chapter][0] + "/" + chapterPasses[chapter][1]);
            }
            File.WriteAllLines(PATH + "ChapterPasses.txt", passes);

            int[] iPassed = new int[4];
            int[] iFailed = new int[4];
            foreach (string injectionType in injectionTypes)
            {
                string[] parts = injectionType.Split("->");
                //parts[0] is a number
                //in AllChapters, fails and passes are appended on one line per chapter tested
                //in a normal injection, parts.length=2 and 1 is just the pass or fail.
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i] == "FAILED")
                    {
                        iFailed[int.Parse(parts[0])]++;
                    }
                    else
                    {
                        iPassed[int.Parse(parts[0])]++;
                    }

                }
            }
            writeToConsole("0: Variable Name: passes-" + iPassed[0] + " fails-" + iFailed[0]);
            writeToConsole("1: Negation: passes-" + iPassed[1] + " fails-" + iFailed[1]);
            writeToConsole("2: shunt replace: passes-" + iPassed[2] + " fails-" + iFailed[2]);
            writeToConsole("3: reshuffle-" + iPassed[3] + " fails-" + iFailed[3]);

            File.WriteAllLines(PATH + "InjectionTypes.txt", injectionTypes);
            File.WriteAllLines(PATH + "ConsoleOutput.txt", consoleOut);
        }


        /*
         * A method used to test rungShuffling
         */
        private static void rungShufflingTests(List<RawRung> rungs, string[] ladderLines) 
        {
            RungShuffler rungShuffler = new RungShuffler(rungs[0]);

            foreach (string line in rungShuffler.toWritableLines())
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("=---=");
            foreach (string line in rungShuffler.reshuffle().toWritableLines())
            {
                Console.WriteLine(line);
            }
            File.WriteAllLines(PATH + "vibe.txt", rungShuffler.rewriteLadder(ladderLines));
        }

        /*
         * A method for obtaining a count of variables in a ladder
         */
        private static void superMap(List<RawRung> rungs) 
        {
           Dictionary<string,bool> superMap = new Dictionary<string, bool>();
           foreach(RawRung rung in rungs) 
           {
               superMap[rung.coilVar()] = true;
               foreach (string ble in rung.variables()) 
               {
                   superMap[ble]= true;
               }
           }
           foreach(string key in superMap.Keys) 
           {
               Console.WriteLine(key);
           }
           Console.WriteLine(superMap.Count);
        }

        /*
         * A modified version of a standard run that iterates through each chapter (or safety principle)
         */
        private static void chapterInjections(string condDirPath, string ladderPath, Random random) 
        {
           


           List<string> conds = new List<string>(Directory.EnumerateFiles(condDirPath));

           Dictionary<string,List<int>> condLookup = makeLookup(PATH+"\\LookupTable.txt");
          
           foreach (string chapter in condLookup.Keys) 
           {
               int detectedErrors = 0;
               for (int i = 0; i < 10; i++) 
               {
                   string condFilePath = condDirPath + condLookup[chapter][random.Next(condLookup[chapter].Count)];
                   bool errorDetected = InjectRelative(ladderPath, condFilePath);
                   if (errorDetected) { detectedErrors++; }
               }
               writeToConsole(detectedErrors + "/10 errors detected in this chapter");
           }
        }

        /*
         * A method for generating statistics about ladders
         */
        private static void ladderStats(List<RawRung> rungs, string[] ladderLines, List<string> conds) 
        {
            
            int c = 0;
            int highestIteration = 0;
            int totalUbarVars = 0;
            int ubarVarTotal = 0;
            int highestTotalVariables = 0;
            string highestIterationName = "";
            string highestIterationCond = "";
            
            foreach(string cond in conds) 
            {
                string condF = File.ReadAllLines(cond)[4];

                List<UbarVar> ubarConds = getVariablesFromCondAsUbars(condF);
                findUbarBackwards(rungs, ubarConds);
                Console.WriteLine(UBAR.Count);
                foreach (UbarVar ubv in UBAR) 
                {
                    if (ubv.iteration > highestIteration) 
                    {
                        highestIteration = ubv.iteration;
                        highestIterationName = ubv.name;
                        highestIterationCond = cond;
                    }
                    totalUbarVars++;
                    ubarVarTotal += ubv.iteration;
                }
                if(UBAR.Count > highestTotalVariables) 
                {
                    highestTotalVariables = UBAR.Count;
                   
                }
                dumpUbar(c);
                
                c++;
                
            }
            Console.WriteLine("Highest amount of variables in a cond's Ubar: " + highestTotalVariables);
            Console.WriteLine("Highest iteration: "+highestIterationName+" with "+highestIteration+" in "+highestIterationCond);
            Console.WriteLine("Average iteration: " + ubarVarTotal/totalUbarVars);
        }



        /*
         * I dislike C#'s output system, if you press a key it closes the terminal, losing the output forever
         * hence everything in the program uses writeToConsole so I can save it.
         */
        public static void writeToConsole(string v) 
        {
            consoleOut.Add(v);
            Console.WriteLine(v);
        }

        //LADDER METHODS//

        /**
         * takes the path to a ladder and makes a slightly more manageable version in Object code
         * RawRung has a bunch of useful methods for working with ladders in code
         */
        public static List<RawRung> createRawLadder(string[] ladderLines)
        {
            List<RawRung> rawRungs = new List<RawRung>();

            for (int i = 0; i < ladderLines.Length; i++)
            {
                if (ladderLines[i].Contains("ORIGIN"))
                {
                    string coil = "";
                    List<string> rung = new List<string>();
                    i++;
                    while (ladderLines[i].Contains("CELL"))
                    {
                        if (ladderLines[i].Contains("COIL"))
                        {
                            coil = ladderLines[i];
                        }
                        else
                        {
                            rung.Add(ladderLines[i]);
                        }
                        i++;

                    }
                    rawRungs.Add(new RawRung(rung, coil));
                }
            }


            return rawRungs;
        }


        /**
         * Uses Clausegen_TPTP to prove the ladder at ladderPath using the cond file at propertyPath dumping the results at dumpResultPath
         * -Note: dumpResultPath should not end with .txt (since inductive and bmc are both valid outputs)
         */
        public static bool proveLadder(string ladderPath, string propertyPath, string dumpResultPath)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo
            {
                FileName = CLAUSEGEN_TPTP + "clausegen.exe",
                Arguments =
                       " --ladderfile=" + ladderPath +
                       " --safetyfile=" + propertyPath +
                       " --proofstrategy=\"inductive\"" +
                       " --bound=\"0\"" +
                       " --performslicing=\"no\"" +
                       " --generateladder=\"yes\"",
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                WorkingDirectory = CLAUSEGEN_TPTP
            };
            //writeToConsole(StartInfo.FileName+" "+ StartInfo.Arguments);

            List<string> contents = new List<string>();

            bool shouldBMC = false;

            Process process = Process.Start(StartInfo);
            while (!process.StandardOutput.EndOfStream)
            {
                string v = process.StandardOutput.ReadLine();
                writeToConsole("Clausegen: " + v);


                contents.Add(v);
                if (v.Contains("CounterSatisfiable"))
                {
                    shouldBMC = true;
                }
            }
            if (contents.Count == 0)
            {
                writeToConsole("There looks like there was an inductive error, let's try bmc...");
                shouldBMC = true;
            }
            File.WriteAllLines(dumpResultPath + "_induction.txt", contents);
            process.Close();

            if (shouldBMC)
            {
                ProcessStartInfo StartInfo2 = new ProcessStartInfo
                {
                    FileName = CLAUSEGEN_TPTP + "clausegen.exe",
                    Arguments =
                       "--ladderfile=" + ladderPath +
                       " --safetyfile=" + propertyPath +
                       " --proofstrategy=\"bmc\"" +
                       " --bound=\"10\"" +
                       " --performslicing=\"no\"" +
                       " --generateladder=\"yes\"",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    WorkingDirectory = CLAUSEGEN_TPTP
                };

                List<string> contents2 = new List<string>();

                bool hasFailed = false;


                Process process2 = Process.Start(StartInfo2);
                writeToConsole("BMC Check, only printing if counterexample found:");
                while (!process2.StandardOutput.EndOfStream)
                {
                    string v = process2.StandardOutput.ReadLine();

                    contents2.Add(v);
                    if (v.Contains("CounterSatisfiable"))
                    {
                        hasFailed = true;
                        writeToConsole("Clausegen: " + v);
                    }



                }
                File.WriteAllLines(dumpResultPath + "_bmc.txt", contents2);
                process2.Close();

                if (hasFailed)
                {
                    writeToConsole("Failed Inductive and found BMC Counterexmple (fault detected)");
                    outcomes.Add(Outcome.PROVEN_WITH_ERROR);
                }
                else
                {
                    writeToConsole("Failed Inductive but no BMC Counterexample, we don't know if this ladder has an error");
                    outcomes.Add(Outcome.NOT_PROVEN);
                }
            }
            else
            {
                writeToConsole("Passed Inductive verification, hence this ladder is proved correct");
                outcomes.Add(Outcome.PROVEN_NO_ERROR);
            }
            return shouldBMC;
        }



        //INJECTION SETUP METHODS//
        //these methods call injection implementation methods.

        /**
         * Inject an error into the ladder at ladderPath, with regards to the cond file at condPath
         * for: rh0
         */
        public static bool InjectRelative(string ladderPath, string condPath)
        {
            string[] ladderLines = File.ReadAllLines(ladderPath);

            string cond = File.ReadAllLines(condPath)[4];

            List<RawRung> rawLadder = createRawLadder(ladderLines);

            List<UbarVar> ubarConds = getVariablesFromCondAsUbars(cond);
            findUbarBackwards(rawLadder, ubarConds);

           


            //dumpUbar();
            string brokenLadderPath = injectFault(rawLadder, ladderPath, ladderLines);
            while(brokenLadderPath == null) 
            {
                repCount = 0;
                writeToConsole("looks like that didnt work, let's try another fault");
                brokenLadderPath = injectFault(rawLadder, ladderPath, ladderLines);
            }
            if (brokenLadderPath == "IMPOSSIBLE") 
            {
                writeToConsole("We found a cond that doesnt exist as the only Ubar, this test is impossible, failing...");
                return false;
            }

            bool v = proveLadder(brokenLadderPath, condPath, PATH + "Data\\detectable"
                + DateTime.Now.Day + "-"
                + DateTime.Now.Month + "-"
                + DateTime.Now.Year + "At"
                + DateTime.Now.Hour + "-"
                + DateTime.Now.Minute + "-"
                + DateTime.Now.Second);

            countChapterPassesNormal(v, condPath);




            writeToConsole("On Path: " + brokenLadderPath);
            writeToConsole("----------");
            return v;
        }


        /**
         * A modified version of InjectRelative, every cond in a chapter is checked instead of just the found cond
         * - requires a lookup table generated using findPassingConds (parsed in using makeLookup inside this method)
         * 
         * 
         */
        public static void InjectRelativeAllChapters(string ladderPath, string condPath)
        {
            string[] ladderLines = File.ReadAllLines(ladderPath);

            string cond = File.ReadAllLines(condPath)[4];

            List<RawRung> rawLadder = createRawLadder(ladderLines);

            List<UbarVar> ubarConds = getVariablesFromCondAsUbars(cond);
            findUbarBackwards(rawLadder, ubarConds);

            doUbarFiltering(0);



            //dumpUbar();
            string brokenLadderPath = injectFault(rawLadder, ladderPath, ladderLines);


            int condNumber = int.Parse(condPath.Split("group")[1].Replace(".cond",""));
            Dictionary<string, List<int>> table = makeLookup(PATH + "LookupTable.txt");
            string chapter = null;
            foreach (string key in table.Keys)
            {
                foreach (int lookupCondNo in table[key])
                {
                    if (lookupCondNo == condNumber)
                    {
                        chapter = key;
                        break;
                    }
                }
                if (chapter != null)
                {
                    break;
                }
            }
            if (chapter != null)
            {
                foreach (int lookupCondNo in table[chapter])
                {
                    writeToConsole("Testing conds in chapter " + chapter + ": " + lookupCondNo);
                    string rebuiltCondPath = PATH+"ValidConds\\group" + lookupCondNo + ".cond";
                    bool v = proveLadder(brokenLadderPath, rebuiltCondPath, PATH + "Data\\detectable"
                    + DateTime.Now.Day + "-"
                    + DateTime.Now.Month + "-"
                    + DateTime.Now.Year + "At"
                    + DateTime.Now.Hour + "-"
                    + DateTime.Now.Minute + "-"
                    + DateTime.Now.Second);
                    countChapterPassesNormal(v, rebuiltCondPath);
                }
            }
            else
            {
                writeToConsole("Something went wrong, just proving this cond");
                bool v = proveLadder(brokenLadderPath, condPath, PATH + "Data\\detectable"
                + DateTime.Now.Day + "-"
                + DateTime.Now.Month + "-"
                + DateTime.Now.Year + "At"
                + DateTime.Now.Hour + "-"
                + DateTime.Now.Minute + "-"
                + DateTime.Now.Second);
                countChapterPassesNormal(v, condPath);
            }




            writeToConsole("On Path: " + brokenLadderPath);
            writeToConsole("----------");

        }


        //INJECTION SETUP-IMPLEMENTATION COMBINED METHODS//
        private static int oneEquationCount = 0;
        private static List<string> noErrors = new List<string>();
        private static Dictionary<string, int[]> chapterPasses = new Dictionary<string, int[]>();


        /*
         * take a cond and a ladder, find a rung and inject errors into the rung until it breaks
         * or we run out of cells to inject errors into
         */
        public static void doSystematicNegation(string ladderPath, string condPath)
        {

            //SETUP
            string[] ladderLines = File.ReadAllLines(ladderPath);

            string[] outLines = new string[ladderLines.Length];
            ladderLines.CopyTo(outLines, 0);

            string cond = File.ReadAllLines(condPath)[4];

            List<RawRung> rawLadder = createRawLadder(ladderLines);

            List<UbarVar> ubarConds = getVariablesFromCondAsUbars(cond);
            findUbarBackwards(rawLadder, ubarConds);

            doUbarFiltering(0);

            //implementation
            Random random = new Random();
            UbarVar varData = UBAR[random.Next(UBAR.Count)];
            writeToConsole("using " + varData.name + " from iteration " + varData.iteration);

            string copyDest = ladderPath.Replace(".wt2", "") + "_modified_" + varData.index + ".wt2";


            bool foundRung = false;
            RawRung injectionRung = rawLadder[0];

            foreach (RawRung rung in rawLadder)
            {
                if (rung.coilHas(varData.name))
                {
                    injectionRung = rung;
                    foundRung = true;
                }
            }

            injectionRung.determineBounds(outLines);
            writeToConsole("Injection rung with coil: " + injectionRung.coil + " with index bounds " + injectionRung.lowerIndexBound + " and " + injectionRung.upperIndexBound);

            if (foundRung)
            {
                if (injectionRung.eqauation.Count < 2)
                {
                    oneEquationCount++;
                    writeToConsole("One equation detected");
                }
                bool wasErrorFound = false;
                for (int e = 0; e < injectionRung.eqauation.Count; e++)
                {
                    //writeToConsole("refreshed ladder");
                    //outLines = new string[ladderLines.Length];
                    //ladderLines.CopyTo(outLines, 0);

                    writeToConsole("Equation no. " + e + "/" + injectionRung.eqauation.Count);
                    if (injectionRung.eqauation[e].Contains("NORMALLY_OPEN") || injectionRung.eqauation[e].Contains("NORMALLY_CLOSED"))
                    {
                        writeToConsole("Negation Fault");
                        string line = injectionRung.eqauation[e];


                        writeToConsole("at line: " + line);
                        int changingIndex = dynamicSearch(outLines, injectionRung, line);

                        writeToConsole("found at line number " + (changingIndex + 1));
                        if (line.Contains("NORMALLY_OPEN"))
                        {
                            line = line.Replace("NORMALLY_OPEN", "NORMALLY_CLOSED");
                        }
                        else
                        {
                            line = line.Replace("NORMALLY_CLOSED", "NORMALLY_OPEN");
                        }
                        writeToConsole("Now looks like: " + line);
                        outLines[changingIndex] = line;

                        File.WriteAllLines(copyDest, outLines);

                        bool cont = proveLadder(copyDest, condPath, PATH + "Data\\detectable"
                        + DateTime.Now.Day + "-"
                        + DateTime.Now.Month + "-"
                        + DateTime.Now.Year + "At"
                        + DateTime.Now.Hour + "-"
                        + DateTime.Now.Minute + "-"
                        + DateTime.Now.Second);
                        if (cont)
                        {
                            writeToConsole("error found, moving on...");
                            totalFoundErrors++;
                            wasErrorFound = true;
                            break;
                        }
                    }
                    else
                    {
                        writeToConsole("Not negatable, moving on...");
                    }

                }
                countChapterPassesSystematic(wasErrorFound, condPath, injectionRung);

            }
            else
            {
                writeToConsole("did not find a rung with " + varData.name + " as the coil...");
            }

        }

        /*
         * same as above but we use Variable Name Fault as opposed to Negation Fault
         */
        public static void doSystematicRename(string ladderPath, string condPath)
        {

            //SETUP
            string[] ladderLines = File.ReadAllLines(ladderPath);

            string[] outLines = new string[ladderLines.Length];
            ladderLines.CopyTo(outLines, 0);

            string cond = File.ReadAllLines(condPath)[4];

            List<RawRung> rawLadder = createRawLadder(ladderLines);

            List<UbarVar> ubarConds = getVariablesFromCondAsUbars(cond);
            findUbarBackwards(rawLadder, ubarConds);

            doUbarFiltering(0);

            //implementation
            Random random = new Random();
            UbarVar varData = UBAR[random.Next(UBAR.Count)];
            writeToConsole("using " + varData.name + " from iteration " + varData.iteration);

            string copyDest = ladderPath.Replace(".wt2", "") + "_modified_" + varData.index + ".wt2";


            bool foundRung = false;
            RawRung injectionRung = rawLadder[0];

            foreach (RawRung rung in rawLadder)
            {
                if (rung.coilHas(varData.name))
                {
                    injectionRung = rung;
                    foundRung = true;
                }
            }

            injectionRung.determineBounds(outLines);
            writeToConsole("Injection rung with coil: " + injectionRung.coil + " with index bounds " + injectionRung.lowerIndexBound + " and " + injectionRung.upperIndexBound);

            if (foundRung)
            {
                if (injectionRung.eqauation.Count < 2)
                {
                    oneEquationCount++;
                    writeToConsole("One equation detected");
                }
                bool wasErrorFound = false;
                for (int e = 0; e < injectionRung.eqauation.Count; e++)
                {
                    //writeToConsole("refreshed ladder");
                    //outLines = new string[ladderLines.Length];
                    //ladderLines.CopyTo(outLines, 0);

                    writeToConsole("Equation no. " + e + "/" + injectionRung.eqauation.Count);
                    if (injectionRung.eqauation[e].Contains("NORMALLY_OPEN") || injectionRung.eqauation[e].Contains("NORMALLY_CLOSED"))
                    {
                        writeToConsole("Name Fault");
                        string line = injectionRung.eqauation[e];


                        writeToConsole("at line: " + line);
                        int changingIndex = dynamicSearch(outLines, injectionRung, line);

                        writeToConsole("found at line number " + (changingIndex + 1));

                        ///
                        string cvar = line.Split('"')[1];
                        line = line.Replace(cvar, OriginalUBAR[random.Next(OriginalUBAR.Count)].name);
                        ///

                        writeToConsole("Now looks like: " + line);
                        outLines[changingIndex] = line;

                        File.WriteAllLines(copyDest, outLines);

                        bool cont = proveLadder(copyDest, condPath, PATH + "Data\\detectable"
                        + DateTime.Now.Day + "-"
                        + DateTime.Now.Month + "-"
                        + DateTime.Now.Year + "At"
                        + DateTime.Now.Hour + "-"
                        + DateTime.Now.Minute + "-"
                        + DateTime.Now.Second);
                        if (cont)
                        {
                            writeToConsole("error found, moving on...");
                            totalFoundErrors++;
                            wasErrorFound = true;
                            break;
                        }
                    }
                    else
                    {
                        writeToConsole("Not negatable, moving on...");
                    }

                }
                countChapterPassesSystematic(wasErrorFound, condPath, injectionRung);

            }
            else
            {
                writeToConsole("did not find a rung with " + varData.name + " as the coil...");
            }

        }



        //INJECTION IMPLEMENTATION METHODS//

        /**
         * UBAR should be populated before this is called
         * impl of InjectRelative, returns the file location of the "broken" ladder
         */

        private static int repCount = 0;
        private static string injectFault(List<RawRung> rawLadder, string ladderPath, string[] ladderLines)
        {

            string[] outLines = new string[ladderLines.Length];
            ladderLines.CopyTo(outLines, 0);

            Random random = new Random();
            writeToConsole("UBAR Count: " + UBAR.Count);
            if (UBAR.Count == 0)
            {
                return "IMPOSSIBLE";
            }
            if (UBAR.Count < 2) 
            {
                writeToConsole("UBAR is too small for meaningful testing");
                repCount++;
                if (repCount > 5) 
                {
                    return null;
                }
            }
            
            
            UbarVar varData = UBAR[random.Next(UBAR.Count)];
            writeToConsole("using " + varData.name + " from iteration " + varData.iteration);

            string copyDest = ladderPath.Replace(".wt2", "") + "_modified_" + varData.index + ".wt2";


            bool foundRung = false;
            RawRung injectionRung = rawLadder[0];

            foreach (RawRung rung in rawLadder)
            {
                if (rung.coilHas(varData.name))
                {
                    injectionRung = rung;
                    foundRung = true;
                }
            }

            if (!foundRung)
            {
                writeToConsole("did not find a coil rung with variable name " + varData.name + " so we will try again...");
                repCount++;
                if (repCount > 5)
                {
                    //AFTER NOTE: repCount wasn't always set back to 0 here. this means there was a circumstance where a test was deemed impossible
                    //before trying 5 times (i.e it failed after the first try was undetected)
                    repCount = 0;
                    return "IMPOSSIBLE";
                }
                return injectFault(rawLadder, ladderPath, ladderLines);
            }
            if (injectionRung.coil == "") 
            {
                writeToConsole("This rung has no coil! trying again...");
                return injectFault(rawLadder, ladderPath, ladderLines);
            }

            writeToConsole("Injecting fault at rung with coil: " + injectionRung.coil);

            int fixedSwitch = random.Next(4);
            injectionTypes.Add("" + fixedSwitch);
            switch (fixedSwitch)
            {
                case 1:
                    writeToConsole("Negation Fault");
                    string line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    while (!line.Contains("NORMALLY_OPEN") && !line.Contains("NORMALLY_CLOSED"))
                    {
                        line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    }
                    writeToConsole("at line: " + line);

                    int changingIndex = dynamicSearch(outLines, injectionRung, line);

                    writeToConsole("found at line number " + (changingIndex + 1));
                    if (line.Contains("NORMALLY_OPEN"))
                    {
                        line = line.Replace("NORMALLY_OPEN", "NORMALLY_CLOSED");
                    }
                    else
                    {
                        line = line.Replace("NORMALLY_CLOSED", "NORMALLY_OPEN");
                    }
                    writeToConsole("Now looks like: " + line);
                    outLines[changingIndex] = line;

                    break;
                case 2:
                    //This seems to be the best equivalent to insert/remove subexpression right now.
                    writeToConsole("replace item with shunt or vv. Fault");
                    line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    while (line.Contains(injectionRung.coilVar()))
                    {
                        line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    }
                    writeToConsole("at line: " + line);

                    changingIndex = dynamicSearch(outLines, injectionRung, line);

                    writeToConsole("found at line number " + (changingIndex + 1));
                    if (line.Contains("HORIZONTAL_SHUNT"))
                    {
                        if (random.Next(2) == 0)
                        {
                            line = line.Replace("HORIZONTAL_SHUNT", "NORMALLY_OPEN " + '"' + UBAR[random.Next(UBAR.Count)].name + '"' + " ");
                        }
                        else
                        {
                            line = line.Replace("HORIZONTAL_SHUNT", "NORMALLY_CLOSED " + '"' + UBAR[random.Next(UBAR.Count)].name + '"' + " ");
                        }

                    }
                    else if (line.Contains("EMPTY"))
                    {
                        if (random.Next(2) == 0)
                        {
                            line = line.Replace("EMPTY", "NORMALLY_OPEN " + '"' + UBAR[random.Next(UBAR.Count)].name + '"' + " ");
                        }
                        else
                        {
                            line = line.Replace("EMPTY", "NORMALLY_CLOSED " + '"' + UBAR[random.Next(UBAR.Count)].name + '"' + " ");
                        }

                    }
                    else
                    {
                        string[] v = line.Split('"');
                        line = v[0].Replace("NORMALLY_OPEN ", "").Replace("NORMALLY_CLOSED ", "") + "HORIZONTAL_SHUNT" + v[2];
                    }
                    writeToConsole("Now looks like: " + line);
                    outLines[changingIndex] = line;
                    break;
                case 3:
                    writeToConsole("Rung re-shuffle fault");
                    
                    RungShuffler rungShuffler = new RungShuffler(injectionRung);
                    foreach (string v in rungShuffler.toWritableLines())
                    {
                        writeToConsole(v);
                    }
                    writeToConsole("Now looks like: ");
                    rungShuffler.reshuffle();
                    foreach (string v in rungShuffler.toWritableLines())
                    {
                        writeToConsole(v);
                    }
                    outLines=rungShuffler.rewriteLadder(outLines);
                    break;

                default:
                    writeToConsole("Variable Name Fault");
                    line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    while (!line.Contains("NORMALLY_OPEN") && !line.Contains("NORMALLY_CLOSED"))
                    {
                        line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    }
                    writeToConsole("at line: " + line);

                    changingIndex = dynamicSearch(outLines,injectionRung, line);

                    writeToConsole("found at line number " + (changingIndex + 1));
                    string cvar = line.Split('"')[1];
                    line = line.Replace(cvar, OriginalUBAR[random.Next(OriginalUBAR.Count)].name);
                    
                    writeToConsole("Now looks like: " + line);
                    outLines[changingIndex] = line;

                    break;

            }
            File.WriteAllLines(copyDest, outLines);



            return copyDest;
        }

        
        /**
        * A modified version of InjectFault, but specifically for RH1
        * specifically only injects faults outside of UBAR
        * 
        * Doesnt technically have all the new output systems, probably doesnt need them.
        */
        private static string injectFaultNotInUbar(List<RawRung> rawLadder, string ladderPath, string[] ladderLines)
        {

            string[] outLines = new string[ladderLines.Length];
            ladderLines.CopyTo(outLines, 0);

            Random random = new Random();
            List<RawRung> notInUbar = findNotUbar(rawLadder);
            RawRung injectionRung = notInUbar[random.Next(notInUbar.Count)];
            while (injectionRung.variables().Count < 1)
            {
                injectionRung = notInUbar[random.Next(notInUbar.Count)];
                writeToConsole("No variables in " + injectionRung.coil + " looking for another...");
            }

            string copyDest = ladderPath.Replace(".wt2", "") + "_modified_" + random.Next(1000) + ".wt2";


            if (injectionRung.coil == "" || injectionRung.coil.Replace(" ", "") == "") 
            {
                Console.WriteLine("Empty coil! - trying again");
                return injectFaultNotInUbar(rawLadder, ladderPath, ladderLines);
            }


            writeToConsole("Injecting fault at rung with coil: " + injectionRung.coil);

            int fixedSwitch = random.Next(3);
            injectionTypes.Add("" + fixedSwitch);
            switch (fixedSwitch)
            {
                case 1:
                    writeToConsole("Negation Fault");
                    string line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    while (!line.Contains("NORMALLY_OPEN") && !line.Contains("NORMALLY_CLOSED"))
                    {
                        line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    }
                    writeToConsole("at line: " + line);
                    int changingIndex = -1;
                    for (int i = 0; i < outLines.Length; i++)
                    {
                        if (outLines[i] == line)
                        {
                            changingIndex = i;
                            break;
                        }
                    }
                    writeToConsole("found at line number " + (changingIndex + 1));
                    if (line.Contains("NORMALLY_OPEN"))
                    {
                        line = line.Replace("NORMALLY_OPEN", "NORMALLY_CLOSED");
                    }
                    else
                    {
                        line = line.Replace("NORMALLY_CLOSED", "NORMALLY_OPEN");
                    }
                    writeToConsole("Now looks like: " + line);
                    outLines[changingIndex] = line;

                    break;
              
                case 2:
                    writeToConsole("Rung re-shuffle fault");

                    RungShuffler rungShuffler = new RungShuffler(injectionRung);
                    foreach (string v in rungShuffler.toWritableLines())
                    {
                        writeToConsole(v);
                    }   
                    writeToConsole("Now looks like: ");
                    rungShuffler.reshuffle();
                    foreach (string v in rungShuffler.toWritableLines())
                    {
                        writeToConsole(v);
                    }
                    outLines = rungShuffler.rewriteLadder(outLines);
                    break;
                default:
                    writeToConsole("Variable Name Fault");
                    line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    while (!line.Contains("NORMALLY_OPEN") && !line.Contains("NORMALLY_CLOSED"))
                    {
                        line = injectionRung.eqauation[random.Next(injectionRung.eqauation.Count)];
                    }
                    writeToConsole("at line: " + line);
                    changingIndex = -1;
                    for (int i = 0; i < outLines.Length; i++)
                    {
                        if (outLines[i] == line)
                        {
                            changingIndex = i;
                            break;
                        }
                    }
                    writeToConsole("found at line number " + (changingIndex + 1));
                    string cvar = line.Split('"')[1];
                    line = line.Replace(cvar, UBAR[random.Next(UBAR.Count)].name);
                    
                    writeToConsole("Now looks like: " + line);
                    outLines[changingIndex] = line;

                    break;

            }
            File.WriteAllLines(copyDest, outLines);



            return copyDest;
        }

        

        //LADDER UTILITY METHODS//
        
        /*
         * work out how many errors are detected per chapter
         */
        private static void countChapterPassesSystematic(bool wasErrorFound,string condPath, RawRung injectionRung)
        {
            if (!wasErrorFound)
            {
                string chapter = getChapterFromCondPath(condPath);
                writeToConsole("Error not found after negating all equations, will save for later scruitiny...");
                string[] context = new string[] { condPath, injectionRung.coil };
                noErrors.Add("FAILED: " + condPath + " (" + chapter + ") ->" + injectionRung.coil);

                if (chapterPasses.ContainsKey(chapter))
                {
                    int[] x = chapterPasses[chapter];

                    x[1] += 1;
                    chapterPasses[chapter] = x;
                }
                else
                {
                    int[] x = new int[] { 0, 1 };
                    chapterPasses[chapter] = x;
                }
            }
            else
            {
                string chapter = getChapterFromCondPath(condPath);
                noErrors.Add("passed: " + condPath + " (" + chapter + ") ->" + injectionRung.coil);

                if (chapterPasses.ContainsKey(chapter))
                {
                    int[] x = chapterPasses[chapter];
                    x[0] += 1;
                    x[1] += 1;
                    chapterPasses[chapter] = x;
                }
                else
                {
                    int[] x = new int[] { 1, 1 };
                    chapterPasses[chapter] = x;
                }
            }
        }

        /*
         * same as above, but without the need for an InjectionRung (mainly because we are likely using all 4 of the error types here)
         */
        private static void countChapterPassesNormal(bool wasErrorFound, string condPath) 
        {
            if (!wasErrorFound)
            {
                string chapter = getChapterFromCondPath(condPath);
                //writeToConsole("Error not found after negating all equations, will save for later scruitiny...");

                injectionTypes[injectionTypes.Count - 1] = injectionTypes[injectionTypes.Count - 1] + "->FAILED";
                noErrors.Add("FAILED: " + condPath + " (" + chapter + ")");

                if (chapterPasses.ContainsKey(chapter))
                {
                    int[] x = chapterPasses[chapter];

                    x[1] += 1;
                    chapterPasses[chapter] = x;
                }
                else
                {
                    int[] x = new int[] { 0, 1 };
                    chapterPasses[chapter] = x;
                }
            }
            else
            {
                totalFoundErrors++;
                string chapter = getChapterFromCondPath(condPath);
                injectionTypes[injectionTypes.Count - 1] = injectionTypes[injectionTypes.Count - 1] + "->passed";
                noErrors.Add("passed: " + condPath + " (" + chapter + ")");

                if (chapterPasses.ContainsKey(chapter))
                {
                    int[] x = chapterPasses[chapter];
                    x[0] += 1;
                    x[1] += 1;
                    chapterPasses[chapter] = x;
                }
                else
                {
                    int[] x = new int[] { 1, 1 };
                    chapterPasses[chapter] = x;
                }
            }
        }

        /*
         * used to put rung data back into the ladderLines to write to a wt2 file
         */
        private static int dynamicSearch(string[] outLines, RawRung rung, string searchable)
        {
            if (rung.upperIndexBound == -1)
            {
                rung.determineBounds(outLines);
            }
            for (int i = rung.lowerIndexBound; i < rung.upperIndexBound; i++)
            {
                if (outLines[i] == searchable)
                {
                    return i;

                }
            }
            return -1;
        }

    }



}
