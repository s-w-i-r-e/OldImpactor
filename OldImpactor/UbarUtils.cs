using static OldImpactor.DataTypes;

namespace OldImpactor
{
    class UbarUtils
    {
        public static List<UbarVar> UBAR = new List<UbarVar>();

        /**
         * find all variables in Ubar according to Phil James' Algorithm:
         * - the coil occurs within the safety condition (parameter: sliceable)
         * - if a rung is already in Ubar, then any run that depends on that rung should also be added
         * - we start from the bottom of the ladder, as order matters in ladder logic and work our way up, adding new variables as we go
         */
        public static void findUbarBackwards(List<RawRung> ladder, List<UbarVar> sliceable)
        {
            UBAR = new List<UbarVar>();

            for (int i = ladder.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < sliceable.Count; j++)
                {
                    //if one of the known variables is the coil of this (i) ladder rung
                    if (ladder[i].coilHas(sliceable[j].name))
                    {
                        foreach (string variable in ladder[i].variables())
                        {
                            if (notInUbarList(sliceable, variable))
                            {
                                sliceable.Add(new UbarVar(variable, i, sliceable[j].isLeftSide, sliceable[j].name, sliceable[j].iteration + 1));
                            }
                        }
                    }
                }
            }
            UBAR = sliceable;
            OriginalUBAR = new List<UbarVar>(UBAR);
        }

        /*
         * make a ladder where only things not in UBAR exist in it.
         */
        public static List<RawRung> findNotUbar(List<RawRung> ladder)
        {

            List<RawRung> rungs = new List<RawRung>(ladder);
            foreach (RawRung rawRung in ladder)
            {
                foreach (UbarVar ubarVar in UBAR)
                {
                    if (rawRung.coilHas(ubarVar.name))
                    {
                        rungs.Remove(rawRung);
                    }
                }
            }
            return rungs;
        }

        /**
         * UbarVar stores a lot of information about variables, we take the cond (at this point it is a single string containing just the cond)
         * and then we split it into a UbarVar, the -1 refers to the rung index and the null refers to the lack of a derived variable
         */
        public static List<UbarVar> getVariablesFromCondAsUbars(string cond)
        {
            List<UbarVar> vars = new List<UbarVar>();

            string[] leftRight = cond.Split("->");
            for (int i = 0; i < leftRight.Length; i++)
            {
                //Console.WriteLine(leftRight[i]);
                string[] parts = cond.Split('"');
                foreach (string part in parts)
                {
                    if (part.EndsWith("_0") || part.EndsWith("_1"))
                    {

                        vars.Add(new UbarVar(part.Replace("_0", "").Replace("_1", ""), -1, i == 0, null, 0));
                    }
                }
            }



            return vars;
        }

        /*
         * as above, but we only want to find things on the left side of the cond.
         */
        public static List<UbarVar> getVariablesFromCondAsUbarsLeftBias(string cond)
        {
            List<UbarVar> vars = new List<UbarVar>();

            string[] leftRight = cond.Split("->");
            //Console.WriteLine(leftRight[i]);
            string[] parts = leftRight[0].Split('"');
            foreach (string part in parts)
            {
                if (part.EndsWith("_0") || part.EndsWith("_1"))
                {

                    vars.Add(new UbarVar(part.Replace("_0", "").Replace("_1", ""), -1, true, null, 0));
                }
            }



            return vars;
        }
        public static List<UbarVar> getVariablesFromCondAsUbarsRightBias(string cond)
        {
            List<UbarVar> vars = new List<UbarVar>();

            string[] leftRight = cond.Split("->");
            //Console.WriteLine(leftRight[i]);
            string[] parts = leftRight[1].Split('"');
            foreach (string part in parts)
            {
                if (part.EndsWith("_0") || part.EndsWith("_1"))
                {

                    vars.Add(new UbarVar(part.Replace("_0", "").Replace("_1", ""), -1, false, null, 0));
                }
            }



            return vars;
        }


        /**
         * This method is here simply so I don't have to work with four levels of recursion in findUbarBackwards
         */
        public static bool notInUbarList(List<UbarVar> ubarVars, string ubarPotential)
        {
            foreach (UbarVar ubarVar in ubarVars)
            {
                if (ubarVar.name == ubarPotential)
                {
                    return false;
                }
            }
            return true;
        }



        /**
         * dumps the currently saved in memory Ubar in .csv format.
         */
        public static void dumpUbar(int counter)
        {
            using (StreamWriter outputFile = new StreamWriter(PATH + "Dumps\\ubar-"+counter+"-dump.csv"))
            {
                //name+","+index+","+isLeftSide+","+derivedFrom;
                outputFile.WriteLine("Name,Rung No.,Left Biased?,derived from,iteration");

                foreach (UbarVar ubarVar in UBAR)
                {
                    outputFile.WriteLine(ubarVar.ToString());
                }


            }
        }


        public static List<UbarVar> OriginalUBAR = new List<UbarVar>();

        /**
         * originally this implementation of this method was in quite a few places
         * its side-effect based, after calling, UBAR will now only contain variables at the respective levels
         */
        public static void doUbarFiltering(int level)
        {
            List<UbarVar> filteredUBAR = new List<UbarVar>();
            foreach (UbarVar ubarVar in UBAR)
            {
                if (ubarVar.iteration <= level)
                {
                    filteredUBAR.Add(ubarVar);
                }
            }
            OriginalUBAR = new List<UbarVar>(UBAR);
            UBAR = filteredUBAR;
        }
        public static void doUbarFilteringUp(int level)
        {
            List<UbarVar> filteredUBAR = new List<UbarVar>();
            foreach (UbarVar ubarVar in UBAR)
            {
                if (ubarVar.iteration >= level)
                {
                    filteredUBAR.Add(ubarVar);
                }
            }
            OriginalUBAR = new List<UbarVar>(UBAR);
            UBAR = filteredUBAR;
        }
        public static void doUbarFilteringDirect(int level)
        {
            List<UbarVar> filteredUBAR = new List<UbarVar>();
            foreach (UbarVar ubarVar in UBAR)
            {
                if (ubarVar.iteration == level)
                {
                    filteredUBAR.Add(ubarVar);
                }
            }
            OriginalUBAR = new List<UbarVar>(UBAR);
            UBAR = filteredUBAR;
        }
    }
}
